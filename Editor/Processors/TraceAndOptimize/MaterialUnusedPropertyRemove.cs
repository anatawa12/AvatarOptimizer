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
            var swapDict = new Dictionary<Material, Material>();

            void SwapMaterialArray(Material[] matArray)
            {
                for (var i = 0; matArray.Length > i; i += 1)
                {
                    var sourceMaterial = matArray[i];
                    if (sourceMaterial == null) { continue; }

                    if (swapDict.TryGetValue(sourceMaterial, out var cleanMaterial)) { matArray[i] = cleanMaterial; }
                    else { swapDict[sourceMaterial] = matArray[i] = MaterialCleaning(sourceMaterial); }
                }
            }

            foreach (var renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer smr)
                {
                    var meshInfo = context.GetMeshInfoFor(smr);
                    foreach (var subMesh in meshInfo.SubMeshes)
                        SwapMaterialArray(subMesh.SharedMaterials);
                }
                else
                {
                    var matArray = renderer.sharedMaterials;
                    SwapMaterialArray(matArray);
                    renderer.sharedMaterials = matArray;
                }
            }

            foreach (var matKv in swapDict) ObjectRegistry.RegisterReplacedObject(matKv.Key, matKv.Value);
        }

        public static Material MaterialCleaning(Material i)
        {
            var mat = UnityEngine.Object.Instantiate(i);
            mat.name = i.name + "&AAO_MATERIAL_UNUSED_PROPERTIES_REMOLDED";
            RemoveUnusedProperties(mat);
            return mat;
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
