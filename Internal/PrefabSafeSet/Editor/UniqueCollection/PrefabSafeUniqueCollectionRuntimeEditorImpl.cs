using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.PrefabSafeSet;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection
{
    [UsedImplicitly] // used by reflection
    internal static class PrefabSafeUniqueCollectionRuntimeEditorImpl<
        TAdditionValue,
        TRemoveKey,
        TManipulator
    >
        where TRemoveKey : notnull
        where TAdditionValue : notnull
        where TManipulator : struct, IManipulator<TAdditionValue, TRemoveKey>
    {
        [UsedImplicitly] // used by reflection
        public static void OnValidate<TComponent>(TComponent component,
            Func<TComponent, PrefabSafeUniqueCollection<TAdditionValue, TRemoveKey, TManipulator>>
                getPrefabSafeUniqueCollection) where TComponent : Component
        {
            // Notes for implementation
            // This implementation is based on the following assumptions:
            // - OnValidate will be called when the component is added
            // - OnValidate will be called when the component is maked prefab
            //   - Both for New Prefab Asset and Prefab Instance on Scene
            // - OnValidate will be called for prefab instance when the base prefab is changed
            var PrefabSafeUniqueCollection = getPrefabSafeUniqueCollection(component);

            // The prefab from prefab stage is going to be saved. I don't update on this phase.
            if (component == null) return;

            // detect creating new prefab
            var newCorrespondingObject = PrefabUtility.GetCorrespondingObjectFromSource(component);
            if (newCorrespondingObject != null &&
                PrefabUtility.GetCorrespondingObjectFromSource(newCorrespondingObject) ==
                PrefabSafeUniqueCollection.CorrespondingObject
                && !PrefabSafeUniqueCollection.IsNew) // not loading scene
            {
                // this might be creating prefab. we do more checks
                var newCorrespondingPrefabSafeUniqueCollection = getPrefabSafeUniqueCollection(newCorrespondingObject);
                // if the corresponding object is not new, this likely mean the prefab is replaced
                if (newCorrespondingPrefabSafeUniqueCollection.IsNew)
                {
                    // if the prefab is created, we clear onSceneLayer to avoid unnecessary modifications
                    PrefabSafeUniqueCollection.onSceneLayer = new PrefabLayer<TAdditionValue, TRemoveKey>();
                    PrefabSafeUniqueCollection.usingOnSceneLayer = false; // this should avoid creating prefab overrides
                }
            }

            PrefabSafeUniqueCollection.OuterObject = component;
            PrefabSafeUniqueCollection.CorrespondingObject = newCorrespondingObject;
            var nestCount = PrefabNestCount(component, getPrefabSafeUniqueCollection);
            PrefabSafeUniqueCollection.NestCount = nestCount;

            var shouldUsePrefabOnSceneLayer =
                nestCount != 0 && PSUCRuntimeUtil.ShouldUsePrefabOnSceneLayer(component);
            var maxLayerCount = shouldUsePrefabOnSceneLayer ? nestCount - 1 : nestCount;

            // https://github.com/anatawa12/AvatarOptimizer/issues/52
            // to avoid unnecessary modifications, do not resize array if layer count is smaller than expected

            if (!shouldUsePrefabOnSceneLayer && PrefabSafeUniqueCollection.usingOnSceneLayer)
            {
                // migrate onSceneLayer to latest layer
                var onSceneLayer = PrefabSafeUniqueCollection.onSceneLayer;

                if (maxLayerCount == 0)
                {
                    var result = new ListMap<TAdditionValue, TRemoveKey, TManipulator>(PrefabSafeUniqueCollection.mainSet, default);
                    foreach (var layer in PrefabSafeUniqueCollection.prefabLayers)
                    {
                        result.RemoveRange(layer.removes);
                        result.AddRange(layer.additions);
                    }

                    result.RemoveRange(onSceneLayer.removes);
                    result.AddRange(onSceneLayer.additions);

                    PrefabSafeUniqueCollection.mainSet = result.ToArray();
                    PrefabSafeUniqueCollection.prefabLayers = Array.Empty<PrefabLayer<TAdditionValue, TRemoveKey>>();
                }
                else
                {
                    PSUCRuntimeUtil.ResizeArray(ref PrefabSafeUniqueCollection.prefabLayers,
                        maxLayerCount);
                    var currentLayer = PrefabSafeUniqueCollection.prefabLayers[maxLayerCount - 1];
                    currentLayer.additions = currentLayer.additions.Concat(onSceneLayer.additions).ToArray();
                    currentLayer.removes = currentLayer.removes.Concat(onSceneLayer.removes).ToArray();
                }

                PrefabSafeUniqueCollection.onSceneLayer = new PrefabLayer<TAdditionValue, TRemoveKey>();
                PrefabSafeUniqueCollection.usingOnSceneLayer = false;
            }

            if (PrefabSafeUniqueCollection.prefabLayers.Length > maxLayerCount)
                ApplyModificationsToLatestLayer(PrefabSafeUniqueCollection, maxLayerCount, shouldUsePrefabOnSceneLayer);

            GeneralCheck(PrefabSafeUniqueCollection, maxLayerCount, shouldUsePrefabOnSceneLayer);
        }

        private static int PrefabNestCount<TComponent>(TComponent component,
            Func<TComponent, PrefabSafeUniqueCollection<TAdditionValue, TRemoveKey, TManipulator>>
                getPrefabSafeUniqueCollection) where TComponent : Component
        {
            var correspondingObject = PrefabUtility.GetCorrespondingObjectFromSource(component);
            if (correspondingObject == null)
                return 0;
            var correspondingPrefabSafeUniqueCollection = getPrefabSafeUniqueCollection(correspondingObject);
            correspondingPrefabSafeUniqueCollection.OuterObject = correspondingObject;
            if (correspondingPrefabSafeUniqueCollection.NestCount is not { } nestCount)
                correspondingPrefabSafeUniqueCollection.NestCount =
                    nestCount = PrefabNestCount(correspondingObject, getPrefabSafeUniqueCollection);
            return nestCount + 1;
        }

        private static void GeneralCheck(PrefabSafeUniqueCollection<TAdditionValue, TRemoveKey, TManipulator> self,
            int maxLayerCount, bool shouldUsePrefabOnSceneLayer)
        {
            // first, replace missing with null
            if (typeof(Object).IsAssignableFrom(typeof(TRemoveKey)))
            {
                var context = new PSUCUtil.NullOrMissingContext(self.OuterObject);

                void ReplaceMissingWithNullEntries(TAdditionValue?[] array)
                {
                    foreach (ref var e in array.AsSpan())
                    {
                        ref var key = ref default(TManipulator).GetKey(ref e);
                        if (key.IsNullOrMissing(context))
                            key = default!;
                    }
                }

                void ReplaceMissingWithNull(TRemoveKey?[] array)
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

            void DistinctCheckArrayKey(ref TRemoveKey?[] source, Func<TRemoveKey, bool> filter)
            {
                var array = source.Distinct().Where(v => v.IsNotNull() && filter(v)).ToArray();
                if (array.Length != source.Length)
                    source = array;
            }

            void DistinctCheckArrayEntry(ref TAdditionValue?[] source, Func<TRemoveKey?, bool> filter)
            {
                var indexIndex = new Dictionary<TRemoveKey, int>();
                var list = new List<TAdditionValue?>();
                foreach (ref var entry in source.AsSpan())
                {
                    var key = default(TManipulator).GetKey(entry);
                    if (!key.IsNotNull()) continue;
                    if (!filter(key)) continue;

                    if (indexIndex.TryGetValue(key, out var index))
                    {
                        list[index] = entry;
                    }
                    else
                    {
                        indexIndex.Add(key, list.Count);
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
                DistinctCheckArrayEntry(ref currentLayer.additions, PSUCRuntimeUtil.IsNotNull);
                DistinctCheckArrayKey(ref currentLayer.removes,
                    x => x.IsNotNull() && !currentLayer.additions.Any(e => Equals(default(TManipulator).GetKey(e), x)));
            }
            else if (maxLayerCount == 0)
            {
                DistinctCheckArrayEntry(ref self.mainSet, PSUCRuntimeUtil.IsNotNull);
            }
            else if (maxLayerCount < self.prefabLayers.Length)
            {
                var currentLayer = self.prefabLayers[maxLayerCount - 1] ??
                                   (self.prefabLayers[maxLayerCount - 1] =
                                       new PrefabLayer<TAdditionValue, TRemoveKey>());
                DistinctCheckArrayEntry(ref currentLayer.additions, PSUCRuntimeUtil.IsNotNull);
                DistinctCheckArrayKey(ref currentLayer.removes,
                    x => x.IsNotNull() && !currentLayer.additions.Any(e => Equals(default(TManipulator).GetKey(e), x)));
            }
        }

        private static void ApplyModificationsToLatestLayer(
            PrefabSafeUniqueCollection<TAdditionValue, TRemoveKey, TManipulator> self, int maxLayerCount,
            bool shouldUsePrefabOnSceneLayer)
        {
            // after apply modifications?: apply to latest layer
            if (maxLayerCount == 0 && !shouldUsePrefabOnSceneLayer)
            {
                // nestCount is 0: apply everything to mainSet
                var result = new ListMap<TAdditionValue, TRemoveKey, TManipulator>(self.mainSet, default);
                foreach (var layer in self.prefabLayers)
                {
                    result.RemoveRange(layer.removes);
                    result.AddRange(layer.additions);
                }

                self.mainSet = result.ToArray();
                self.prefabLayers = Array.Empty<PrefabLayer<TAdditionValue, TRemoveKey>>();
            }
            else
            {
                // nestCount is not zero: apply to current layer
                if (shouldUsePrefabOnSceneLayer) self.usingOnSceneLayer = true;
                var targetLayer = shouldUsePrefabOnSceneLayer
                    ? self.onSceneLayer
                    : self.prefabLayers[maxLayerCount - 1];
                var additions = new ListMap<TAdditionValue, TRemoveKey, TManipulator>(targetLayer.additions, default);
                var removes = new ListMap<TRemoveKey, TRemoveKey, IdentityManipulator<TRemoveKey>>(targetLayer.removes, default);

                foreach (var layer in self.prefabLayers.Skip(maxLayerCount))
                {
                    additions.RemoveRange(layer.removes);
                    removes.AddRange(layer.removes);

                    additions.AddRange(layer.additions);
                    removes.RemoveRange(layer.additions.Select(e => default(TManipulator).GetKey(e)).NonNulls());
                }

                targetLayer.additions = additions.ToArray();
                targetLayer.removes = removes.ToArray();

                // resize array.               
                PSUCRuntimeUtil.ResizeArray(ref self.prefabLayers, maxLayerCount);
            }
        }
    }
}
