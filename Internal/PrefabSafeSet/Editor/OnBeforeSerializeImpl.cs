using System;
using System.Linq;
using JetBrains.Annotations;

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

            if (self.prefabLayers.Length == nestCount)
                DistinctCheck(self, nestCount);
            else if (self.prefabLayers.Length < nestCount)
                self.prefabLayers = PrefabSafeSetRuntimeUtil.ResizeArray(self.prefabLayers, nestCount);
            else if (self.prefabLayers.Length > nestCount)
                ApplyModificationsToLatestLayer(self, nestCount);
        }

        private static void DistinctCheck(PrefabSafeSet<T, TLayer> self, int nestCount)
        {
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
                var currentLayer = self.prefabLayers[nestCount - 1] ?? (self.prefabLayers[nestCount - 1] = new TLayer());
                DistinctCheckArray(ref currentLayer.additions, ref self.CheckedCurrentLayerAdditions,
                    PrefabSafeSetRuntimeUtil.IsNotNull);
                DistinctCheckArray(ref currentLayer.removes, ref self.CheckedCurrentLayerRemoves,
                    x => x.IsNotNull() && !currentLayer.additions.Contains(x));
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
                self.prefabLayers = PrefabSafeSetRuntimeUtil.ResizeArray(self.prefabLayers, nestCount);
            }
        }
    }
}
