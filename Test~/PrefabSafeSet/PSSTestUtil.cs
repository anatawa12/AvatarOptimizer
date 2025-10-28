using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.PrefabSafeSet;
using Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection;
using Anatawa12.AvatarOptimizer.Test.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.PrefabSafeSet
{
    public static class PSSTestUtil
    {
        private static GameObject CreateBasePrefab()
        {
            var newObject = new GameObject();
            var component = newObject.AddComponent<PrefabSafeSetComponent>();
            component.stringSet.mainSet = new[]
                { "mainSet", "addedTwiceInVariant", "removedInVariant", "addedTwiceInInstance", "removedInInstance" };
            newObject = PrefabUtility.SaveAsPrefabAsset(newObject, $"Assets/test-{Guid.NewGuid()}.prefab");
            newObject.GetComponent<PrefabSafeSetComponent>().stringSet.IsNew = false;
            return newObject;
        }

        private static GameObject CreateBaseVariantPrefab(GameObject basePrefab)
        {
            var newObject = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            var component = newObject.GetComponent<PrefabSafeSetComponent>();
            PSUCRuntimeUtil.ResizeArray(ref component.stringSet.prefabLayers, 1);
            component.stringSet.prefabLayers[0].additions =
                new[] { "addedTwiceInVariant", "addedInVariant", "addedInVariantRemovedInInstance" };
            component.stringSet.prefabLayers[0].removes = new[] { "removedInVariant", "fakeRemovedInVariant" };
            newObject = PrefabUtility.SaveAsPrefabAsset(newObject, $"Assets/test-{Guid.NewGuid()}.prefab");
            newObject.GetComponent<PrefabSafeSetComponent>().stringSet.IsNew = false;
            Assert.True(newObject);
            return newObject;
        }

        private static GameObject CreateInstance(GameObject baseObject)
        {
            var newObject = (GameObject)PrefabUtility.InstantiatePrefab(baseObject);
            var component = newObject.GetComponent<PrefabSafeSetComponent>();
            PSUCRuntimeUtil.ResizeArray(ref component.stringSet.prefabLayers, 2);
            component.stringSet.prefabLayers[1].additions = new[] { "addedTwiceInInstance", "addedInInstance" };
            component.stringSet.prefabLayers[1].removes = new[]
                { "removedInInstance", "addedInVariantRemovedInInstance", "fakeRemovedInInstance" };
            component.stringSet.IsNew = false;
            return newObject;
        }

        public class Scope : IDisposable
        {
            public readonly PrefabSafeSetComponent Prefab;
            public readonly PrefabSafeSetComponent Variant;
            public readonly PrefabSafeSetComponent Instance;
            
            public readonly SerializedObject PrefabSerialized;
            public readonly SerializedObject VariantSerialized;
            public readonly SerializedObject InstanceSerialized;

            public readonly PSSEditorUtil<string> PrefabEditorUtil;
            public readonly PSSEditorUtil<string> VariantEditorUtil;
            public readonly PSSEditorUtil<string> InstanceEditorUtil;

            public Scope()
            {
                var prefab = CreateBasePrefab();
                var prefabVariant = CreateBaseVariantPrefab(prefab);
                var instance = CreateInstance(prefabVariant);
                Prefab = prefab.GetComponent<PrefabSafeSetComponent>();
                Variant = prefabVariant.GetComponent<PrefabSafeSetComponent>();
                Instance = instance.GetComponent<PrefabSafeSetComponent>();
                
                PrefabSerialized = new SerializedObject(Prefab);
                VariantSerialized = new SerializedObject(Variant);
                InstanceSerialized = new SerializedObject(Instance);

                PSSEditorUtil<string> MakeUtil(SerializedObject obj, int nestCount) => PSSEditorUtil<string>.Create(
                        obj.FindProperty("stringSet"),
                        x => x.stringValue, (x, v) => x.stringValue = v);

                List<string> props = new List<string>();
                var iter = VariantSerialized.GetIterator();
                while (iter.Next(true))
                    props.Add(iter.propertyPath);
                Debug.Log(props);

                PrefabEditorUtil = MakeUtil(PrefabSerialized, 0);
                VariantEditorUtil = MakeUtil(VariantSerialized, 1);
                InstanceEditorUtil = MakeUtil(InstanceSerialized, 2);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Instance);
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(Variant.gameObject));
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(Prefab.gameObject));
            }
        }

        
    }
}
