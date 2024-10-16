using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class MaterialUnusedPropertyRemove : TraceAndOptimizePass<MaterialUnusedPropertyRemove>
    {
        public override string DisplayName => "T&O: MaterialUnusedPropertyRemove";
        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            if (!state.RemoveUnusedObjects) { return; }
            if (state.SkipRemoveMaterialUnusedProperties) { return; }

            var renderers = context.GetComponents<Renderer>();
            var cleaned = new HashSet<Material>();

            void MaterialCleaning(IEnumerable<Material?> materials)
            {
                foreach (var m in materials)
                    if (m is not null && cleaned.Contains(m) is false)
                    {
                        RemoveUnusedProperties(m);
                        cleaned.Add(m);
                    }
            }

            foreach (var renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer smr)
                {
                    var meshInfo = context.GetMeshInfoFor(smr);
                    foreach (var subMesh in meshInfo.SubMeshes)
                        MaterialCleaning(subMesh.SharedMaterials);
                }
                else { MaterialCleaning(renderer.sharedMaterials); }
            }
        }

        //MIT License
        //Copyright (c) 2020-2021 lilxyzw
        //https://github.com/lilxyzw/lilToon/blob/b96470d3dd9092b840052578048b2307fe6d8786/Assets/lilToon/Editor/lilMaterialUtils.cs#L658-L686
        //
        //https://light11.hatenadiary.com/entry/2018/12/04/224253
        public static void RemoveUnusedProperties(Material material)
        {
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
            for (int i = props.arraySize - 1; i >= 0; i--)
            {
                if (!material.HasProperty(props.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue))
                {
                    props.DeleteArrayElementAtIndex(i);
                }
            }
        }
    }
}
