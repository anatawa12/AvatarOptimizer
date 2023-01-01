using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using VRC.Dynamics;

namespace Anatawa12.Merger
{
    internal static class Utils
    {
        public static ArraySerializedPropertyEnumerable AsEnumerable(this SerializedProperty property)
        {
            Assert.IsTrue(property.isArray);
            return new ArraySerializedPropertyEnumerable(property);
        }

        public static TransformParentEnumerable ParentEnumerable(this Transform transform) =>
            new TransformParentEnumerable(transform);

        public static TransformDirectChildrenEnumerable DirectChildrenEnumerable(this Transform transform) =>
            new TransformDirectChildrenEnumerable(transform);

        public static string RelativePath(Transform root, Transform child)
        {
            if (root == child) return "";

            var pathSegments = new List<string>();
            while (child != root)
            {
                pathSegments.Add(child.name);
                child = child.transform.parent;
                if (child == null) return null;
            }

            pathSegments.Reverse();
            return string.Join("/", pathSegments);
        }


        // func should returns false if nothing to return
        public static void WalkChildren(this Transform root, Func<Transform, bool> func)
        {
            var queue = new Queue<TransformDirectChildrenEnumerable.Enumerator>();
            var head = root.DirectChildrenEnumerable().GetEnumerator();
            for (;;)
            {
                while (!head.MoveNext())
                {
                    if (queue.Count == 0) return;
                    head = queue.Dequeue();
                }

                if (func(head.Current))
                {
                    queue.Enqueue(head);
                    head = head.Current.DirectChildrenEnumerable().GetEnumerator();
                }
            }
        }

        public static Transform GetTarget(this VRCPhysBoneBase physBoneBase) =>
            physBoneBase.rootTransform ? physBoneBase.rootTransform : physBoneBase.transform;

        public static void CopyDataFrom(this SerializedProperty property, SerializedProperty source)
        {
#if UNITY_2020_1_OR_NEWER
            boxedValue = targetProperty.boxedValue;
#else
            switch (property.propertyType)
            {
                // no support property
                //case SerializedPropertyType.Generic:
                //    break;
                case SerializedPropertyType.Integer:
                    property.intValue = source.intValue;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = source.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = source.floatValue;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = source.stringValue;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = source.colorValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = source.objectReferenceValue;
                    break;
                case SerializedPropertyType.LayerMask:
                    property.intValue = source.intValue;
                    break;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = source.enumValueIndex;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = source.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = source.vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = source.vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = source.rectValue;
                    break;
                case SerializedPropertyType.ArraySize:
                    property.intValue = source.intValue;
                    break;
                case SerializedPropertyType.Character:
                    property.intValue = source.intValue;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = source.animationCurveValue;
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = source.boundsValue;
                    break;
                // no ***Value property
                //case SerializedPropertyType.Gradient:
                //    property.gradientValue = source.gradientValue;
                //    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = source.quaternionValue;
                    break;
                case SerializedPropertyType.ExposedReference:
                    property.exposedReferenceValue = source.exposedReferenceValue;
                    break;
                // read-only
                //case SerializedPropertyType.FixedBufferSize:
                //    property.fixedBufferSize = source.fixedBufferSize;
                //    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = source.vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = source.vector3IntValue;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = source.rectIntValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = source.boundsIntValue;
                    break;
                // there's no getter property
                //case SerializedPropertyType.ManagedReference:
                //    property.managedReferenceValue = source.managedReferenceValue;
                //    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
#endif
        }

        public static GameObject NewGameObject(string name, Transform parent)
        {
            var rootObject = new GameObject(name);
            rootObject.transform.parent = parent;
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;
            return rootObject;
        }
    }

    internal struct ArraySerializedPropertyEnumerable : IEnumerable<SerializedProperty>
    {
        private readonly SerializedProperty _property;

        public ArraySerializedPropertyEnumerable(SerializedProperty property)
        {
            this._property = property;
        }

        Enumerator GetEnumerator() => new Enumerator(_property);

        private struct Enumerator : IEnumerator<SerializedProperty>
        {
            private int _index;
            private readonly SerializedProperty _property;

            public Enumerator(SerializedProperty property)
            {
                _index = -1;
                _property = property;
            }

            public bool MoveNext() => ++_index < _property.arraySize;

            public void Reset() => _index = -1;

            public SerializedProperty Current => _property.GetArrayElementAtIndex(_index);

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }

        IEnumerator<SerializedProperty> IEnumerable<SerializedProperty>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal readonly struct TransformParentEnumerable : IEnumerable<Transform>
    {
        private readonly Transform _transform;

        public TransformParentEnumerable(Transform transform)
        {
            
            _transform = transform;
        }

        public Enumerator GetEnumerator() => new Enumerator(_transform);
        IEnumerator<Transform> IEnumerable<Transform>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator: IEnumerator<Transform>
        {
            object IEnumerator.Current => Current;
            public Transform Current { get; private set; }
            private readonly Transform _initial;

            public Enumerator(Transform transform) => _initial = Current = transform;

            public bool MoveNext()
            {
                Current = Current != null ? Current.parent : null;
                return Current;
            }

            public void Reset() => Current = _initial;

            public void Dispose() {}
        }
    }
    
    internal readonly struct TransformDirectChildrenEnumerable : IEnumerable<Transform>
    {
        private readonly Transform _parent;

        public TransformDirectChildrenEnumerable(Transform parent)
        {
            
            _parent = parent;
        }

        public Enumerator GetEnumerator() => new Enumerator(_parent);
        IEnumerator<Transform> IEnumerable<Transform>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator: IEnumerator<Transform>
        {
            private int _index;
            private readonly Transform _parent;
            object IEnumerator.Current => Current;
            public Transform Current => _parent.GetChild(_index);

            public Enumerator(Transform parent) => (_index, _parent) = (-1, parent);

            public bool MoveNext() => ++_index < _parent.childCount;
            public void Reset() => _index = -1;
            public void Dispose() {}
        }
    }
}
