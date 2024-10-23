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
        /// <summary>
        /// Returns an enumerable of all object reference properties in the serialized object.
        /// </summary>
        /// <param name="obj">The serialized object to enumerate.</param>
        /// <returns>An enumerable of all object reference properties in the serialized object.</returns>
        /// <remarks>
        /// Please note this may omit some object reference properties that will MonoScript be assigned.
        /// </remarks>
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
                    var result = MoveNextImpl();
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
                                if (_iterator.serializedObject.targetObject is AnimationClip)
                                {
                                    switch (_iterator.propertyPath)
                                    {
                                        // there will be many monoScript property for those types, but it's useless
                                        case "m_RotationCurves":
                                        case "m_CompressedRotationCurves":
                                        case "m_EulerCurves":
                                        case "m_PositionCurves":
                                        case "m_ScaleCurves":
                                        case "m_FloatCurves":

                                        case "m_EditorCurves":
                                        case "m_EulerEditorCurves":
                                            enterChildren = false;
                                            break;
                                    }
                                }
                                else if (_iterator.serializedObject.targetObject is ParticleSystem)
                                {
                                    // some ParticleSystem module doesn't have any object reference property,
                                    // but has many properties so it's better to skip them.
                                    switch (_iterator.propertyPath)
                                    {
                                        case "InitialModule":
                                        // case "ShapeModule": // has SMR / Renderer / Mesh reference
                                        case "EmissionModule":
                                        case "SizeModule":
                                        case "RotationModule":
                                        case "ColorModule":
                                        // case "UVModule": // has sprite reference
                                        case "VelocityModule":
                                        case "InheritVelocityModule":
                                        case "LifetimeByEmitterSpeedModule":
                                        case "ForceModule":
                                        case "ExternalForcesModule":
                                        case "ClampVelocityModule": 
                                        case "NoiseModule":
                                        case "SizeBySpeedModule":
                                        case "RotationBySpeedModule":
                                        case "ColorBySpeedModule":
                                        // case "CollisionModule": // has collider (transform) reference
                                        // case "TriggerModule": // has collider (component) reference
                                        // case "SubModule": // has ParticleSystem reference
                                        // case "LightsModule": // has light reference
                                        case "TrailModule":
                                        case "CustomDataModule":
                                            enterChildren = false;
                                            break;
                                    }
                                }
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
