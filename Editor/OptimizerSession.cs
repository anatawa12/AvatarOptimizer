using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal class OptimizerSession
    {
        private readonly GameObject _rootObject;
        private readonly Dictionary<Object, Object> _mapping = new Dictionary<Object, Object>();
        private readonly List<Object> _toDestroy = new List<Object>();
        private readonly HashSet<Object> _added = new HashSet<Object>();
        private readonly DummyObject _assetFileObject;

        public OptimizerSession(GameObject rootObject, bool addToAsset)
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

        public T GetRootComponent<T>() where T : Component
        {
            return _rootObject != null ? _rootObject.GetComponent<T>() : null;
        }

        public IEnumerable<T> GetComponents<T>() where T : Component
        {
            return (_rootObject != null ? _rootObject.GetComponentsInChildren<T>(true) : Object.FindObjectsOfType<T>())
                .Where(x => x)
                .Where(x => !_toDestroy.Contains(x));
        }

        public T AddToAsset<T>(T obj) where T : Object
        {
            if (obj)
            {
                // already added to some asset
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj))) return obj;
                _added.Add(obj);
                if (_assetFileObject)
                    AssetDatabase.AddObjectToAsset(obj, _assetFileObject);
            }
            return obj;
        }

        public T MayInstantiate<T>(T obj) where T : Object =>
            _added.Contains(obj) ? obj : AddToAsset(Object.Instantiate(obj));

        public void MarkDirtyAll()
        {
            foreach (var o in _added)
                EditorUtility.SetDirty(o);
        }
    }
}
