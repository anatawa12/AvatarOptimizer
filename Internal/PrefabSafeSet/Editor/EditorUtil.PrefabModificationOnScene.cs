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
                if (force) _usingOnSceneLayer.boolValue = true;
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
                else
                {
                    // this should not happen; OnValidate will fix this
                    // but unpacking prefab may cause this case

                    var additions = new ListSet<T>(Array.Empty<T>());
                    var removes = new ListSet<T>(Array.Empty<T>());

                    for (var i = NestCount - 1; i < PrefabLayers.arraySize; i++)
                    {
                        var currentLayer = PrefabLayers.GetArrayElementAtIndex(i);

                        var additionsArray = ToArray(currentLayer.FindPropertyRelative(Names.Additions));
                        var removesArray = ToArray(currentLayer.FindPropertyRelative(Names.Removes));

                        additions.RemoveRange(removesArray);
                        removes.AddRange(removesArray);

                        additions.AddRange(additionsArray);
                        removes.RemoveRange(additionsArray);
                    }

                    if (_usingOnSceneLayer.boolValue) {
                        var currentLayer = _onSceneLayer;
                        
                        var additionsArray = ToArray(currentLayer.FindPropertyRelative(Names.Additions));
                        var removesArray = ToArray(currentLayer.FindPropertyRelative(Names.Removes));

                        additions.RemoveRange(removesArray);
                        removes.AddRange(removesArray);

                        additions.AddRange(additionsArray);
                        removes.RemoveRange(additionsArray);
                    }

                    InitCurrentLayer(true);

                    PrefabLayers.arraySize = NestCount - 1;
                    SetArray(CurrentAdditions!, additions.ToArray());
                    SetArray(CurrentRemoves!, removes.ToArray());
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
