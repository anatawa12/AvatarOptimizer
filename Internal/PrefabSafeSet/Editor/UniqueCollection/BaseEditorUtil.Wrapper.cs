using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection
{
    /// <summary>
    /// Utility to edit PrefabSafeSet in CustomEditor with SerializedProperty
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract partial class BaseEditorUtil<TAdditionValue, TRemoveKey>
    {
        private class Wrapper : BaseEditorUtil<TAdditionValue, TRemoveKey>
        {
            private BaseEditorUtil<TAdditionValue, TRemoveKey> _impl;

            private readonly SerializedProperty _property;
            private readonly Object _targetObject;
            private Object? _correspondingObject;

            private BaseEditorUtil<TAdditionValue, TRemoveKey> GetImpl()
            {
                var correspondingObject = PrefabUtility.GetCorrespondingObjectFromSource(_targetObject);
                if (correspondingObject != _correspondingObject)
                {
                    _impl = CreateImpl(_property, PSUCUtil.PrefabNestCount(_targetObject), _helper);
                    _correspondingObject = correspondingObject;
                }

                return _impl;
            }

            public Wrapper(SerializedProperty property,
                IEditorUtilHelper<TAdditionValue, TRemoveKey> helper) : base(helper)
            {
                _property = property;
                _targetObject = property.serializedObject.targetObject;
                _correspondingObject = PrefabUtility.GetCorrespondingObjectFromSource(_targetObject);
                var nestCount = PSUCUtil.PrefabNestCount(_targetObject);
                _impl = CreateImpl(property, nestCount, _helper);
            }

            public override IReadOnlyList<IBaseElement<TAdditionValue, TRemoveKey>> Elements => GetImpl().Elements;
            public override int ElementsCount => GetImpl().ElementsCount;
            public override int Count => GetImpl().Count;
            public override IEnumerable<TAdditionValue> Values => GetImpl().Values;
            public override void Clear() => GetImpl().Clear();
            public override bool HasPrefabOverride() => GetImpl().HasPrefabOverride();
            public override IBaseElement<TAdditionValue, TRemoveKey> Set(TAdditionValue value) => GetImpl().Set(value);
            public override IBaseElement<TAdditionValue, TRemoveKey> Add(TAdditionValue value) => GetImpl().Add(value);
            public override IBaseElement<TAdditionValue, TRemoveKey>? Remove(TRemoveKey key) => GetImpl().Remove(key);

            public override void HandleApplyRevertMenuItems(IBaseElement<TAdditionValue, TRemoveKey> element, GenericMenu genericMenu) =>
                GetImpl().HandleApplyRevertMenuItems(element, genericMenu);
        }
    }
}
