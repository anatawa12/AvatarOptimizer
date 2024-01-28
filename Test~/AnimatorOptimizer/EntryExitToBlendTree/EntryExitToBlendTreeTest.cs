using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    public class EntryExitToBlendTreeTest : AnimatorOptimizerTestBase
    {
        [Test]
        public void GestureConvertibleSimple()
        {
            var controller = LoadCloneAnimatorController("GestureConvertibleSimple");
            controller.name = "GestureConvertibleSimple.converted";
            EntryExitToBlendTree.Execute(new AOAnimatorController(controller));
            var except = LoadAnimatorController("GestureConvertibleSimple.converted");
            CheckEqualityContext.CheckEquality(except, controller);
        }

        private struct CheckEqualityContext
        {
            public static void CheckEquality(Object except, Object actual)
            {
                var context = new CheckEqualityContext();
                context._mapping = new Dictionary<Object, Object>();
                context._objectPath = new List<string>();
                context.CheckEqualityImpl(except, actual);
            }

            private Dictionary<Object, Object> _mapping;
            private List<string> _objectPath;

            public void CheckEqualityImpl(Object except, Object actual)
            {
                if (_mapping.TryGetValue(except, out var mapped))
                {
                    Check("(root)", "different object mapping", mapped, actual);
                    return;
                }

                _mapping.Add(except, actual);

                using (var exceptSerialized = new SerializedObject(except))
                using (var actualSerialized = new SerializedObject(actual))
                {
                    var expectIterator = exceptSerialized.GetIterator();
                    var actualIterator = actualSerialized.GetIterator();
                    var enterChildren = true;

                    for (;;)
                    {
                        if (!expectIterator.Next(enterChildren)) break;
                        if (!actualIterator.Next(enterChildren)) break;

                        Check(expectIterator.propertyPath, "property path mismatch",
                            expectIterator.propertyPath, actualIterator.propertyPath);

                        Check(expectIterator.propertyPath, "property type mismatch",
                            expectIterator.propertyType, actualIterator.propertyType);

                        if (ShouldCheckProperty(except, expectIterator.propertyPath))
                            PropertyEquality(expectIterator, actualIterator);

                        enterChildren = expectIterator.propertyType == SerializedPropertyType.Generic;
                    }
                }

            }

            private static readonly Regex ControllerInParameterRegex =
                new Regex(@"m_AnimatorParameters\.Array\.data\[\d+\]\.m_Controller");
            static bool ShouldCheckProperty(Object except, string propertyPath)
            {
                if (propertyPath == "m_ObjectHideFlags") return false;
                if (except is AnimatorController && ControllerInParameterRegex.IsMatch(propertyPath))
                    return false;
                return true;
            }

            void Check(
                string propertyPath,
                string message,
                object expectedValue,
                object actualValue)
            {
                if (Equals(expectedValue, actualValue)) return;
                throw NewException($"{message} at {propertyPath}.\n" +
                                   $"expected: {expectedValue}\n" +
                                   $"actual: {actualValue}");
            }

            AssertionException NewException(string message)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine(message);
                if (_objectPath.Count == 0)
                {
                    builder.AppendLine("in root project");
                }
                else
                {
                    builder.AppendLine();
                    foreach (var s in _objectPath)
                        builder.AppendLine($"in object: {s}");
                }

                return new AssertionException(builder.ToString());
            }

            void PropertyEquality(SerializedProperty expectIterator, SerializedProperty actualIterator)
            {
                switch (expectIterator.propertyType)
                {
                    case SerializedPropertyType.Generic:
                        // reqursively
                        break;
                    case SerializedPropertyType.ObjectReference:
                        var expectObject = expectIterator.objectReferenceValue;
                        var actualObject = actualIterator.objectReferenceValue;
                        if (expectObject == null && actualObject == null) break;
                        if (expectObject == null || actualObject == null)
                            Check(expectIterator.propertyPath, $"object reference mismatch",
                                expectObject, actualObject);
                        Check(expectIterator.propertyPath, "object reference type mismatch",
                            expectObject.GetType(), actualObject.GetType());

                        switch (expectObject)
                        {
                            // for some types, check instance equality
                            case AnimationClip _:
                            case MonoScript _:
                                Check(expectIterator.propertyPath, "object reference mismatch",
                                    expectObject, actualObject);
                                break;
                            // for other types, check equality by serialized data
                            default:
                                _objectPath.Add(expectIterator.propertyPath);
                                CheckEqualityImpl(expectIterator.objectReferenceValue, actualIterator.objectReferenceValue);
                                _objectPath.RemoveAt(_objectPath.Count - 1);
                                break;
                        }

                        break;
                    case SerializedPropertyType.ArraySize:
                    case SerializedPropertyType.Integer:
                        Check(expectIterator.propertyPath, "int value mismatch",
                            expectIterator.longValue, actualIterator.longValue);
                        break;
                    case SerializedPropertyType.Boolean:
                        Check(expectIterator.propertyPath, "bool value mismatch",
                            expectIterator.boolValue, actualIterator.boolValue);
                        break;
                    case SerializedPropertyType.Float:
                        Check(expectIterator.propertyPath, "float value mismatch",
                            expectIterator.floatValue, actualIterator.floatValue);
                        break;
                    case SerializedPropertyType.String:
                        Check(expectIterator.propertyPath, "string value mismatch",
                            expectIterator.stringValue, actualIterator.stringValue);
                        break;
                    case SerializedPropertyType.Color:
                        Check(expectIterator.propertyPath, "color value mismatch",
                            expectIterator.colorValue, actualIterator.colorValue);
                        break;
                    case SerializedPropertyType.LayerMask:
                        Check(expectIterator.propertyPath, "layer mask value mismatch",
                            expectIterator.intValue, actualIterator.intValue);
                        break;
                    case SerializedPropertyType.Enum:
                        Check(expectIterator.propertyPath, "enum value mismatch",
                            expectIterator.enumValueIndex, actualIterator.enumValueIndex);
                        break;
                    case SerializedPropertyType.Vector2:
                        Check(expectIterator.propertyPath, "vector2 value mismatch",
                            expectIterator.vector2Value, actualIterator.vector2Value);
                        break;
                    case SerializedPropertyType.Vector3:
                        Check(expectIterator.propertyPath, "vector3 value mismatch",
                            expectIterator.vector3Value, actualIterator.vector3Value);
                        break;
                    case SerializedPropertyType.Vector4:
                        Check(expectIterator.propertyPath, "vector4 value mismatch",
                            expectIterator.vector4Value, actualIterator.vector4Value);
                        break;
                    case SerializedPropertyType.Rect:
                        Check(expectIterator.propertyPath, "rect value mismatch",
                            expectIterator.rectValue, actualIterator.rectValue);
                        break;
                    case SerializedPropertyType.Character:
                        Check(expectIterator.propertyPath, "character value mismatch",
                            expectIterator.intValue, actualIterator.intValue);
                        break;
                    case SerializedPropertyType.AnimationCurve:
                        Check(expectIterator.propertyPath, "animation curve value mismatch",
                            expectIterator.animationCurveValue, actualIterator.animationCurveValue);
                        break;
                    case SerializedPropertyType.Bounds:
                        Check(expectIterator.propertyPath, "bounds value mismatch",
                            expectIterator.boundsValue, actualIterator.boundsValue);
                        break;
                    case SerializedPropertyType.Quaternion:
                        Check(expectIterator.propertyPath, "quaternion value mismatch",
                            expectIterator.quaternionValue, actualIterator.quaternionValue);
                        break;
                    case SerializedPropertyType.FixedBufferSize:
                        Check(expectIterator.propertyPath, "fixed buffer size value mismatch",
                            expectIterator.fixedBufferSize, actualIterator.fixedBufferSize);
                        break;
                    case SerializedPropertyType.Vector2Int:
                        Check(expectIterator.propertyPath, "vector2 int value mismatch",
                            expectIterator.vector2IntValue, actualIterator.vector2IntValue);
                        break;
                    case SerializedPropertyType.Vector3Int:
                        Check(expectIterator.propertyPath, "vector3 int value mismatch",
                            expectIterator.vector3IntValue, actualIterator.vector3IntValue);
                        break;
                    case SerializedPropertyType.RectInt:
                        Check(expectIterator.propertyPath, "rect int value mismatch",
                            expectIterator.rectIntValue, actualIterator.rectIntValue);
                        break;
                    case SerializedPropertyType.BoundsInt:
                        Check(expectIterator.propertyPath, "bounds int value mismatch",
                            expectIterator.boundsIntValue, actualIterator.boundsIntValue);
                        break;
                    case SerializedPropertyType.ExposedReference:
                    case SerializedPropertyType.Gradient:
                    case SerializedPropertyType.ManagedReference:
                    default:
                        throw NewException($"{expectIterator.propertyType} is not supported at " +
                                           expectIterator.propertyPath);
                }
            }
        }
    }
}
