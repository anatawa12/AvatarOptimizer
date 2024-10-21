using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Anatawa12.AvatarOptimizer
{
    partial class Utils
    {
        public static ObjectReferencePropertiesEnumerable ObjectReferenceProperties(this SerializedObject obj)
            => new ObjectReferencePropertiesEnumerable(obj);

        public readonly struct ObjectReferencePropertiesEnumerable : IEnumerable<SerializedProperty>
        {
            private readonly SerializedObject _obj;

            public ObjectReferencePropertiesEnumerable(SerializedObject obj) => _obj = obj ?? throw new ArgumentNullException(nameof(obj));

            public Enumerator GetEnumerator() => new Enumerator(_obj);
            IEnumerator<SerializedProperty> IEnumerable<SerializedProperty>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<SerializedProperty>
            {
                private readonly SerializedProperty _iterator;

                public Enumerator(SerializedObject obj) => _iterator = obj.GetIterator();

                public bool MoveNext()
                {
                    Profiler.BeginSample($"ObjectReferencePropertiesEnumerable.MoveNext ({_iterator.serializedObject.targetObject.GetType().FullName})");
                    var result = MoveNextImpl();
                    if (result && Profiler.enabled)
                    {
                        // I think this is not best way to collect list of propertyPath with profiler
                        // but I can't find better way.
                        Profiler.BeginSample(_iterator.propertyPath);
                        Profiler.EndSample();
                    }
                    Profiler.EndSample();
                    return result;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private bool MoveNextImpl()
                {
                    while (true)
                    {
                        bool enterChildren;
                        switch (_iterator.propertyType)
                        {
                            case SerializedPropertyType.String:
                            case SerializedPropertyType.Integer:
                            case SerializedPropertyType.Boolean:
                            case SerializedPropertyType.Float:
                            case SerializedPropertyType.Color:
                            case SerializedPropertyType.ObjectReference:
                            case SerializedPropertyType.LayerMask:
                            case SerializedPropertyType.Enum:
                            case SerializedPropertyType.Vector2:
                            case SerializedPropertyType.Vector3:
                            case SerializedPropertyType.Vector4:
                            case SerializedPropertyType.Rect:
                            case SerializedPropertyType.ArraySize:
                            case SerializedPropertyType.Character:
                            case SerializedPropertyType.AnimationCurve:
                            case SerializedPropertyType.Bounds:
                            case SerializedPropertyType.Gradient:
                            case SerializedPropertyType.Quaternion:
                            case SerializedPropertyType.FixedBufferSize:
                            case SerializedPropertyType.Vector2Int:
                            case SerializedPropertyType.Vector3Int:
                            case SerializedPropertyType.RectInt:
                            case SerializedPropertyType.BoundsInt:
                                enterChildren = false;
                                break;
                            case SerializedPropertyType.Generic:
                            case SerializedPropertyType.ExposedReference:
                            case SerializedPropertyType.ManagedReference:
                            default:
                                enterChildren = true;
                                break;
                        }

                        if (!_iterator.Next(enterChildren)) return false;
                        if (_iterator.propertyType == SerializedPropertyType.ObjectReference)
                            return true;
                    }
                }

                public void Reset()
                {
                    var obj = _iterator.serializedObject;
                    Dispose();
                    this = new Enumerator(obj);
                }

                public SerializedProperty Current => _iterator;
                object IEnumerator.Current => Current;

                public void Dispose()
                {
                }
            }
        }
    }
}
