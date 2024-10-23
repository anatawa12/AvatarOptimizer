using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class RemoveUnusedMaterialProperties : TraceAndOptimizePass<RemoveUnusedMaterialProperties>
    {
        public override string DisplayName => "T&O: RemoveUnusedMaterialProperties";
        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            if (!state.RemoveUnusedObjects) { return; }
            if (state.SkipRemoveMaterialUnusedProperties) { return; }

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

        // Algorithm is based on lilToon or thr following blog post,
        // but speed up with not using DeleteArrayElementAtIndex.
        //
        // MIT License
        // Copyright (c) 2020-2021 lilxyzw
        // https://github.com/lilxyzw/lilToon/blob/b96470d3dd9092b840052578048b2307fe6d8786/Assets/lilToon/Editor/lilMaterialUtils.cs#L658-L686
        //
        // https://light11.hatenadiary.com/entry/2018/12/04/224253
        private static void RemoveUnusedProperties(Material material)
        {
            using var so = new SerializedObject(material);

            DeleteUnusedProperties(so.FindProperty("m_SavedProperties.m_TexEnvs"), material);
            DeleteUnusedProperties(so.FindProperty("m_SavedProperties.m_Floats"), material);
            DeleteUnusedProperties(so.FindProperty("m_SavedProperties.m_Colors"), material);

            so.ApplyModifiedProperties();
        }

        private static void DeleteUnusedProperties(SerializedProperty props, Material material)
        {
            if (props.arraySize == 0) return;

            var destCount = 0;
            for (
                SerializedProperty srcIter = props.GetArrayElementAtIndex(0),
                    destIter = srcIter.Copy(),
                    srcEnd = props.GetEndProperty();
                !SerializedProperty.EqualContents(srcIter, srcEnd);
                srcIter.NextVisible(false)
            )
            {
                var porpertyName = srcIter.FindPropertyRelative("first").stringValue;
                if (material.HasProperty(porpertyName) || fallbackShaderProperties.Contains(porpertyName))
                {
                    destIter.CopyDataFrom(srcIter);
                    destIter.NextVisible(false);
                    destCount++;
                }
            }

            props.arraySize = destCount;
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
