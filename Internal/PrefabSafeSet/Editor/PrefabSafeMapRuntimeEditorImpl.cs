using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.PrefabSafeSet;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeMap
{
    [UsedImplicitly] // used by reflection
    internal static class PrefabSafeMapRuntimeEditorImpl<TKey, TValue>
    {
        [UsedImplicitly] // used by reflection
        public static void OnValidate<TComponent>(TComponent component, Func<TComponent, PrefabSafeMap<TKey, TValue>> getPrefabSafeMap) where TComponent : Component
        {
            // Notes for implementation
            // This implementation is based on the following assumptions:
            // - OnValidate will be called when the component is added
            // - OnValidate will be called when the component is maked prefab
            //   - Both for New Prefab Asset and Prefab Instance on Scene
            // - OnValidate will be called for prefab instance when the base prefab is changed
            var PrefabSafeMap = getPrefabSafeMap(component);

            // detect creating new prefab
            var newCorrespondingObject = PrefabUtility.GetCorrespondingObjectFromSource(component);
            if (newCorrespondingObject != null && PrefabUtility.GetCorrespondingObjectFromSource(newCorrespondingObject) ==  PrefabSafeMap.CorrespondingObject)
            {
                // this might be creating prefab. we do more checks
                var newCorrespondingPrefabSafeMap = getPrefabSafeMap(newCorrespondingObject);
                // if the corresponding object is not new, this likely mean the prefab is replaced
                if (newCorrespondingPrefabSafeMap.IsNew)
                {
                    // if the prefab is created, we clear onSceneLayer to avoid unnecessary modifications
                    PrefabSafeMap.onSceneLayer = new PrefabLayer<TKey, TValue>();
                    PrefabSafeMap.usingOnSceneLayer = false; // this should avoid creating prefab overrides
                }
            }

            PrefabSafeMap.OuterObject = component;
            PrefabSafeMap.CorrespondingObject = newCorrespondingObject;
            var nestCount = PrefabNestCount(component, getPrefabSafeMap);
            PrefabSafeMap.NestCount = nestCount;

            var shouldUsePrefabOnSceneLayer = PrefabSafeMapRuntimeUtil.ShouldUsePrefabOnSceneLayer(component);
            var maxLayerCount = shouldUsePrefabOnSceneLayer ? nestCount - 1 : nestCount;

            // https://github.com/anatawa12/AvatarOptimizer/issues/52
            // to avoid unnecessary modifications, do not resize array if layer count is smaller than expected

            if (!shouldUsePrefabOnSceneLayer && PrefabSafeMap.usingOnSceneLayer)
            {
                // migrate onSceneLayer to latest layer
                var onSceneLayer = PrefabSafeMap.onSceneLayer;

                if (maxLayerCount == 0)
                {
                    var result = new ListMap<TKey, TValue>(PrefabSafeMap.mainSet);
                    foreach (var layer in PrefabSafeMap.prefabLayers)
                    {
                        result.RemoveRange(layer.removes);
                        result.AddRange(layer.additions);
                    }

                    result.RemoveRange(onSceneLayer.removes);
                    result.AddRange(onSceneLayer.additions);

                    PrefabSafeMap.mainSet = result.ToArray();
                    PrefabSafeMap.prefabLayers = Array.Empty<PrefabLayer<TKey, TValue>>();
                }
                else
                {
                    PrefabSafeMapRuntimeUtil.ResizeArray(ref PrefabSafeMap.prefabLayers, maxLayerCount);
                    var currentLayer = PrefabSafeMap.prefabLayers[maxLayerCount - 1];
                    currentLayer.additions = currentLayer.additions.Concat(onSceneLayer.additions).ToArray();
                    currentLayer.removes = currentLayer.removes.Concat(onSceneLayer.removes).ToArray();
                }

                PrefabSafeMap.onSceneLayer = new PrefabLayer<TKey, TValue>();
                PrefabSafeMap.usingOnSceneLayer = false;
            }

            if (PrefabSafeMap.prefabLayers.Length > maxLayerCount)
                ApplyModificationsToLatestLayer(PrefabSafeMap, maxLayerCount, shouldUsePrefabOnSceneLayer);

            GeneralCheck(PrefabSafeMap, maxLayerCount, shouldUsePrefabOnSceneLayer);
        }

        private static int PrefabNestCount<TComponent>(TComponent component,
            Func<TComponent, PrefabSafeMap<TKey, TValue>> getPrefabSafeMap) where TComponent : Component
        {
            var correspondingObject = PrefabUtility.GetCorrespondingObjectFromSource(component);
            if (correspondingObject == null)
                return 0;
            var correspondingPrefabSafeMap = getPrefabSafeMap(correspondingObject);
            correspondingPrefabSafeMap.OuterObject = correspondingObject;
            if (correspondingPrefabSafeMap.NestCount is not { } nestCount)
                correspondingPrefabSafeMap.NestCount =
                    nestCount = PrefabNestCount(correspondingObject, getPrefabSafeMap);
            return nestCount + 1;
        }

        private static void GeneralCheck(PrefabSafeMap<TKey, TValue> self, int maxLayerCount, bool shouldUsePrefabOnSceneLayer)
        {
            // first, replace missing with null
            if (typeof(Object).IsAssignableFrom(typeof(TKey)))
            {
                var context = new PrefabSafeSetUtil.NullOrMissingContext(self.OuterObject);

                void ReplaceMissingWithNullEntries(MapEntry<TKey, TValue>[] array)
                {
                    for (var i = 0; i < array.Length; i++)
                    {
                        if (array[i].key.IsNullOrMissing(context))
                            array[i].key = default!;
                        else if (array[i].value.IsNullOrMissing(context))
                            array[i].value = default!;
                    }
                }

                void ReplaceMissingWithNull(TKey?[] array)
                {
                    for (var i = 0; i < array.Length; i++)
                    {
                        if (array[i].IsNullOrMissing(context))
                            array[i] = default;
                    }
                }

                ReplaceMissingWithNullEntries(self.mainSet);

                foreach (var layer in self.prefabLayers)
                {
                    ReplaceMissingWithNullEntries(layer.additions);
                    ReplaceMissingWithNull(layer.removes);
                }
            }

            void DistinctCheckArrayKey(ref TKey[] source, Func<TKey, bool> filter)
            {
                var array = source.Distinct().Where(filter).ToArray();
                if (array.Length != source.Length)
                    source = array;
            }

            void DistinctCheckArrayEntry(ref MapEntry<TKey, TValue>[] source, Func<TKey?, bool> filter)
            {
                var indexIndex = new Dictionary<TKey, int>();
                var list = new List<MapEntry<TKey, TValue>>();
                foreach (var entry in source)
                {
                    if (!entry.key.IsNotNull()) continue;
                    if (!filter(entry.key)) continue;

                    if (indexIndex.TryGetValue(entry.key, out var index))
                    {
                        list[index] = entry;
                    }
                    else
                    {
                        indexIndex.Add(entry.key, list.Count);
                        list.Add(entry);
                    }
                }

                if (list.Count != source.Length)
                    source = list.ToArray();
            }


            if (shouldUsePrefabOnSceneLayer)
            {
                var currentLayer = self.onSceneLayer;
                //self.usingOnSceneLayer = true; // this will create prefab overrides, which is not good.
                DistinctCheckArrayEntry(ref currentLayer.additions, PrefabSafeMapRuntimeUtil.IsNotNull);
                DistinctCheckArrayKey(ref currentLayer.removes,
                    x => x.IsNotNull() && !currentLayer.additions.Any(e => Equals(e.key, x)));
            }
            else if (maxLayerCount == 0)
            {
                DistinctCheckArrayEntry(ref self.mainSet, PrefabSafeMapRuntimeUtil.IsNotNull);
            }
            else if (maxLayerCount < self.prefabLayers.Length)
            {
                var currentLayer = self.prefabLayers[maxLayerCount - 1] ??
                                   (self.prefabLayers[maxLayerCount - 1] = new PrefabLayer<TKey, TValue>());
                DistinctCheckArrayEntry(ref currentLayer.additions, PrefabSafeMapRuntimeUtil.IsNotNull);
                DistinctCheckArrayKey(ref currentLayer.removes,
                    x => x.IsNotNull() && !currentLayer.additions.Any(e => Equals(e.key, x)));
            }
        }

        private static void ApplyModificationsToLatestLayer(PrefabSafeMap<TKey, TValue> self, int maxLayerCount, bool shouldUsePrefabOnSceneLayer)
        {
            // after apply modifications?: apply to latest layer
            if (maxLayerCount == 0 && !shouldUsePrefabOnSceneLayer)
            {
                // nestCount is 0: apply everything to mainSet
                var result = new ListMap<TKey, TValue>(self.mainSet);
                foreach (var layer in self.prefabLayers)
                {
                    result.RemoveRange(layer.removes);
                    result.AddRange(layer.additions);
                }

                self.mainSet = result.ToArray();
                self.prefabLayers = Array.Empty<PrefabLayer<TKey, TValue>>();
            }
            else
            {
                // nestCount is not zero: apply to current layer
                if (shouldUsePrefabOnSceneLayer) self.usingOnSceneLayer = true;
                var targetLayer = shouldUsePrefabOnSceneLayer ? self.onSceneLayer : self.prefabLayers[maxLayerCount - 1];
                var additions = new ListMap<TKey, TValue>(targetLayer.additions);
                var removes = new ListSet<TKey>(targetLayer.removes);

                foreach (var layer in self.prefabLayers.Skip(maxLayerCount))
                {
                    additions.RemoveRange(layer.removes);
                    removes.AddRange(layer.removes);

                    additions.AddRange(layer.additions);
                    removes.RemoveRange(layer.additions.Select(e => e.key!));
                }

                targetLayer.additions = additions.ToArray();
                targetLayer.removes = removes.ToArray();

                // resize array.               
                PrefabSafeMapRuntimeUtil.ResizeArray(ref self.prefabLayers, maxLayerCount);
            }
        }
    }
}
