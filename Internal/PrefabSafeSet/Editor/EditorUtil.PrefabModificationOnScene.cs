using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    public abstract partial class EditorUtil<T>
    {
        private sealed class PrefabModificationOnScene : PrefabModificationBase
        {

            private SerializedProperty? _usingOnSceneLayerField;
            private SerializedProperty? _onSceneLayerField;

            private SerializedProperty _usingOnSceneLayer =>
                _usingOnSceneLayerField ??= RootProperty.FindPropertyRelative(Names.UsingOnSceneLayer);
            private SerializedProperty _onSceneLayer =>
                _onSceneLayerField ??= RootProperty.FindPropertyRelative(Names.OnSceneLayer);

            // upstream change check

            public PrefabModificationOnScene(SerializedProperty property, int nestCount,
                Func<SerializedProperty, T> getValue, Action<SerializedProperty, T> setValue) : base(property, nestCount, getValue, setValue)
            {
            }

            protected override void InitCurrentLayer(bool force = false)
            {
                // noop
                CurrentRemoves = _onSceneLayer.FindPropertyRelative(Names.Removes);
                CurrentAdditions = _onSceneLayer.FindPropertyRelative(Names.Additions);
                _prefabLayersSize.Updated();
            }

            protected override void Normalize()
            {
                if (PrefabLayers.arraySize < NestCount)
                {
                    // no problem
                }
                else if (PrefabLayers.arraySize == NestCount)
                {
                    // we should normalize to onSceneLayer
                    var currentLayer = PrefabLayers.GetArrayElementAtIndex(NestCount - 1);
                    _onSceneLayer.CopyDataFrom(currentLayer);
                    _usingOnSceneLayer.boolValue = true;
                    PrefabLayers.arraySize = NestCount - 1;
                }
                else
                {
                    // this should not happen; OnValidate will fix this
                    // TODO: normalize on this 
                }
            }

            protected override string[] GetAllowedPropertyPaths(int nestCount) => new[] 
            {
                Names.UsingOnSceneLayer,
                Names.OnSceneLayer,
            };
        }
    }
}
