using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
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

        public ObjectMapping BuildObjectMapping()
        {
            var goNewPath = _tree.BuildNewPathMapping();
            var goOldPath = _tree.BuildOldPathMapping();
            var goMapping = goOldPath.ToDictionary(x => x.Value,
                x =>
                {
                    goNewPath.TryGetValue(x.Key, out var newPath);
                    return newPath;
                });

            var componentMapping = new Dictionary<(Type, string), (string, Dictionary<string, string>)>();
            var instanceIdToComponent = new Dictionary<int, (Type, string, Component)>();
            var newGameObjectCache = new Dictionary<string, GameObject>();

            foreach (var component in _tree.GetAllComponents())
            {
                var oldPath = goOldPath[component.OriginalGameObject];
                if (component.NewGameObject == null)
                {
                    componentMapping[(component.Type, oldPath)] = (null, null);
                    instanceIdToComponent[component.InstanceId] = (component.Type, oldPath, null);
                }
                else
                {
                    var newPath = goNewPath[component.NewGameObject];
                    var propertyMapping = new Dictionary<string, string>();
                    var newMapping = component.NewProperties.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
                    foreach (var kvp in component.OriginalProperties)
                        propertyMapping[kvp.Key] = newMapping[kvp.Value];

                    componentMapping[(component.Type, oldPath)] = (newPath, propertyMapping);

                    if (!newGameObjectCache.TryGetValue(newPath, out var newGameObject))
                        newGameObject = newGameObjectCache[newPath] = Utils.GetGameObjectRelative(_rootObject, newPath);
                    instanceIdToComponent[component.InstanceId] =
                        (component.Type, oldPath, newGameObject.GetComponent(component.Type));
                }
            }

            return new ObjectMapping(goMapping, componentMapping, instanceIdToComponent);
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

            public Dictionary<VGameObject, string> BuildNewPathMapping() => BuildMapping(x => x
                ._newChildren.Select(pair => new KeyValuePair<string, IEnumerable<VGameObject>>(pair.Key, pair.Value)));
            public Dictionary<VGameObject, string> BuildOldPathMapping() => BuildMapping(x => x
                ._originalChildren.Select(pair => new KeyValuePair<string, IEnumerable<VGameObject>>(pair.Key, new []{pair.Value})));

            private Dictionary<VGameObject, string> BuildMapping(Func<VGameObject, IEnumerable<KeyValuePair<string, IEnumerable<VGameObject>>>> mappingGetter)
            {
                var result = new Dictionary<VGameObject, string>();
                var queue = new Queue<(string, VGameObject)>();

                foreach (var keyValuePair in mappingGetter(this))
                foreach (var gameObject in keyValuePair.Value)
                    queue.Enqueue((keyValuePair.Key, gameObject));

                while (queue.Count != 0)
                {
                    var (name, go) = queue.Dequeue();
                    result.Add(go, name);

                    
                    foreach (var keyValuePair in mappingGetter(go))
                    foreach (var gameObject in keyValuePair.Value)
                        queue.Enqueue(($"{name}/{keyValuePair.Key}", gameObject));
                }

                return result;
            }

            public IEnumerable<VComponent> GetAllComponents()
            {
                var queue = new Queue<VGameObject>();
                queue.Enqueue(this);

                while (queue.Count != 0)
                {
                    var go = queue.Dequeue();
                    foreach (var component in go._originalComponents.Values)
                        yield return component;

                    foreach (var gameObject in go._originalChildren.Values)
                        queue.Enqueue(gameObject);
                }
            }
        }

        /// <summary> Represents a component </summary>
        class VComponent
        {
            public readonly VGameObject OriginalGameObject;
            public VGameObject NewGameObject;
            public readonly Type Type;
            public readonly Dictionary<string, VProperty> OriginalProperties = new Dictionary<string, VProperty>();
            public readonly Dictionary<string, VProperty> NewProperties = new Dictionary<string, VProperty>();
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
                if (!NewProperties.TryGetValue(oldProp, out var prop))
                    prop = NewProperties[oldProp] = OriginalProperties[oldProp] = new VProperty();

                NewProperties.Remove(oldProp);
                NewProperties[newProp] = prop;
            }

            public void RemoveProperty(string oldProp)
            {
                if (!NewProperties.TryGetValue(oldProp, out var prop))
                    prop = NewProperties[oldProp] = OriginalProperties[oldProp] = new VProperty();

                NewProperties.Remove(oldProp);
            }
        }

        /// <summary> Represents a property </summary>
        class VProperty
        {
        }
    }

    internal class ObjectMapping
    {
        public ObjectMapping(Dictionary<string, string> goMapping,
            Dictionary<(Type, string), (string, Dictionary<string, string>)> componentMapping,
            Dictionary<int, (Type, string, Component)> instanceIdToComponent)
        {
            GameObjectPathMapping = goMapping;
            ComponentMapping = componentMapping;
            InstanceIdToComponent = instanceIdToComponent;
        }


        public Dictionary<(Type, string),(string newPath, Dictionary<string,string> propMapping)> ComponentMapping { get; }
        public Dictionary<string, string> GameObjectPathMapping { get; }
        public Dictionary<int,(Type, string, Component)> InstanceIdToComponent { get; }

        public EditorCurveBinding MapPath(string rootPath, EditorCurveBinding binding)
        {
            string Join(string a, string b) => a == "" ? b : b == "" ? a : $"{a}/{b}";
            string StripPrefixPath(string parent, string path, char sep)
            {
                if (path == null) return null;
                if (parent == path) return "";
                if (path.StartsWith($"{parent}{sep}", StringComparison.Ordinal))
                    return parent.Substring(parent.Length + 1);
                return null;
            }

            var oldPath = Join(rootPath, binding.path);

            // try as component first
            if (ComponentMapping.TryGetValue((binding.type, oldPath), out var componentInfo))
            {
                var (newPath, propMapping) = componentInfo;
                newPath = StripPrefixPath(rootPath, newPath, '/');
                if (newPath == null || propMapping == null) return default;

                binding.path = newPath;

                foreach (var (prop, rest) in Utils.FindSubProps(binding.propertyName))
                {
                    if (propMapping.TryGetValue(prop, out var newProp))
                    {
                        var newFullProp = newProp + rest;
                        binding.propertyName = newFullProp;
                        break;
                    }
                }
                return binding;
            }

            // then, try as GameObject
            if (GameObjectPathMapping.TryGetValue(oldPath, out var newGoPath))
            {
                newGoPath = StripPrefixPath(rootPath, newGoPath, '/');
                if (newGoPath == null) return default;
                binding.path = newGoPath;
                return binding;
            }

            return binding;
        }
    }

    static class VProp
    {
        private const string EXTRA_PROP = "AvatarOptimizerExtraProps";
        public static string BlendShapeIndex(int index) => $"{EXTRA_PROP}.BlendShapeIndex.{index}";
    }
}
