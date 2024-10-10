using System;
using System.Linq;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection
{
    public abstract partial class BaseEditorUtil<TAdditionValue, TRemoveKey>
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
                IEditorUtilHelper<TAdditionValue, TRemoveKey> helper) : base(property, nestCount, helper)
            {
            }

            protected override void InitCurrentLayer(bool force = false)
            {
                if (force) _usingOnSceneLayer.boolValue = true;
                CurrentRemoves = _onSceneLayer.FindPropertyRelative(Names.Removes);
                CurrentAdditions = _onSceneLayer.FindPropertyRelative(Names.Additions);
                _prefabLayersSize.Updated();
            }

            readonly struct HelperManipulator : IManipulator<TAdditionValue, TRemoveKey>
            {
                private readonly IEditorUtilHelper<TAdditionValue, TRemoveKey> _helper;

                public HelperManipulator(IEditorUtilHelper<TAdditionValue, TRemoveKey> helper) => _helper = helper;

                public TRemoveKey? GetKey(TAdditionValue? value) =>
                    value == null ? default : _helper.GetRemoveKey(value);

                public ref TRemoveKey? GetKey(ref TAdditionValue? value) => throw new NotImplementedException();
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

                    var additions = new ListMap<TAdditionValue, TRemoveKey, HelperManipulator>(Array.Empty<TAdditionValue>(), new HelperManipulator(_helper));
                    var removes = new ListMap<TRemoveKey, TRemoveKey, IdentityManipulator<TRemoveKey>>(Array.Empty<TRemoveKey>(), default);

                    for (var i = NestCount - 1; i < PrefabLayers.arraySize; i++)
                    {
                        var currentLayer = PrefabLayers.GetArrayElementAtIndex(i);

                        var additionsArray = AdditionsToArray(currentLayer.FindPropertyRelative(Names.Additions));
                        var removesArray = RemoveKeysToArray(currentLayer.FindPropertyRelative(Names.Removes));

                        additions.RemoveRange(removesArray);
                        removes.AddRange(removesArray.NonNulls());

                        additions.AddRange(additionsArray);
                        removes.RemoveRange(additionsArray.NonNulls().Select(_helper.GetRemoveKey));
                    }

                    if (_usingOnSceneLayer.boolValue) {
                        var currentLayer = _onSceneLayer;
                        
                        var additionsArray = AdditionsToArray(currentLayer.FindPropertyRelative(Names.Additions));
                        var removesArray = RemoveKeysToArray(currentLayer.FindPropertyRelative(Names.Removes));

                        additions.RemoveRange(removesArray);
                        removes.AddRange(removesArray.NonNulls());

                        additions.AddRange(additionsArray);
                        removes.RemoveRange(additionsArray.NonNulls().Select(_helper.GetRemoveKey));
                    }

                    InitCurrentLayer(true);

                    PrefabLayers.arraySize = NestCount - 1;
                    SetArray(CurrentAdditions!, additions.ToArray(), _helper.WriteAdditionValue);
                    SetArray(CurrentRemoves!, removes.ToArray(), _helper.WriteRemoveKey);
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
