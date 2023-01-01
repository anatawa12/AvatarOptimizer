using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.Merger
{
    internal class MergerSession
    {
        private readonly GameObject _rootObject;
        private readonly Dictionary<Object, Object> _mapping = new Dictionary<Object, Object>();
        private readonly List<Object> _toDestroy = new List<Object>();

        public MergerSession(GameObject rootObject)
        {
            this._rootObject = rootObject;
        }

        public void AddObjectMapping<T>(T oldValue, T newValue) where T : Object => _mapping[oldValue] = newValue;
        internal Dictionary<Object, Object> GetMapping() => _mapping;

        public void Destroy(Object merge) => _toDestroy.Add(merge);
        internal List<Object> GetObjectsToDestroy() => _toDestroy;

        public IEnumerable<T> GetComponents<T>() where T : Component
        {
            return _rootObject != null ? _rootObject.GetComponentsInChildren<T>() : Object.FindObjectsOfType<T>();
        }
    }
}
