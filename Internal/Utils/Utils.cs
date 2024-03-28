using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    public static partial class Utils
    {
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

        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (!component) component = go.AddComponent<T>();
            return component;
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

        public static GameObject NewGameObject(string name, GameObject[] children)
        {
            var rootObject = new GameObject(name);
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;
            foreach (var child in children)
                child.transform.parent = rootObject.transform;
            return rootObject;
        }

        public static GameObject NewGameObject(string name)
        {
            var rootObject = new GameObject(name);
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;
            return rootObject;
        }

        public static IEnumerable<(T, T)> ZipWithNext<T>(this IEnumerable<T> enumerable)
        {
            using (var enumerator = enumerable.GetEnumerator())
            {
                if (!enumerator.MoveNext()) yield break;
                var prev = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    yield return (prev, current);
                    prev = current;
                }
            }
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

        [CanBeNull]
        public static Type GetTypeFromName(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name))
                .FirstOrDefault(type => !(type == null));

        public static T DistinctSingleOrDefaultIfNoneOrMultiple<T>(this IEnumerable<T> enumerable)
        {
            using (var enumerator = enumerable.GetEnumerator())
            {
                if (!enumerator.MoveNext()) return default;
                var found = enumerator.Current;
                var eqOperator = EqualityComparer<T>.Default;

                while (enumerator.MoveNext())
                {
                    var nextValue = enumerator.Current;
                    if (!eqOperator.Equals(found, nextValue))
                        return default;
                }

                return found;
            }
        }

        public static T RemoveLast<T>([NotNull] this IList<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            var lastIndex = list.Count - 1;
            var last = list[lastIndex];
            list.RemoveAt(lastIndex);
            return last;
        }
    }
}
