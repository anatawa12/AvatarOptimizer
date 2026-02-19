using System;
using System.Collections;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection
{
    // PSUC: PrefabSafeUniqueCollection
    public static class PSUCUtil
    {
        public static int PrefabNestCount(Object instance)
        {
            var nestCount = 0;
            while ((bool)(instance = PrefabUtility.GetCorrespondingObjectFromSource(instance)))
                nestCount++;

            return nestCount;
        }

        public static bool ShouldUsePrefabOnSceneLayer(Object instance) =>
            PSUCRuntimeUtil.ShouldUsePrefabOnSceneLayer(instance);

        public static bool IsNullOrMissing<T>(this T self, Object context) =>
            self.IsNullOrMissing(new NullOrMissingContext(context));

        public static bool IsNullOrMissing<T>(this T self, NullOrMissingContext context)
        {
            if (default(T) != null) return false;

            if (self == null) return true;

            if (!(self is Object obj)) return false;

            if (obj == null) return true;

            if (obj is Component || obj is GameObject)
            {
                var contextPrefabAsset = context.IsPartOfPrefabAsset;
                var selfPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(obj);
                if (contextPrefabAsset != selfPrefabAsset) return true;

                if (selfPrefabAsset)
                {
                    // if it's prefab asset, check for root GameObject
                    var selfRoot = (obj is GameObject selfGo ? selfGo.transform : ((Component)obj).transform).root;
                    if (context.RootTransform != selfRoot) return true;
                }
            }

            return false;
        }

        public readonly struct NullOrMissingContext
        {
            internal Transform? RootTransform { get; }
            internal bool IsPartOfPrefabAsset => (object?)RootTransform != null;

            public NullOrMissingContext(Object context)
            {
                var contextPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(context);

                if (contextPrefabAsset)
                {
                    // if it's prefab asset, check for root GameObject
                    RootTransform = (context is GameObject go ? go.transform : ((Component)context).transform).root;
                }
                else
                {
                    RootTransform = null;
                }
            }
        }

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
                var srcEnd = src.GetEndProperty(true);
                var dstEnd = dst.GetEndProperty(true);
                var enterChildren = true;
                while (srcIter.Next(enterChildren) && !SerializedProperty.EqualContents(srcIter, srcEnd))
                {
                    var destCheck = dstIter.Next(enterChildren) && !SerializedProperty.EqualContents(dstIter, dstEnd);
                    Assert.IsTrue(destCheck);

                    //Debug.Log($"prop: {dstIter.propertyPath}: {dstIter.propertyType}");

                    switch (dstIter.propertyType)
                    {
                        case SerializedPropertyType.FixedBufferSize:
                            Assert.AreEqual(srcIter.fixedBufferSize, dstIter.fixedBufferSize);
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
                    Assert.IsFalse(destCheck);
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
                        throw new InvalidOperationException("unknown property type: " + dst.propertyType);
                }
            }
        }
    }

    internal readonly struct ArrayPropertyEnumerable : IEnumerable<SerializedProperty>
    {
        private readonly SerializedProperty _property;
        private readonly int _begin;
        private readonly int _end;

        public ArrayPropertyEnumerable(SerializedProperty property)
        {
            _property = property;
            _begin = 0;
            _end = property.arraySize;
        }

        private ArrayPropertyEnumerable(SerializedProperty property, int begin, int end)
        {
            _property = property;
            _begin = begin;
            _end = end;
        }

        public ArrayPropertyEnumerable Take(int count) =>
            new ArrayPropertyEnumerable(_property, _begin, Math.Min(_end, _begin + count));

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<SerializedProperty> IEnumerable<SerializedProperty>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<SerializedProperty>
        {
            private readonly SerializedProperty _property;
            private int _index;
            private int _size;

            public Enumerator(ArrayPropertyEnumerable enumerable)
            {
                _property = enumerable._property;
                _index = enumerable._begin - 1;
                _size = enumerable._end;
            }

            public SerializedProperty Current => _property.GetArrayElementAtIndex(_index);
            SerializedProperty IEnumerator<SerializedProperty>.Current => Current;
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                _index++;
                return _index < _size;
            }

            public void Reset() => throw new NotSupportedException();

            public void Dispose()
            {
            }
        }
    }
}
