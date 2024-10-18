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

        //MIT License
        //Copyright (c) 2020-2021 lilxyzw
        //https://github.com/lilxyzw/lilToon/blob/b96470d3dd9092b840052578048b2307fe6d8786/Assets/lilToon/Editor/lilMaterialUtils.cs#L658-L686
        //
        //https://light11.hatenadiary.com/entry/2018/12/04/224253
        public static void RemoveUnusedProperties(Material material)
        {
            // TODO: support material variant
            var so = new SerializedObject(material);
            so.Update();
            var savedProps = so.FindProperty("m_SavedProperties");

            var texs = savedProps.FindPropertyRelative("m_TexEnvs");
            DeleteUnused(ref texs, material);

            var floats = savedProps.FindPropertyRelative("m_Floats");
            DeleteUnused(ref floats, material);

            var colors = savedProps.FindPropertyRelative("m_Colors");
            DeleteUnused(ref colors, material);

            so.ApplyModifiedProperties();
        }

        public static void DeleteUnused(ref SerializedProperty props, Material material)
        {
            for (var i = props.arraySize - 1; i >= 0; i--)
            {
                var porpertyName = props.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue;
                if (!material.HasProperty(porpertyName) && !fallbackShaderProperties.Contains(porpertyName))
                {
                    props.DeleteArrayElementAtIndex(i);
                }
            }
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
