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

        public ObjectMappingBuilder([NotNull] GameObject rootObject)
        {
            _rootObject = rootObject ? rootObject : throw new ArgumentNullException(nameof(rootObject));
            RegisterAll(_rootObject, _rootObject.GetComponentsInChildren(typeof(Component), true));
        }

        private void RegisterAll(GameObject rootObject, Component[] components)
        {
            foreach (var component in components)
            {
                var path = Utils.RelativePath(rootObject.transform, component.transform);
                System.Diagnostics.Debug.Assert(path != null, nameof(path) + " != null");
                _tree.GetGameObject(path).GetComponents(component.GetType(), component.GetInstanceID());
            }
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

            var components = _tree.GetGameObject(oldPath).GetComponents(from.GetType(), from.GetInstanceID());
            var newParentVGameObject = _tree.GetGameObject(newParentPath);

            foreach (var component in components.ToArray())
                component.MoveTo(newParentVGameObject);
        }

        public void RecordRemoveComponent(Component component)
        {
            var oldPath = Utils.RelativePath(_rootObject.transform, component.transform)
                          ?? throw new ArgumentException("not of root GameObject", nameof(component));
            foreach (var vComponent in _tree.GetGameObject(oldPath)
                         .GetComponents(component.GetType(), component.GetInstanceID()).ToArray())
                vComponent.Remove();
        }

        public void RecordMoveProperty(Component from, string oldProp, string newProp)
        {
            var path = Utils.RelativePath(_rootObject.transform, from.transform)
                       ?? throw new ArgumentException("not of root GameObject", nameof(from));
            foreach (var component in _tree.GetGameObject(path).GetComponents(from.GetType(), from.GetInstanceID()))
                component.MoveProperty(oldProp, newProp);
        }

        public void RecordRemoveProperty(Component from, string oldProp)
        {
            var path = Utils.RelativePath(_rootObject.transform, from.transform)
                       ?? throw new ArgumentException("not of root GameObject", nameof(from));
            foreach (var component in _tree.GetGameObject(path).GetComponents(from.GetType(), from.GetInstanceID()))
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

            var componentMapping = new Dictionary<ObjectMapping.ComponentKey, ObjectMapping.MappedComponent>();
            var instanceIdToComponent = new Dictionary<int, (Component, ObjectMapping.MappedComponent)>();
            var newGameObjectCache = new Dictionary<string, GameObject>();

            foreach (var component in _tree.GetAllComponents())
            {
                var oldPath = goOldPath[component.OriginalGameObject];
                if (component.NewGameObject == null)
                {
                    componentMapping[new ObjectMapping.ComponentKey(oldPath, component.Type)] =
                        ObjectMapping.MappedComponent.Removed;
                    instanceIdToComponent[component.InstanceId] = (null, null);
                }
                else
                {
                    var newPath = goNewPath[component.NewGameObject];
                    var propertyMapping = new Dictionary<string, string>();
                    var newMapping = component.NewProperties.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
                    foreach (var kvp in component.OriginalProperties)
                    {
                        newMapping.TryGetValue(kvp.Value, out var newPropName);
                        propertyMapping[kvp.Key] = newPropName;
                    }

                    var mapped = new ObjectMapping.MappedComponent(newPath, propertyMapping);
                    componentMapping[new ObjectMapping.ComponentKey(oldPath, component.Type)] = mapped;

                    if (!newGameObjectCache.TryGetValue(newPath, out var newGameObject))
                        newGameObject = newGameObjectCache[newPath] = Utils.GetGameObjectRelative(_rootObject, newPath);
                    var actualComponent = newGameObject.GetComponent(component.Type);
                    instanceIdToComponent[component.InstanceId] = (actualComponent, mapped);
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

            public IEnumerable<VComponent> GetComponents(Type type, int instanceId)
            {
                var list = GetComponents(type);
                if (list.All(x => x.InstanceId != instanceId))
                {
                    var newComponent = new VComponent(this, type, instanceId);
                    list.Add(newComponent);
                    if (!_originalComponents.ContainsKey(type))
                        _originalComponents.Add(type, newComponent);
                }
                return list;
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
                if (path == "") return this;

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

            private List<VGameObject> ChildListWithName(string name)
            {
                if (name == "") throw new ArgumentException("name must not null");
                return _newChildren.TryGetValue(name, out var newList)
                    ? newList
                    : _newChildren[name] = new List<VGameObject>();
            }

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
            Dictionary<ComponentKey, MappedComponent> componentMapping,
            Dictionary<int, (Component, MappedComponent)> instanceIdToComponent)
        {
            GameObjectPathMapping = goMapping;
            ComponentMapping = componentMapping;
            InstanceIdToComponent = instanceIdToComponent;
        }

        public Dictionary<ComponentKey, MappedComponent> ComponentMapping { get; }
        public Dictionary<string, string> GameObjectPathMapping { get; }
        public Dictionary<int, (Component component, MappedComponent mapping)> InstanceIdToComponent { get; }

        public EditorCurveBinding MapPath(string rootPath, EditorCurveBinding binding)
        {
            string Join(string a, string b) => a == "" ? b : b == "" ? a : $"{a}/{b}";
            string StripPrefixPath(string parent, string path, char sep)
            {
                if (path == null) return null;
                if (parent == path) return "";
                if (parent == "") return path;
                if (path.StartsWith($"{parent}{sep}", StringComparison.Ordinal))
                    return parent.Substring(parent.Length + 1);
                return null;
            }

            var oldPath = Join(rootPath, binding.path);

            // try as component first
            if (ComponentMapping.TryGetValue(new ComponentKey(oldPath, binding.type), out var mapped))
            {
                var newPath = StripPrefixPath(rootPath, mapped.MappedGameObjectPath, '/');
                if (newPath == null || mapped.PropertyMapping == null) return default;

                binding.path = newPath;

                foreach (var (prop, rest) in Utils.FindSubPaths(binding.propertyName, '.'))
                {
                    if (mapped.PropertyMapping.TryGetValue(prop, out var newProp))
                    {
                        if (newProp == null) return default;
                        var newFullProp = newProp + rest;
                        binding.propertyName = newFullProp;
                        break;
                    }
                }
                return binding;
            }

            // then, try as GameObject
            foreach (var (path, rest) in Utils.FindSubPaths(oldPath, '/'))
            {
                if (GameObjectPathMapping.TryGetValue(path, out var newPath))
                {
                    newPath = StripPrefixPath(rootPath, newPath, '/');
                    if (newPath == null) return default;
                    binding.path = newPath + rest;
                    return binding;
                }
            }

            return binding;
        }

        public readonly struct ComponentKey
        {
            public readonly string GameObjectPath;
            public readonly Type Type;

            public ComponentKey(string gameObjectPath, Type type)
            {
                GameObjectPath = gameObjectPath;
                Type = type;
            }

            public bool Equals(ComponentKey other)
            {
                return GameObjectPath == other.GameObjectPath && Type == other.Type;
            }

            public override bool Equals(object obj)
            {
                return obj is ComponentKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((GameObjectPath != null ? GameObjectPath.GetHashCode() : 0) * 397) ^
                           (Type != null ? Type.GetHashCode() : 0);
                }
            }
        }

        public class MappedComponent
        {
            public readonly string MappedGameObjectPath;
            public readonly IReadOnlyDictionary<string, string> PropertyMapping;

            public MappedComponent(string mappedGameObjectPath, IReadOnlyDictionary<string, string> propertyMapping)
            {
                MappedGameObjectPath = mappedGameObjectPath;
                PropertyMapping = propertyMapping;
            }

            public static MappedComponent Removed = new MappedComponent(null, null);
        }
    }

    static class VProp
    {
        private const string EXTRA_PROP = "AvatarOptimizerExtraProps";
        public static string BlendShapeIndex(int index) => $"{EXTRA_PROP}.BlendShapeIndex.{index}";
    }
}
