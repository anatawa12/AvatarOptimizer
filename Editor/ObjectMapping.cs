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
        private readonly GameObject _rootObject;

        public ObjectMappingBuilder([CanBeNull] GameObject rootObject)
        {
            _rootObject = rootObject;
        }

        public void RecordMoveObject(GameObject from, GameObject newParent)
        {
            var oldPath = Utils.RelativePath(_rootObject.transform, from.transform)
                          ?? throw new ArgumentException("not of root GameObject", nameof(from));
            var newParentPath = Utils.RelativePath(_rootObject.transform, newParent.transform)
                                ?? throw new ArgumentException("not of root GameObject", nameof(newParent));

            var oldVGameObject = _tree.GetGameObject(oldPath);
            var newParentVGameObject = _tree.GetGameObject(newParentPath);

            oldVGameObject.MoveTo(newParentVGameObject);
        }

        public void RecordRemoveGameObject(GameObject component)
        {
            var oldPath = Utils.RelativePath(_rootObject.transform, component.transform)
                          ?? throw new ArgumentException("not of root GameObject", nameof(component));
            _tree.GetGameObject(oldPath).Remove();
        }


        public void RecordMoveComponent(Component from, GameObject newGameObject)
        {
            var oldPath = Utils.RelativePath(_rootObject.transform, from.transform)
                          ?? throw new ArgumentException("not of root GameObject", nameof(from));
            var newParentPath = Utils.RelativePath(_rootObject.transform, newGameObject.transform)
                                ?? throw new ArgumentException("not of root GameObject", nameof(newGameObject));

            var component = _tree.GetGameObject(oldPath).GetComponent(from.GetType(), from.GetInstanceID());
            var newParentVGameObject = _tree.GetGameObject(newParentPath);

            component.MoveTo(newParentVGameObject);
        }

        public void RecordRemoveComponent(Component component)
        {
            var oldPath = Utils.RelativePath(_rootObject.transform, component.transform)
                          ?? throw new ArgumentException("not of root GameObject", nameof(component));
            var vComponent = _tree.GetGameObject(oldPath).GetComponent(component.GetType(), component.GetInstanceID());
            vComponent.Remove();
        }

        public void RecordMoveProperty(Component from, string oldProp, string newProp)
        {
            var path = Utils.RelativePath(_rootObject.transform, from.transform)
                       ?? throw new ArgumentException("not of root GameObject", nameof(from));
            var component = _tree.GetGameObject(path).GetComponent(from.GetType(), from.GetInstanceID());

            component.MoveProperty(oldProp, newProp);
        }

        public void RecordRemoveProperty(Component from, string oldProp)
        {
            var path = Utils.RelativePath(_rootObject.transform, from.transform)
                       ?? throw new ArgumentException("not of root GameObject", nameof(from));
            var component = _tree.GetGameObject(path).GetComponent(from.GetType(), from.GetInstanceID());

            component.RemoveProperty(oldProp);
        }

        /// <summary> Represents a GameObject in Hierarchy </summary>
        class VGameObject
        {
            private readonly VGameObject _originalParent;
            private readonly string _originalName;
            private VGameObject _newParent;
            private string _newName;

            private readonly Dictionary<string, VGameObject> _originalChildren = new Dictionary<string, VGameObject>();

            private readonly Dictionary<string, List<VGameObject>> _newChildren =
                new Dictionary<string, List<VGameObject>>();

            private readonly Dictionary<Type, VComponent> _originalComponents = new Dictionary<Type, VComponent>();

            private readonly Dictionary<Type, List<VComponent>> _newComponents =
                new Dictionary<Type, List<VComponent>>();

            public VGameObject(VGameObject parent, string name)
            {
                _originalParent = _newParent = parent;
                _originalName = _newName = name;
            }

            private List<VComponent> GetComponents(Type type) =>
                _newComponents.TryGetValue(type, out var newList)
                    ? newList
                    : _newComponents[type] = new List<VComponent>();

            public VComponent GetComponent(Type type, int instanceId)
            {
                var list = GetComponents(type);
                if (list.Count == 0)
                    list.Add(_originalComponents[type] = new VComponent(this, type, instanceId));
                return list[0];
            }

            public void MoveComponentTo(VComponent component, VGameObject newGameObject)
            {
                if (component.NewGameObject != this)
                    throw new ArgumentException("bad newGameObject", nameof(component));
                var components = GetComponents(component.Type);
                Debug.Assert(components.Remove(component));
                newGameObject.GetComponents(component.Type).Add(component);
                component.NewGameObject = newGameObject;
            }

            public void RemoveComponent(VComponent component)
            {
                if (component.NewGameObject != this)
                    throw new ArgumentException("bad newGameObject", nameof(component));
                Debug.Assert(GetComponents(component.Type).Remove(component));
                component.NewGameObject = null;
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

            public void Remove()
            {
                System.Diagnostics.Debug.Assert(_newParent.ChildListWithName(_newName).Remove(this));
                _newParent = null;
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
            public int InstanceId { get; }

            public VComponent(VGameObject gameObject, Type type, int instanceId)
            {
                OriginalGameObject = NewGameObject = gameObject;
                InstanceId = instanceId;
                Type = type;
            }

            public void MoveTo(VGameObject newGameObject) => NewGameObject.MoveComponentTo(this, newGameObject);

            public void Remove()
            {
                NewGameObject.RemoveComponent(this);
            }

            public void MoveProperty(string oldProp, string newProp)
            {
                if (!_newProperties.TryGetValue(oldProp, out var prop))
                    prop = _newProperties[oldProp] = _newProperties[oldProp] = new VProperty();

                _newProperties.Remove(oldProp);
                _newProperties[newProp] = prop;
            }

            public void RemoveProperty(string oldProp)
            {
                if (!_newProperties.TryGetValue(oldProp, out var prop))
                    prop = _newProperties[oldProp] = _newProperties[oldProp] = new VProperty();

                _newProperties.Remove(oldProp);
            }
        }

        /// <summary> Represents a property </summary>
        class VProperty
        {
        }
    }

    static class VProp
    {
        private const string EXTRA_PROP = "AvatarOptimizerExtraProps";
        public static string BlendShapeIndex(int index) => $"{EXTRA_PROP}.BlendShapeIndex.{index}";
    }
}
