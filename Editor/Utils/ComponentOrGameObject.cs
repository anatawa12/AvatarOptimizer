using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    public readonly struct ComponentOrGameObject : IEquatable<ComponentOrGameObject>
    {
        private readonly Object _object;

        public ComponentOrGameObject(Object o)
        {
            if (!(o is null || o is Component || o is GameObject))
                throw new ArgumentException();
            _object = o;
        }

        private ComponentOrGameObject(Object o, int marker) => _object = o;

        public static implicit operator ComponentOrGameObject(GameObject gameObject) =>
            new ComponentOrGameObject(gameObject, 0);

        public static implicit operator ComponentOrGameObject(Component component) =>
            new ComponentOrGameObject(component, 0);

        public static implicit operator Object(ComponentOrGameObject componentOrGameObject) =>
            componentOrGameObject._object;

        // ReSharper disable InconsistentNaming
        // GameObject-like properties
        public GameObject gameObject => _object as GameObject ?? ((Component)_object).gameObject;
        public Transform transform => gameObject.transform;

        // ReSharper restore InconsistentNaming
        public Object Value => _object;

        public bool TryAs<T>(out T gameObject) where T : Object
        {
            gameObject = _object as T;
            return gameObject;
        }

        public bool Equals(ComponentOrGameObject other) => Equals(_object, other._object);
        public override bool Equals(object obj) => obj is ComponentOrGameObject other && Equals(other);
        public override int GetHashCode() => _object != null ? _object.GetHashCode() : 0;
    }
}