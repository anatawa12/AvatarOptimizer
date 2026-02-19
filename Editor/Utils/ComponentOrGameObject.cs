#nullable disable

// TODO: consider nullable reference type
// This class may and may not be null so it's hard to determine if it's nullable or not
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    internal readonly struct ComponentOrGameObject : IEquatable<ComponentOrGameObject>
    {
        private readonly Object _object;

        public ComponentOrGameObject(Object o)
        {
            if (!(o is null || o is Component || o is GameObject))
                throw new ArgumentException("Argument is not null, Component, nor GameObject", nameof(o));
            _object = o;
        }

        private ComponentOrGameObject(Object o, int marker) => _object = o;

        public static implicit operator ComponentOrGameObject(GameObject gameObject) =>
            new ComponentOrGameObject(gameObject, 0);

        public static implicit operator ComponentOrGameObject(Component component) =>
            new ComponentOrGameObject(component, 0);

        public static implicit operator Object(ComponentOrGameObject componentOrGameObject) =>
            componentOrGameObject._object;

        public static implicit operator bool(ComponentOrGameObject componentOrGameObject) =>
            (Object)componentOrGameObject;

        // ReSharper disable InconsistentNaming
        // GameObject-like properties
        public GameObject gameObject => _object as GameObject ?? ((Component)_object).gameObject;
        public Transform transform => gameObject.transform;
        public int GetInstanceID() => ((Object)this).GetInstanceID();
        // ReSharper restore InconsistentNaming
        public Object Value => _object;

        public bool TryAs<T>(out T gameObject) where T : Object
        {
            gameObject = _object as T;
            return gameObject;
        }

        public bool Equals(ComponentOrGameObject other) => Equals(_object, other._object);
        public override bool Equals(object obj) => obj is ComponentOrGameObject other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_object);
        public override string ToString() => _object != null ? _object.ToString() : string.Empty;
    }
}
