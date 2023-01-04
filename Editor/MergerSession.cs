using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.Merger
{
    internal class MergerSession
    {
        private readonly GameObject _rootObject;
        private readonly Dictionary<Object, Object> _mapping = new Dictionary<Object, Object>();
        private readonly List<Object> _toDestroy = new List<Object>();
        private readonly DummyObject _assetFileObject;

        public MergerSession(GameObject rootObject, bool addToAsset)
        {
            this._rootObject = rootObject;
            if (addToAsset)
            {
                _assetFileObject = Utils.CreateAssetFile();
            }
            else
            {
                _assetFileObject = null;
            }
        }

        public void AddObjectMapping<T>(T oldValue, T newValue) where T : Object => _mapping[oldValue] = newValue;
        internal Dictionary<Object, Object> GetMapping() => _mapping;

        public void Destroy(Object merge) => _toDestroy.Add(merge);
        internal List<Object> GetObjectsToDestroy() => _toDestroy;

        public IEnumerable<T> GetComponents<T>() where T : Component
        {
            return (_rootObject != null ? _rootObject.GetComponentsInChildren<T>(true) : Object.FindObjectsOfType<T>())
                .Where(x => x);
        }

        public T AddToAsset<T>(T obj) where T : Object
        {
            if (obj && _assetFileObject)
                AssetDatabase.AddObjectToAsset(obj, _assetFileObject);
            return obj;
        }
    }
}
