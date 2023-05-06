using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using VRC.Dynamics;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    internal static class Utils
    {
        private static CachedGuidLoader<Shader> _toonLitShader = "affc81f3d164d734d8f13053effb1c5c";
        public static Shader ToonLitShader => _toonLitShader.Value;
        
        private static CachedGuidLoader<Shader> _mergeTextureHelper = "2d4f01f29e91494bb5eafd4c99153ab0";
        public static Shader MergeTextureHelper => _mergeTextureHelper.Value;

        private static CachedGuidLoader<Texture2D> _previewHereTex = "617775211fe634657ae06fc9f81b6ceb";
        public static Texture2D PreviewHereTex => _previewHereTex.Value;

        public static void HorizontalLine(bool marginTop = true, bool marginBottom = true)
        {
            const float margin = 17f / 2;
            var maxHeight = 1f;
            if (marginTop) maxHeight += margin;
            if (marginBottom) maxHeight += margin;

            var rect = GUILayoutUtility.GetRect(
                EditorGUIUtility.fieldWidth, float.MaxValue, 
                1, maxHeight, GUIStyle.none);
            if (marginTop && marginBottom)
                rect.y += rect.height / 2 - 0.5f;
            else if (marginTop)
                rect.y += rect.height - 1f;
            else if (marginBottom)
                rect.y += 0;
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        public static ArraySerializedPropertyEnumerable AsEnumerable(this SerializedProperty property)
        {
            Assert.IsTrue(property.isArray);
            return new ArraySerializedPropertyEnumerable(property);
        }

        public static TransformParentEnumerable ParentEnumerable(this Transform transform) =>
            new TransformParentEnumerable(transform);

        public static TransformDirectChildrenEnumerable DirectChildrenEnumerable(this Transform transform) =>
            new TransformDirectChildrenEnumerable(transform);

        public static void FlattenMapping<T>(this Dictionary<T, T> self)
        {
            foreach (var key in self.Keys.ToArray())
            {
                var value = self[key];
                while (value != null && self.TryGetValue(value, out var mapped))
                    value = mapped;
                self[key] = value;
            }
        }

        [ContractAnnotation("root:null => notnull")]
        [ContractAnnotation("root:notnull => canbenull")]
        public static string RelativePath(Transform root, Transform child)
        {
            if (root == child) return "";

            var pathSegments = new List<string>();
            while (child != root)
            {
                if (child == null) return null;
                pathSegments.Add(child.name);
                child = child.transform.parent;
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

        // this is same as .Distinct().Count() however Optimized for int with range 0-X with BitArray
        public static int DistinctCountIntWithUpperLimit(this IEnumerable<int> self, int upperLimit)
        {
            var exists = new BitArray(upperLimit);
            var count = 0;
            foreach (var i in self)
            {
                if (exists[i]) continue;
                exists[i] = true;
                count++;
            }

            return count;
        }

        public static int CountFalse(this BitArray self) => self.Count - self.CountTrue();

        public static int CountTrue(this BitArray self)
        {
            var ints = new int[(self.Count >> 5) + 1];
            self.CopyTo(ints, 0);
            var count = 0;

            // fix for not truncated bits in last integer that may have been set to true with SetAll()
            ints[ints.Length - 1] &= ~(-1 << (self.Count % 32));

            foreach (var t in ints)
            {
                uint c = (uint)t;

                // magic (http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel)
                unchecked
                {
                    c = ((c >> 0) & 0xFFFFFFFF) - ((c >> 1) & 0x55555555);
                    c = ((c >> 2) & 0x33333333) + (c & 0x33333333);
                    c = ((c >> 4) + c) & 0x0F0F0F0F;
                    c = ((c >> 8) + c) & 0x00FF00FF;
                    c = ((c >> 16) + c) & 0x0000FFFF;
                }

                count += (int) c;
            }

            return count;
        }

        public static void FillArray<T>(T[] array, T value)
        {
            for (var i = 0; i < array.Length; i++)
                array[i] = value;
        }

        public static Transform GetTarget(this VRCPhysBoneBase physBoneBase) =>
            physBoneBase.rootTransform ? physBoneBase.rootTransform : physBoneBase.transform;

        
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
                        throw new ArgumentOutOfRangeException();
                }
            }
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

        private const string TemporalDirPath = "Assets/9999-OptimizerGeneratedTemporalAssets";

        public static void DeleteTemporalDirectory()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.DeleteAsset(TemporalDirPath);
            FileUtil.DeleteFileOrDirectory(TemporalDirPath);
        }

        public static DummyObject CreateAssetFile()
        {
            var obj = ScriptableObject.CreateInstance<DummyObject>();
            Directory.CreateDirectory(TemporalDirPath);
            AssetDatabase.CreateAsset(obj, $"{TemporalDirPath}/{GUID.Generate()}.asset");
            return obj;
        }

        public static ZipWithNextEnumerable<T> ZipWithNext<T>(this IEnumerable<T> enumerable) =>
            new ZipWithNextEnumerable<T>(enumerable);

        public static NativeArray<T> SliceNativeArray<T>(NativeArray<T> source, int length, Allocator allocator)
            where T : unmanaged
        {
            var res = new NativeArray<T>(length, allocator);
            source.AsReadOnlySpan().Slice(0, length).CopyTo(res.AsSpan());
            return res;
        }

        public static AssetEditingScope StartEditingScope(bool saveAssets) => AssetEditingScope.Start(saveAssets);

        internal readonly struct AssetEditingScope : IDisposable
        {
            // 0: default: skip stop
            // 1: stop asset editing
            // 2: stop editing & save assets
            private readonly int _flags;

            private AssetEditingScope(int flags)
            {
                _flags = flags;
            }

            public static AssetEditingScope Start(bool saveAssets)
            {
                AssetDatabase.StartAssetEditing();
                return new AssetEditingScope(1 | (saveAssets ? 2 : 0));
            }

            public void Dispose()
            {
                if ((_flags & 1) != 0) AssetDatabase.StopAssetEditing();
                if ((_flags & 2) != 0) AssetDatabase.SaveAssets();
            }
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

    internal struct CachedGuidLoader<T> where T : Object
    {
        private readonly string _guid;
        private T _cached;

        public CachedGuidLoader(string guid)
        {
            _guid = guid;
            _cached = null;
        }

        public T Value =>
            _cached
                ? _cached
                : _cached =
                    AssetDatabase.LoadAssetAtPath<T>(
                        AssetDatabase.GUIDToAssetPath(_guid));

        public static implicit operator CachedGuidLoader<T>(string guid) =>
            new CachedGuidLoader<T>(guid);
    }
    
    internal struct ZipWithNextEnumerable<T> : IEnumerable<(T, T)>
    {
        private readonly IEnumerable<T> _enumerable;

        public ZipWithNextEnumerable(IEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
        }

        Enumerator GetEnumerator() => new Enumerator(_enumerable.GetEnumerator());
        IEnumerator<(T, T)> IEnumerable<(T, T)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<(T, T)>
        {
            private readonly IEnumerator<T> _enumerator;
            private (T, T) _current;
            private bool _first;

            public Enumerator(IEnumerator<T> enumerator)
            {
                _enumerator = enumerator;
                _current = default;
                _first = true;
            }

            public bool MoveNext()
            {
                if (_first)
                {
                    if (!_enumerator.MoveNext()) return false;
                    _current = (default, _enumerator.Current);
                    _first = false;
                }
                if (!_enumerator.MoveNext()) return false;
                _current = (_current.Item2, _enumerator.Current);
                return true;
            }

            public void Reset()
            {
                _enumerator.Reset();
                _first = false;
            }

            public (T, T) Current => _current;
            object IEnumerator.Current => Current;
            public void Dispose() => _enumerator.Dispose();
        }
    }
}
