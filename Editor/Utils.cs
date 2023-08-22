using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Anatawa12.ApplyOnPlay;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
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

        public static IEnumerable<Transform> GetAffectedTransforms(this VRCPhysBoneBase physBoneBase)
        {
            var ignores = new HashSet<Transform>(physBoneBase.ignoreTransforms);
            var queue = new Queue<Transform>();
            queue.Enqueue(physBoneBase.GetTarget());

            while (queue.Count != 0)
            {
                var transform = queue.Dequeue();
                yield return transform;

                foreach (var child in transform.DirectChildrenEnumerable())
                    if (!ignores.Contains(child))
                        queue.Enqueue(child);
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
        private const string OutputDirPath = "Assets/AvatarOptimizerOutput";

        public static void DeleteTemporalDirectory()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.DeleteAsset(TemporalDirPath);
            FileUtil.DeleteFileOrDirectory(TemporalDirPath);
        }

        [CanBeNull]
        public static DummyObject CreateOutputAssetFile(GameObject avatarGameObject, ApplyReason reason)
        {
            switch (reason)
            {
                case ApplyReason.EnteringPlayMode:
                    return ApplyOnPlayConfig.Generate ? CreateAssetFile() : null;
                case ApplyReason.ManualBake:
                default:
                    return CreateOutputAssetFile(avatarGameObject);
            }
        }

        public static DummyObject CreateAssetFile()
        {
            var obj = ScriptableObject.CreateInstance<DummyObject>();
            Directory.CreateDirectory(TemporalDirPath);
            AssetDatabase.CreateAsset(obj, $"{TemporalDirPath}/{GUID.Generate()}.asset");
            return obj;
        }

        public static DummyObject CreateOutputAssetFile(GameObject avatar)
        {
            var name = avatar.name;
            if (name.EndsWith("(Clone)", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "(Clone)".Length);
            return CreateOutputAssetFile(name);
        }

        public static DummyObject CreateOutputAssetFile(string name)
        {
            Directory.CreateDirectory(OutputDirPath);
            name = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            var path = GetUniqueFileName($"{OutputDirPath}/{name}", "asset");
            var obj = ScriptableObject.CreateInstance<DummyObject>();
            AssetDatabase.CreateAsset(obj, path);
            return obj;
        }

        private static string GetUniqueFileName(string name, string extension)
        {
            // TOCTOU is allowed for now
            string PathIfNotExists(string path) => File.Exists(path) || Directory.Exists(path) ? null : path;

            if (PathIfNotExists($"{name}.{extension}") is string firstTry) return firstTry;

            for (var number = 0; ; number++)
            {
                if (PathIfNotExists($"{name} ({number}).{extension}") is string otherTry) return otherTry;
            }
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

        [NotNull]
        public static GameObject GetGameObjectRelative([NotNull] GameObject rootObject, [NotNull] string path)
        {
            if (path == "") return rootObject;
            var cursor = rootObject.transform.Find(path);
            if (!cursor) throw new InvalidOperationException($"{path} not found");
            return cursor.gameObject;
        }

        // Properties detailed first and nothing last
        public static IEnumerable<(string prop, string rest)> FindSubPaths(string prop, char sep)
        {
            var rest = "";
            for (;;)
            {
                yield return (prop, rest);

                var index = prop.LastIndexOf(sep);
                if (index == -1) yield break;

                rest = prop.Substring(index) + rest;
                prop = prop.Substring(0, index);
            }
        }

        public static BoundsCornersEnumerable Corners(this Bounds bounds)
        {
            return new BoundsCornersEnumerable(bounds);
        }

        public struct DicCaster<TValueCasted>
        {
            public IReadOnlyDictionary<TKey, TValueCasted> CastedDic<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> self)
                where TValue : class, TValueCasted 
                => new CastedDictionary<TKey, TValue, TValueCasted>(self);
        }

        public static DicCaster<TValueCasted> CastDic<TValueCasted>()
            => new DicCaster<TValueCasted>();

        class CastedDictionary<TKey, TValue, TValueCasted> : IReadOnlyDictionary<TKey, TValueCasted>
            where TValue : class, TValueCasted
        {
            private readonly IReadOnlyDictionary<TKey, TValue> _base;
            public CastedDictionary(IReadOnlyDictionary<TKey, TValue> @base) => _base = @base;

            public IEnumerator<KeyValuePair<TKey, TValueCasted>> GetEnumerator() =>
                new Enumerator(_base.GetEnumerator());

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public int Count => _base.Count;
            public bool ContainsKey(TKey key) => _base.ContainsKey(key);
            public TValueCasted this[TKey key] => _base[key];
            public IEnumerable<TKey> Keys => _base.Keys;
            public IEnumerable<TValueCasted> Values => _base.Values;

            public bool TryGetValue(TKey key, out TValueCasted value)
            {
                var result = _base.TryGetValue(key, out var original);
                value = original;
                return result;
            }

            private class Enumerator : IEnumerator<KeyValuePair<TKey, TValueCasted>>
            {
                private IEnumerator<KeyValuePair<TKey, TValue>> _base;

                public Enumerator(IEnumerator<KeyValuePair<TKey, TValue>> @base) => _base = @base;
                public bool MoveNext() => _base.MoveNext();
                public void Reset() => _base.Reset();
                object IEnumerator.Current => Current;
                public void Dispose() => _base.Dispose();

                public KeyValuePair<TKey, TValueCasted> Current
                {
                    get
                    {
                        var (key, value) = _base.Current;
                        return new KeyValuePair<TKey, TValueCasted>(key, value);
                    }
                }
            }
        }

        private class EmptyDictionaryHolder<TKey, TValue>
        {
            public static readonly IReadOnlyDictionary<TKey, TValue> Empty =
                new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>());
        }

        public static IReadOnlyDictionary<TKey, TValue> EmptyDictionary<TKey, TValue>() =>
            EmptyDictionaryHolder<TKey, TValue>.Empty;

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair, out TKey key,
            out TValue value)
        {
            key = keyValuePair.Key;
            value = keyValuePair.Value;
        }

        public static IEnumerable<KeyValuePair<TKey, (TValue1, TValue2)>> ZipByKey<TKey, TValue1, TValue2>(
            this IReadOnlyDictionary<TKey, TValue1> first, IReadOnlyDictionary<TKey, TValue2> second)
        {
            foreach (var key in first.Keys.ToArray())
            {
                if (!second.TryGetValue(key, out var secondValue)) secondValue = default;

                yield return new KeyValuePair<TKey, (TValue1, TValue2)>(key, (first[key], secondValue));
            }

            foreach (var key in second.Keys.ToArray())
                if (!first.ContainsKey(key))
                    yield return new KeyValuePair<TKey, (TValue1, TValue2)>(key, (default, second[key]));
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

        public bool IsValid => _guid != null;

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

    internal readonly struct BoundsCornersEnumerable : IEnumerable<Vector3>
    {
        private readonly Bounds _bounds;

        public BoundsCornersEnumerable(Bounds bounds) => _bounds = bounds;

        public BoundsCornersEnumerator GetEnumerator() => new BoundsCornersEnumerator(_bounds);

        IEnumerator<Vector3> IEnumerable<Vector3>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal struct BoundsCornersEnumerator : IEnumerator<Vector3>
    {
        private readonly Bounds _bounds;
        // 0: before first
        // 1..=8: corner
        // 9: end
        private int _index;

        public BoundsCornersEnumerator(Bounds bounds) => (_bounds, _index) = (bounds, 0);

        public bool MoveNext() => ++_index <= 8;

        public void Reset() => _index = 0;

        public Vector3 Current
        {
            get
            {
                switch (_index)
                {
                    case 1:
                        return new Vector3(_bounds.min.x, _bounds.min.y, _bounds.min.z);
                    case 2:
                        return new Vector3(_bounds.min.x, _bounds.min.y, _bounds.max.z);
                    case 3:
                        return new Vector3(_bounds.min.x, _bounds.max.y, _bounds.min.z);
                    case 4:
                        return new Vector3(_bounds.min.x, _bounds.max.y, _bounds.max.z);
                    case 5:
                        return new Vector3(_bounds.max.x, _bounds.min.y, _bounds.min.z);
                    case 6:
                        return new Vector3(_bounds.max.x, _bounds.min.y, _bounds.max.z);
                    case 7:
                        return new Vector3(_bounds.max.x, _bounds.max.y, _bounds.min.z);
                    case 8:
                        return new Vector3(_bounds.max.x, _bounds.max.y, _bounds.max.z);
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }
    
    class AnimatorLayerMap<T>
    {
        private T[] _values = new T[(int)(VRCAvatarDescriptor.AnimLayerType.IKPose + 1)];

        public static bool IsValid(VRCAvatarDescriptor.AnimLayerType type)
        {
            switch (type)
            {
                case VRCAvatarDescriptor.AnimLayerType.Base:
                case VRCAvatarDescriptor.AnimLayerType.Additive:
                case VRCAvatarDescriptor.AnimLayerType.Gesture:
                case VRCAvatarDescriptor.AnimLayerType.Action:
                case VRCAvatarDescriptor.AnimLayerType.FX:
                case VRCAvatarDescriptor.AnimLayerType.Sitting:
                case VRCAvatarDescriptor.AnimLayerType.TPose:
                case VRCAvatarDescriptor.AnimLayerType.IKPose:
                    return true;
                case VRCAvatarDescriptor.AnimLayerType.Deprecated0:
                default:
                    return false;
            }
        }

        public ref T this[VRCAvatarDescriptor.AnimLayerType type]
        {
            get
            {
                if (!IsValid(type))
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);

                return ref _values[(int)type];
            }
        }
    }
}
