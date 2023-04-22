using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    public static class PrefabSafeSetUtil
    {
        public static int PrefabNestCount(Object instance)
        {
            var nestCount = 0;
            while ((bool)(instance = PrefabUtility.GetCorrespondingObjectFromSource(instance)))
                nestCount++;

            return nestCount;
        }

        public static bool IsNullOrMissing<T>(this T self, NullOrMissingContext context)
        {
            return context.IsNullOrMissing(self);
        }

        public readonly struct NullOrMissingContext
        {
            private readonly Transform _rootTransform;
            private readonly bool _isPartOfPrefabAsset;

            public NullOrMissingContext(Object context)
            {
                _isPartOfPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(context);

                var prefab = _isPartOfPrefabAsset ? context : PrefabUtility.GetCorrespondingObjectFromSource(context);
                _rootTransform = !prefab
                    ? null
                    : (prefab is GameObject go ? go.transform : ((Component)prefab).transform).root;
            }
            
            public bool IsNullOrMissing<T>(T self)
            {
                if (default(T) != null) return false;

                if (self == null) return true;

                if (!(self is Object obj)) return false;

                if (obj == null) return true;

                if (obj is Component || obj is GameObject)
                {
                    var contextPrefabAsset = _isPartOfPrefabAsset;
                    var selfPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(obj);

                    // For PrefabAsset, non-prefab value is not allowed: assume as null
                    if (contextPrefabAsset && !selfPrefabAsset) return true;
                    // If both are scene asset, it's ok: not null
                    if (!contextPrefabAsset && !selfPrefabAsset) return false;

                    System.Diagnostics.Debug.Assert(selfPrefabAsset);

                    var selfRoot = (obj is GameObject selfGo ? selfGo.transform : ((Component)obj).transform).root;

                    // for Scene GameObject, prefab with same root is the only allowed
                    // if it's prefab asset, check for root GameObject
                    if (_rootTransform != selfRoot) return true;
                }

                return false;
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
