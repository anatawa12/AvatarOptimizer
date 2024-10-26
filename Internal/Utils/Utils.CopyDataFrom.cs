using System;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    partial class Utils
    {
        // based on https://gist.github.com/anatawa12/16fbf529c8da4a0fb993c866b1d86512
        public static void CopyDataFrom(this SerializedProperty dest, SerializedProperty source)
        {
            if (dest.propertyType == SerializedPropertyType.Generic)
                CopyBetweenTwoRecursively(source, dest);
            else
                CopyBetweenTwoValue(source, dest);


            void CopyBetweenTwoRecursively(SerializedProperty src, SerializedProperty dst)
            {
                var srcIter = src.Copy();
                var dstIter = dst.Copy();
                var srcEnd = src.GetEndProperty();
                var dstEnd = dst.GetEndProperty();
                var enterChildren = true;
                while (srcIter.Next(enterChildren) && !SerializedProperty.EqualContents(srcIter, srcEnd))
                {
                    var destCheck = dstIter.Next(enterChildren) && !SerializedProperty.EqualContents(dstIter, dstEnd);
                    Utils.Assert(destCheck);

                    //Debug.Log($"prop: {dstIter.propertyPath}: {dstIter.propertyType}");

                    switch (dstIter.propertyType)
                    {
                        case SerializedPropertyType.FixedBufferSize:
                            Utils.Assert(srcIter.fixedBufferSize == dstIter.fixedBufferSize);
                            break;
                        case SerializedPropertyType.Generic:
                            break;
                        default:
                            CopyBetweenTwoValue(srcIter, dstIter);
                            break;
                    }

                    enterChildren = dstIter.propertyType == SerializedPropertyType.Generic;
                }

                {
                    var destCheck = dstIter.NextVisible(enterChildren) &&
                                    !SerializedProperty.EqualContents(dstIter, dstEnd);
                    Utils.Assert(!destCheck);
                }
            }

            void CopyBetweenTwoValue(SerializedProperty src, SerializedProperty dst)
            {
                switch (dst.propertyType)
                {
                    case SerializedPropertyType.Generic:
                        throw new InvalidOperationException("for generic, use CopyBetweenTwoRecursively");
                    case SerializedPropertyType.Integer:
                        dst.intValue = src.intValue;
                        break;
                    case SerializedPropertyType.Boolean:
                        dst.boolValue = src.boolValue;
                        break;
                    case SerializedPropertyType.Float:
                        dst.floatValue = src.floatValue;
                        break;
                    case SerializedPropertyType.String:
                        dst.stringValue = src.stringValue;
                        break;
                    case SerializedPropertyType.Color:
                        dst.colorValue = src.colorValue;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        dst.objectReferenceValue = src.objectReferenceValue;
                        break;
                    case SerializedPropertyType.LayerMask:
                        dst.intValue = src.intValue;
                        break;
                    case SerializedPropertyType.Enum:
                        dst.intValue = src.intValue;
                        break;
                    case SerializedPropertyType.Vector2:
                        dst.vector2Value = src.vector2Value;
                        break;
                    case SerializedPropertyType.Vector3:
                        dst.vector3Value = src.vector3Value;
                        break;
                    case SerializedPropertyType.Vector4:
                        dst.vector4Value = src.vector4Value;
                        break;
                    case SerializedPropertyType.Rect:
                        dst.rectValue = src.rectValue;
                        break;
                    case SerializedPropertyType.ArraySize:
                        dst.intValue = src.intValue;
                        break;
                    case SerializedPropertyType.Character:
                        dst.intValue = src.intValue;
                        break;
                    case SerializedPropertyType.AnimationCurve:
                        dst.animationCurveValue = src.animationCurveValue;
                        break;
                    case SerializedPropertyType.Bounds:
                        dst.boundsValue = src.boundsValue;
                        break;
                    case SerializedPropertyType.Gradient:
                        //dst.gradientValue = src.gradientValue;
                        //break;
                        throw new InvalidOperationException("unsupported type: Gradient");
                    case SerializedPropertyType.Quaternion:
                        dst.quaternionValue = src.quaternionValue;
                        break;
                    case SerializedPropertyType.ExposedReference:
                        dst.exposedReferenceValue = src.exposedReferenceValue;
                        break;
                    case SerializedPropertyType.FixedBufferSize:
                        throw new InvalidOperationException("unsupported type: FixedBufferSize");
                    case SerializedPropertyType.Vector2Int:
                        dst.vector2IntValue = src.vector2IntValue;
                        break;
                    case SerializedPropertyType.Vector3Int:
                        dst.vector3IntValue = src.vector3IntValue;
                        break;
                    case SerializedPropertyType.RectInt:
                        dst.rectIntValue = src.rectIntValue;
                        break;
                    case SerializedPropertyType.BoundsInt:
                        dst.boundsIntValue = src.boundsIntValue;
                        break;
                    case SerializedPropertyType.ManagedReference:
                        throw new InvalidOperationException("unsupported type: ManagedReference");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
