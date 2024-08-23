using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    public struct CachedGuidLoader<T> where T : Object
    {
        private readonly string _guid;
        private T? _cached;

        public CachedGuidLoader(string guid)
        {
            _guid = guid;
            _cached = null;
        }

        public T Value =>
            _cached != null
                ? _cached
                : _cached =
                    AssetDatabase.LoadAssetAtPath<T>(
                        AssetDatabase.GUIDToAssetPath(_guid));

        public bool IsValid => _guid != null;

        public static implicit operator CachedGuidLoader<T>(string guid) => new(guid);
    }
}
