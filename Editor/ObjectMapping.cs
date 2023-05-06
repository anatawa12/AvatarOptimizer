using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// The class manages Object location mapping
    /// </summary>
    internal class ObjectMappingBuilder
    {
        // Those may not be an actual GameObject, may be a hierarchy root 'null'
        private readonly VGameObject _tree = new VGameObject(null, "<root gameobject>");

        public void RecordMoveObject(GameObject from, GameObject newParent)
        {
            var oldPath = Utils.RelativePath(null, from.transform);
            var newParentPath = Utils.RelativePath(null, newParent.transform);

            var oldVGameObject = _tree.GetGameObject(oldPath);
            var newParentVGameObject = _tree.GetGameObject(newParentPath);

            oldVGameObject.MoveTo(newParentVGameObject);
        }

        public void RecordMoveComponent(Component from, GameObject newGameObject)
        {
            var oldPath = Utils.RelativePath(null, from.transform);
            var newParentPath = Utils.RelativePath(null, newGameObject.transform);

            var component = _tree.GetGameObject(oldPath).GetComponent(from.GetType());
            var newParentVGameObject = _tree.GetGameObject(newParentPath);

            component.MoveTo(newParentVGameObject);
        }

        /// <summary> Represents a GameObject in Hierarchy </summary>
        class VGameObject
        {
            private readonly VGameObject _originalParent;
            private readonly string _originalName;
            private VGameObject _newParent;
            private string _newName;

            private readonly Dictionary<string, VGameObject> _originalChildren = new Dictionary<string, VGameObject>();
            private readonly Dictionary<string, List<VGameObject>> _newChildren = new Dictionary<string, List<VGameObject>>();

            private readonly Dictionary<Type, VComponent> _originalComponents = new Dictionary<Type, VComponent>();
            private readonly Dictionary<Type, List<VComponent>> _newComponents = new Dictionary<Type, List<VComponent>>();

            public VGameObject(VGameObject parent, string name)
            {
                _originalParent = _newParent = parent;
                _originalName = _newName = name;
            }

            private List<VComponent> GetComponents(Type type) =>
                _newComponents.TryGetValue(type, out var newList)
                    ? newList
                    : _newComponents[type] = new List<VComponent>();

            public VComponent GetComponent(Type type)
            {
                var list = GetComponents(type);
                if (list.Count == 0)
                    list.Add(_originalComponents[type] = new VComponent(this, type));
                return list[0];
            }

            public void MoveComponentTo(VComponent component, VGameObject newGameObject)
            {
                if (component.NewGameObject != this) throw new ArgumentException("bad newGameObject", nameof(component));
                var components = GetComponents(component.Type);
                Debug.Assert(components.Remove(component));
                newGameObject.GetComponents(component.Type).Add(component);
                component.NewGameObject = newGameObject;
            }

            public VGameObject GetGameObject([NotNull] string path)
            {
                if (path == null) throw new ArgumentNullException(nameof(path));

                var cursor = this;
                foreach (var pathComponent in path.Split('/'))
                {
                    var children = cursor.ChildListWithName(pathComponent);

                    if (children.Count == 0)
                    {
                        var child = new VGameObject(cursor, pathComponent);
                        cursor._originalChildren[pathComponent] = child;
                        children.Add(child);
                    }

                    cursor = children[0];
                }

                return cursor;
            }

            private List<VGameObject> ChildListWithName(string name) =>
                _newChildren.TryGetValue(name, out var newList)
                    ? newList
                    : _newChildren[name] = new List<VGameObject>();

            public void MoveTo(VGameObject newParent)
            {
                if (_newParent == newParent) return; // nothing to move
                var currentList = _newParent.ChildListWithName(_newName);
                var newList = newParent.ChildListWithName(_newName);
                System.Diagnostics.Debug.Assert(currentList.Remove(this));
                newList.Add(this);
                _newParent = newParent;
            }
        }

        /// <summary> Represents a component </summary>
        class VComponent
        {
            public readonly VGameObject OriginalGameObject;
            public VGameObject NewGameObject;
            public readonly Type Type;
            private readonly Dictionary<string, VProperty> _originalProperties = new Dictionary<string, VProperty>();
            private readonly Dictionary<string, VProperty> _newProperties = new Dictionary<string, VProperty>();

            public VComponent(VGameObject gameObject, Type type)
            {
                OriginalGameObject = NewGameObject = gameObject;
                Type = type;
            }

            public void MoveTo(VGameObject newGameObject) => NewGameObject.MoveComponentTo(this, newGameObject);
        }

        /// <summary> Represents a property </summary>
        class VProperty
        {
        }
    }
}
