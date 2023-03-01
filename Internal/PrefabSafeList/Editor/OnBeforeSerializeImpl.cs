using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Anatawa12.AvatarOptimizer.PrefabSafeList
{
    [UsedImplicitly] // used by reflection
    internal static class OnBeforeSerializeImpl<T, TLayer, TContainer>
        where TLayer : PrefabLayer<T, TContainer>, new()
        where TContainer : ValueContainer<T>, new()
    {
        [UsedImplicitly] // used by reflection
        public static void Impl(PrefabSafeList<T, TLayer, TContainer> self)
        {
            // fakeSlot must not be modified,
            if (self.OuterObject == null) return;

            // match prefabLayers count.
            var nestCount = PrefabSafeListUtil.PrefabNestCount(self.OuterObject);

            if (self.prefabLayers.Length == nestCount)
                RemoveCheckCheck(self, nestCount);
            else if (self.prefabLayers.Length < nestCount)
                self.prefabLayers = PrefabSafeListRuntimeUtil.ResizeArray(self.prefabLayers, nestCount);
            else if (self.prefabLayers.Length > nestCount)
                ApplyModificationsToLatestLayer(self, nestCount);
        }

        private static void RemoveCheckCheck(PrefabSafeList<T, TLayer, TContainer> self, int nestCount)
        {
            if (nestCount == 0)
            {
                self.firstLayer = self.GetAsList().Select(x => new TContainer { value = x }).ToArray();
            }
            else
            {
                var currentLayer = self.prefabLayers[nestCount - 1] ?? (self.prefabLayers[nestCount - 1] = new TLayer());
                
                var list = new List<T>();
                currentLayer.ApplyTo(list);
                currentLayer.elements = list.Select(x => new TContainer { value = x }).ToArray();
            }
        }

        private static void ApplyModificationsToLatestLayer(PrefabSafeList<T, TLayer, TContainer> self, int nestCount)
        {
            // after apply modifications?: apply to latest layer
            if (nestCount == 0)
            {
                self.firstLayer = self.GetAsList().Select(x => new TContainer { value = x }).ToArray();
                self.prefabLayers = Array.Empty<TLayer>();
            }
            else
            {
                // nestCount is not zero: apply to current layer

                var list = new List<T>();
                foreach (var layer in self.prefabLayers.Skip(nestCount - 1))
                    layer.ApplyTo(list);

                var currentLayer = self.prefabLayers[nestCount - 1] ?? (self.prefabLayers[nestCount - 1] = new TLayer());
                currentLayer.elements = list.Select(x => new TContainer { value = x }).ToArray();
                // resize layers array.
                self.prefabLayers = PrefabSafeListRuntimeUtil.ResizeArray(self.prefabLayers, nestCount);
            }
        }
    }
}
