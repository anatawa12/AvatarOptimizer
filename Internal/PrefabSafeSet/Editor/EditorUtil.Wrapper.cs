using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    /// <summary>
    /// Utility to edit PrefabSafeSet in CustomEditor with SerializedProperty
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract partial class EditorUtil<T> where T : notnull
    {
        private class Wrapper : EditorUtil<T>
        {
            private EditorUtil<T> _impl;

            private readonly SerializedProperty _property;
            private readonly Object _targetObject;
            private Object? _correspondingObject;

            private EditorUtil<T> GetImpl()
            {
                var correspondingObject = PrefabUtility.GetCorrespondingObjectFromSource(_targetObject);
                if (correspondingObject != _correspondingObject)
                {
                    _impl = CreateImpl(_property, PrefabSafeSetUtil.PrefabNestCount(_targetObject), _getValue,
                        _setValue);
                    _correspondingObject = correspondingObject;
                }

                return _impl;
            }

            public Wrapper(SerializedProperty property,
                Func<SerializedProperty, T> getValue,
                Action<SerializedProperty, T> setValue) : base(getValue, setValue)
            {
                _property = property;
                _targetObject = property.serializedObject.targetObject;
                _correspondingObject = PrefabUtility.GetCorrespondingObjectFromSource(_targetObject);
                var nestCount = PrefabSafeSetUtil.PrefabNestCount(_targetObject);
                _impl = CreateImpl(property, nestCount, getValue, setValue);
            }

            public override IReadOnlyList<IElement<T>> Elements => GetImpl().Elements;
            public override int ElementsCount => GetImpl().ElementsCount;
            public override int Count => GetImpl().Count;
            public override IEnumerable<T> Values => GetImpl().Values;
            public override void Clear() => GetImpl().Clear();
            protected override IElement<T> NewSlotElement(T value) => GetImpl().NewSlotElement(value);
            public override bool HasPrefabOverride() => GetImpl().HasPrefabOverride();

            public override void HandleApplyRevertMenuItems(IElement<T> element, GenericMenu genericMenu) =>
                GetImpl().HandleApplyRevertMenuItems(element, genericMenu);
        }
    }
}
