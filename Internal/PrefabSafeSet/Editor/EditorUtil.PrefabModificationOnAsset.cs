using System;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    public abstract partial class EditorUtil<T>
    {
        private sealed class PrefabModificationOnAsset : PrefabModificationBase
        {
            private bool _needsUpstreamUpdate;
            private int _upstreamElementCount;

            // upstream change check

            public PrefabModificationOnAsset(SerializedProperty property, int nestCount,
                Func<SerializedProperty, T> getValue, Action<SerializedProperty, T> setValue) : base(property, nestCount, getValue, setValue)
            {
            }

            protected override void InitCurrentLayer(bool force = false)
            {
                if (PrefabLayers.arraySize < NestCount && force)
                    PrefabLayers.arraySize = NestCount;

                if (PrefabLayers.arraySize < NestCount)
                {
                    CurrentRemoves = null;
                    CurrentAdditions = null;
                }
                else
                {
                    var currentLayer = PrefabLayers.GetArrayElementAtIndex(NestCount - 1);
                    CurrentRemoves = currentLayer.FindPropertyRelative(Names.Removes)
                                      ?? throw new InvalidOperationException("prefabLayers.removes not found");
                    CurrentAdditions = currentLayer.FindPropertyRelative(Names.Additions)
                                        ?? throw new InvalidOperationException("prefabLayers.additions not found");
                }
                _prefabLayersSize.Updated();
            }

            protected override void Normalize()
            {
                // TODO
            }

            protected override string[] GetAllowedPropertyPaths(int nestCount) => new[]
            {
                $"{Names.PrefabLayers}.Array.size",
                $"{Names.PrefabLayers}.Array.data[{nestCount - 1}]",
            };
        }
    }
}
