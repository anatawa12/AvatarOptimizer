using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    [UsedImplicitly] // used by reflection
    internal static class OnBeforeSerializeImpl<T, TLayer> where TLayer : PrefabLayer<T>, new()
    {
        [UsedImplicitly] // used by reflection
        public static void Impl(PrefabSafeSet<T, TLayer> self)
        {
            // fakeSlot must not be modified,
            self.fakeSlot = default;
            if (!self.OuterObject) return;

            // match prefabLayers count.
            var nestCount = PrefabSafeSetUtil.PrefabNestCount(self.OuterObject);

            if (self.prefabLayers.Length < nestCount)
            {
                // https://github.com/anatawa12/AvatarOptimizer/issues/52
                // to avoid unnecessary modifications, resize is not performed later.
            }
            else if (self.prefabLayers.Length > nestCount)
                ApplyModificationsToLatestLayer(self, nestCount);

            GeneralCheck(self, nestCount);
        }

        private static void GeneralCheck(PrefabSafeSet<T, TLayer> self, int nestCount)
        {
            // first, replace missing with corresponding object
            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                // TODO: add to removed of current layer if replaced with correspondingObject
                var context = new PrefabSafeSetUtil.NullOrMissingContext(self.OuterObject);

                void ReplaceMissingWithCorrespondingObject(T[] array, Object[] correspondingObjects)
                {
                    for (var i = 0; i < array.Length; i++)
                        if (array[i].IsNullOrMissing(context))
                        {
                            var correspondingObject = correspondingObjects[i];
                            array[i] = correspondingObject is null ? default : (T)(object)correspondingObject;
                        }
                }

                ReplaceMissingWithCorrespondingObject(self.mainSet, self.MainSetCorrespondingObjects);

                foreach (var layer in self.prefabLayers)
                {
                    ReplaceMissingWithCorrespondingObject(layer.additions, layer.AdditionsCorrespondingObjects);
                    ReplaceMissingWithCorrespondingObject(layer.removes, layer.RemovesCorrespondingObjects);
                }
            }

            void DistinctCheckArray(ref T[] source, ref T[] checkedArray, Func<T, bool> filter)
            {
                if (checkedArray == source && source.All(filter)) return;
                var array = source.Distinct().Where(filter).ToArray();
                if (array.Length != source.Length)
                    source = array;
                checkedArray = source;
            }

            if (nestCount == 0)
            {
                DistinctCheckArray(ref self.mainSet, ref self.CheckedCurrentLayerAdditions, 
                    PrefabSafeSetRuntimeUtil.IsNotNull);
            }
            else
            {
                if (nestCount < self.prefabLayers.Length)
                {
                    var currentLayer = self.prefabLayers[nestCount - 1] ??
                                       (self.prefabLayers[nestCount - 1] = new TLayer());
                    DistinctCheckArray(ref currentLayer.additions, ref self.CheckedCurrentLayerAdditions,
                        PrefabSafeSetRuntimeUtil.IsNotNull);
                    DistinctCheckArray(ref currentLayer.removes, ref self.CheckedCurrentLayerRemoves,
                        x => x.IsNotNull() && !currentLayer.additions.Contains(x));
                }
            }
        }

        private static void ApplyModificationsToLatestLayer(PrefabSafeSet<T,TLayer> self, int nestCount)
        {
            // after apply modifications?: apply to latest layer
            if (nestCount == 0)
            {
                // nestCount is 0: apply everything to mainSet
                var result = new ListSet<T>(self.mainSet);
                foreach (var layer in self.prefabLayers)
                {
                    result.RemoveRange(layer.removes);
                    result.AddRange(layer.additions);
                }

                self.mainSet = result.ToArray();
                self.prefabLayers = Array.Empty<TLayer>();
            }
            else
            {
                // nestCount is not zero: apply to current layer
                var targetLayer = self.prefabLayers[nestCount - 1];
                var additions = new ListSet<T>(targetLayer.additions);
                var removes = new ListSet<T>(targetLayer.removes);

                foreach (var layer in self.prefabLayers.Skip(nestCount))
                {
                    additions.RemoveRange(layer.removes);
                    removes.AddRange(layer.removes);

                    additions.AddRange(layer.additions);
                    removes.RemoveRange(layer.additions);
                }

                targetLayer.additions = additions.ToArray();
                targetLayer.removes = removes.ToArray();

                // resize array.               
                PrefabSafeSetRuntimeUtil.ResizeArray(ref self.prefabLayers, nestCount);
            }
        }
    }
}
