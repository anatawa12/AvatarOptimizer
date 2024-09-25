using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    [UsedImplicitly] // used by reflection
    internal static class PrefabSafeSetRuntimeEditorImpl<T>
    {
        [UsedImplicitly] // used by reflection
        public static void OnValidate<TComponent>(TComponent component, Func<TComponent, PrefabSafeSet<T>> getPrefabSafeSet) where TComponent : Component
        {
            // Notes for implementation
            // This implementation is based on the following assumptions:
            // - OnValidate will be called when the component is added
            // - OnValidate will be called when the component is maked prefab
            //   - Both for New Prefab Asset and Prefab Instance on Scene
            // - OnValidate will be called for prefab instance when the base prefab is changed

            var prefabSafeSet = getPrefabSafeSet(component);
            prefabSafeSet.OuterObject = component;
            var nestCount = PrefabNestCount(component, getPrefabSafeSet);
            prefabSafeSet.NestCount = nestCount;

            var isInstance = PrefabUtility.IsPartOfPrefabInstance(component);
            var isAsset = PrefabUtility.IsPartOfPrefabAsset(component);

            var shouldUsePrefabOnSceneLayer = isInstance && !isAsset;
            var maxLayerCount = shouldUsePrefabOnSceneLayer ? nestCount - 1 : nestCount;

            // https://github.com/anatawa12/AvatarOptimizer/issues/52
            // to avoid unnecessary modifications, do not resize array if layer count is smaller than expected

            if (!shouldUsePrefabOnSceneLayer && prefabSafeSet.usingOnSceneLayer)
            {
                PrefabSafeSetRuntimeUtil.ResizeArray(ref prefabSafeSet.prefabLayers, maxLayerCount);
                var currentLayer = prefabSafeSet.prefabLayers[maxLayerCount - 1];
                currentLayer.additions = currentLayer.additions.Concat(prefabSafeSet.onSceneLayer.additions).ToArray();
                currentLayer.removes = currentLayer.removes.Concat(prefabSafeSet.onSceneLayer.removes).ToArray();
                prefabSafeSet.onSceneLayer = new PrefabLayer<T>();
                prefabSafeSet.usingOnSceneLayer = false;
            }

            if (prefabSafeSet.prefabLayers.Length > maxLayerCount)
                ApplyModificationsToLatestLayer(prefabSafeSet, maxLayerCount, shouldUsePrefabOnSceneLayer);

            GeneralCheck(prefabSafeSet, maxLayerCount, shouldUsePrefabOnSceneLayer);
        }

        private static int PrefabNestCount<TComponent>(TComponent component,
            Func<TComponent, PrefabSafeSet<T>> getPrefabSafeSet) where TComponent : Component
        {
            var correspondingObject = PrefabUtility.GetCorrespondingObjectFromSource(component);
            if (correspondingObject == null)
                return 0;
            var correspondingPrefabSafeSet = getPrefabSafeSet(correspondingObject);
            correspondingPrefabSafeSet.OuterObject = correspondingObject;
            if (correspondingPrefabSafeSet.NestCount is not { } nestCount)
                correspondingPrefabSafeSet.NestCount =
                    nestCount = PrefabNestCount(correspondingObject, getPrefabSafeSet);
            return nestCount + 1;
        }

        private static void GeneralCheck(PrefabSafeSet<T> self, int maxLayerCount, bool shouldUsePrefabOnSceneLayer)
        {
            // first, replace missing with null
            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                var context = new PrefabSafeSetUtil.NullOrMissingContext(self.OuterObject);

                void ReplaceMissingWithNull(T?[] array)
                {
                    for (var i = 0; i < array.Length; i++)
                        if (array[i].IsNullOrMissing(context))
                            array[i] = default;
                }

                ReplaceMissingWithNull(self.mainSet);

                foreach (var layer in self.prefabLayers)
                {
                    ReplaceMissingWithNull(layer.additions);
                    ReplaceMissingWithNull(layer.removes);
                }
            }

            void DistinctCheckArray(ref T[] source, Func<T, bool> filter)
            {
                var array = source.Distinct().Where(filter).ToArray();
                if (array.Length != source.Length)
                    source = array;
            }

            if (maxLayerCount == 0)
            {
                DistinctCheckArray(ref self.mainSet, PrefabSafeSetRuntimeUtil.IsNotNull);
            }
            else
            {
                if (shouldUsePrefabOnSceneLayer)
                {
                    var currentLayer = self.onSceneLayer;
                    self.usingOnSceneLayer = true;
                    DistinctCheckArray(ref currentLayer.additions, PrefabSafeSetRuntimeUtil.IsNotNull);
                    DistinctCheckArray(ref currentLayer.removes,
                        x => x.IsNotNull() && !currentLayer.additions.Contains(x));
                }
                else if (maxLayerCount < self.prefabLayers.Length)
                {
                    var currentLayer = self.prefabLayers[maxLayerCount - 1] ??
                                       (self.prefabLayers[maxLayerCount - 1] = new PrefabLayer<T>());
                    DistinctCheckArray(ref currentLayer.additions, PrefabSafeSetRuntimeUtil.IsNotNull);
                    DistinctCheckArray(ref currentLayer.removes,
                        x => x.IsNotNull() && !currentLayer.additions.Contains(x));
                }
            }
        }

        private static void ApplyModificationsToLatestLayer(PrefabSafeSet<T> self, int maxLayerCount, bool shouldUsePrefabOnSceneLayer)
        {
            // after apply modifications?: apply to latest layer
            if (maxLayerCount == 0)
            {
                // nestCount is 0: apply everything to mainSet
                var result = new ListSet<T>(self.mainSet);
                foreach (var layer in self.prefabLayers)
                {
                    result.RemoveRange(layer.removes);
                    result.AddRange(layer.additions);
                }

                self.mainSet = result.ToArray();
                self.prefabLayers = Array.Empty<PrefabLayer<T>>();
            }
            else
            {
                // nestCount is not zero: apply to current layer
                var targetLayer = shouldUsePrefabOnSceneLayer ? self.onSceneLayer : self.prefabLayers[maxLayerCount - 1];
                var additions = new ListSet<T>(targetLayer.additions);
                var removes = new ListSet<T>(targetLayer.removes);

                foreach (var layer in self.prefabLayers.Skip(maxLayerCount))
                {
                    additions.RemoveRange(layer.removes);
                    removes.AddRange(layer.removes);

                    additions.AddRange(layer.additions);
                    removes.RemoveRange(layer.additions);
                }

                targetLayer.additions = additions.ToArray();
                targetLayer.removes = removes.ToArray();

                // resize array.               
                PrefabSafeSetRuntimeUtil.ResizeArray(ref self.prefabLayers, maxLayerCount);
            }
        }
    }
}
