using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class RemoveUnusedMaterialProperties : TraceAndOptimizePass<RemoveUnusedMaterialProperties>
    {
        public override string DisplayName => "T&O: RemoveUnusedMaterialProperties";
        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            if (!state.RemoveUnusedObjects) { return; }

            if (!state.SkipRemoveMaterialUnusedProperties)
                RemoveUnusedProperties(context);

            if (!state.SkipRemoveMaterialUnusedTextures)
                RemoveUnusedTextures(context);
        }

        internal void RemoveUnusedTextures(BuildContext context)
        {
            var materials = context.GetComponents<Renderer>()
                .SelectMany(x => context.GetAllPossibleMaterialFor(x))
                .Where(x => x != null)
                .ToHashSet();
            
            foreach (var material in materials)
            {
                if (context.GetMaterialInformation(material) is { DefaultResult.TextureUsageInformationList: { } texInfoList } matInfo)
                {
                    var usedProperties = texInfoList
                        .Select(x => x.MaterialPropertyName)
                        .ToHashSet();
                    
                    // Fallback shaders are only considered if information is found.
                    // This may change the behavior in fallback.                    
                    if (matInfo.FallbackResult?.TextureUsageInformationList is { } fallbackTexInfoList)
                    {
                        usedProperties.UnionWith(fallbackTexInfoList
                            .Select(x => x.MaterialPropertyName));
                    }

                    // GetTexturePropertyNames returns all texture properties regardless of the current shader.
                    foreach (var property in material.GetTexturePropertyNames())
                    {
                        if (!usedProperties.Contains(property))
                            material.SetTexture(property, null);
                    }
                }
            }
        }

        internal void RemoveUnusedProperties(BuildContext context)
        {
            var renderers = context.GetComponents<Renderer>();
            var cleaned = new HashSet<Material>();

            void CleanMaterial(IEnumerable<Material?> materials)
            {
                foreach (var m in materials)
                {
                    if (m != null && !cleaned.Contains(m))
                    {
                        if (context.IsTemporaryAsset(m))
                            RemoveUnusedProperties(m);
                        cleaned.Add(m);
                    }
                }
            }

            foreach (var renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer smr)
                {
                    var meshInfo = context.GetMeshInfoFor(smr);
                    foreach (var subMesh in meshInfo.SubMeshes)
                        CleanMaterial(subMesh.SharedMaterials);
                }
                else
                {
                    CleanMaterial(renderer.sharedMaterials);
                }

                CleanMaterial(context.GetAnimationComponent(renderer).GetAllObjectProperties()
                    .SelectMany(x => x.node.Value.PossibleValues)
                    .OfType<Material>());
            }
        }

        [Serializable]
        class StringContainer : ScriptableSingleton<StringContainer>
        {
            public string? theString;
        } 
        
        class ShaderInfo
        {
            private HashSet<uint> properties;

            public bool HasProperty(SerializedProperty prop)
            {
                return properties.Contains(prop.contentHash);
            }
            
            public ShaderInfo(Shader shader)
            {
                Profiler.BeginSample("ShaderInfo.ctor", shader);
                var props = shader.GetPropertyCount();
                var serializedProp = new SerializedObject(StringContainer.instance)
                    .FindProperty(nameof(StringContainer.theString));

                properties = new HashSet<uint>(props + fallbackShaderProperties.Count);
                
                for (int i = 0; i < props; i++)
                {
                    serializedProp.stringValue = shader.GetPropertyName(i);
                    properties.Add(serializedProp.contentHash);
                }
                
                foreach (var fallbackPropName in fallbackShaderProperties)
                {
                    serializedProp.stringValue = fallbackPropName;
                    properties.Add(serializedProp.contentHash);
                }
                Profiler.EndSample();
            }
        }
        
        private static Dictionary<Shader, ShaderInfo> _shaderInfoCache = new();
        
        // Algorithm is based on lilToon or thr following blog post,
        // but speed up with not using DeleteArrayElementAtIndex.
        //
        // MIT License
        // Copyright (c) 2020-2021 lilxyzw
        // https://github.com/lilxyzw/lilToon/blob/b96470d3dd9092b840052578048b2307fe6d8786/Assets/lilToon/Editor/lilMaterialUtils.cs#L658-L686
        //
        // https://light11.hatenadiary.com/entry/2018/12/04/224253
        public static void RemoveUnusedProperties(Material material)
        {
            var shader = material.shader;
            if (shader == null) return;
            
            if (!_shaderInfoCache.TryGetValue(shader, out var shaderInfo))
            {
                shaderInfo = new ShaderInfo(shader);
                _shaderInfoCache.Add(shader, shaderInfo);
            }
            
            using var so = new SerializedObject(material);

            DeleteUnusedProperties(so.FindProperty("m_SavedProperties.m_TexEnvs"), material, shaderInfo);
            DeleteUnusedProperties(so.FindProperty("m_SavedProperties.m_Floats"), material, shaderInfo);
            DeleteUnusedProperties(so.FindProperty("m_SavedProperties.m_Colors"), material, shaderInfo);

            so.ApplyModifiedProperties();
        }

        private static void DeleteUnusedProperties(SerializedProperty props, Material material, ShaderInfo shaderInfo)
        {
            if (props.arraySize == 0) return;
            
            // Using Utils.CopyDataFrom generates a lot of garbage. Instead, we'll let the native side move stuff
            // around for us; we'll generate a plan for how to move things, then execute it at the end.
            
            List<(int, int)> fromTo = new List<(int, int)>(props.arraySize);

            var arrayLen = props.arraySize;
            var srcIndex = 0;
            var destIndex = 0;
            for (
                SerializedProperty srcIter = props.GetArrayElementAtIndex(0);
                srcIndex < arrayLen;
                srcIter.NextVisible(false)
            )
            {
                var propertyName = srcIter.FindPropertyRelative("first");
                
                if (shaderInfo.HasProperty(propertyName))
                {
                    if (destIndex != srcIndex)
                    {
                        fromTo.Add((srcIndex, destIndex));
                    }

                    destIndex++;
                }

                srcIndex++;
            }

            Profiler.BeginSample("Apply moves");
            int sourceOffset = 0;
            int remainingMoves = fromTo.Count;
            var remainingArraySize = props.arraySize;
            foreach (var (originalFrom, to) in fromTo)
            {
                // Compute whether we want to delete any skipped array elements.
                // If we delete, we have to move (once) every element after the element in question.
                // However, if we don't, then every subsequent retained element will move this element.
                
                // If we do delete, we must adjust sourceOffset to ensure we're reading from the right place.
                var from = originalFrom + sourceOffset;
                while (from != to)
                {
                    var costToRetain = remainingMoves;
                    var costToDelete = remainingArraySize - to - 1;

                    if (costToDelete < costToRetain)
                    {
                        props.DeleteArrayElementAtIndex(to);
                        sourceOffset--;
                        remainingArraySize--;
                        from--;
                    }
                    else
                    {
                        break;
                    }
                }

                if (from == to) continue;
                
                // This MoveArrayElement call effectively rotates the range of elements
                // between from and to. As such, since we are iterating from the start,
                // each prior rotation doesn't affect the "from" index of subsequent elements.
                props.MoveArrayElement(from, to);
            }
            Profiler.EndSample();

            props.arraySize = destIndex;
        }

        // TODO: change set of properties by fallback shader names
        // https://creators.vrchat.com/avatars/shader-fallback-system
        private static HashSet<string> fallbackShaderProperties = new HashSet<string>
        {
            "_MainTex",
            "_MetallicGlossMap",
            "_SpecGlossMap",
            "_BumpMap",
            "_ParallaxMap",
            "_OcclusionMap",
            "_EmissionMap",
            "_DetailMask",
            "_DetailAlbedoMap",
            "_DetailNormalMap",
            "_Color",
            "_EmissionColor",
            "_SpecColor",
            "_Cutoff",
            "_Glossiness",
            "_GlossMapScale",
            "_SpecularHighlights",
            "_GlossyReflections",
            "_SmoothnessTextureChannel",
            "_Metallic",
            "_SpecularHighlights",
            "_GlossyReflections",
            "_BumpScale",
            "_Parallax",
            "_OcclusionStrength",
            "_DetailNormalMapScale",
            "_UVSec",
            "_Mode",
            "_SrcBlend",
            "_DstBlend",
            "_ZWrite",
        };
    }
}
