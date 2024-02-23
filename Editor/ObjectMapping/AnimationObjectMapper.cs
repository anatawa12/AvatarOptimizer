using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal class AnimationObjectMapper
    {
        readonly GameObject _rootGameObject;
        readonly BeforeGameObjectTree _beforeGameObjectTree;
        readonly ObjectMapping _objectMapping;

        private readonly Dictionary<string, MappedGameObjectInfo> _pathsCache =
            new Dictionary<string, MappedGameObjectInfo>();

        public AnimationObjectMapper(GameObject rootGameObject, BeforeGameObjectTree beforeGameObjectTree,
            ObjectMapping objectMapping)
        {
            _rootGameObject = rootGameObject;
            _beforeGameObjectTree = beforeGameObjectTree;
            _objectMapping = objectMapping;
        }

        // null means nothing to map
        [CanBeNull]
        private MappedGameObjectInfo GetGameObjectInfo(string path)
        {
            if (_pathsCache.TryGetValue(path, out var info)) return info;

            var tree = _beforeGameObjectTree.ResolvePath(path);

            if (tree == null)
            {
                info = null;
            }
            else
            {
                var foundGameObject = EditorUtility.InstanceIDToObject(tree.InstanceId) as GameObject;
                var newPath = foundGameObject
                    ? Utils.RelativePath(_rootGameObject.transform, foundGameObject.transform)
                    : null;

                info = new MappedGameObjectInfo(_objectMapping, newPath, tree);
            }

            _pathsCache.Add(path, info);
            return info;
        }

        class MappedGameObjectInfo
        {
            private ObjectMapping _objectMapping;

            readonly BeforeGameObjectTree _tree;

            // null means removed gameObject
            [CanBeNull] public readonly string NewPath;

            public MappedGameObjectInfo(ObjectMapping objectMapping, string newPath,
                BeforeGameObjectTree tree)
            {
                _objectMapping = objectMapping;
                NewPath = newPath;
                _tree = tree;
            }

            public (int instanceId, ComponentInfo) GetComponentByType(Type type)
            {
                if (!_tree.ComponentInstanceIdByType.TryGetValue(type, out var instanceId))
                    return (instanceId, null); // Nothing to map
                return (instanceId, _objectMapping.GetComponentMapping(instanceId));
            }
        }

        [CanBeNull]
        public string MapPath(string srcPath, Type type)
        {
            var gameObjectInfo = GetGameObjectInfo(srcPath);
            if (gameObjectInfo == null) return srcPath;
            var (instanceId, componentInfo) = gameObjectInfo.GetComponentByType(type);

            if (componentInfo != null)
            {
                var component = new ComponentOrGameObject(EditorUtility.InstanceIDToObject(componentInfo.MergedInto));
                // there's mapping about component.
                // this means the component is merged or some prop has mapping
                if (!component) return null; // this means removed.

                var newPath = Utils.RelativePath(_rootGameObject.transform, component.transform);
                if (newPath == null) return null; // this means moved to out of the animator scope

                return newPath;
            }
            else
            {
                // The component is not merged & no prop mapping so process GameObject mapping
                var component = EditorUtility.InstanceIDToObject(instanceId);
                if (!component) return null; // this means removed

                if (gameObjectInfo.NewPath == null) return null;
                return gameObjectInfo.NewPath;
            }
        }

        [CanBeNull]
        public (string path, Type type, string propertyName)[] MapBinding(string path, Type type, string propertyName)
        {
            var gameObjectInfo = GetGameObjectInfo(path);
            if (gameObjectInfo == null)
                return null;
            var (instanceId, componentInfo) = gameObjectInfo.GetComponentByType(type);

            if (componentInfo != null)
            {
                // there's mapping about component.
                // this means the component is merged or some prop has mapping

                if (componentInfo.PropertyMapping.TryGetValue(propertyName, out var newProp))
                {
                    // if mapped one is exactly same as original, return null
                    if (newProp.AllCopiedTo.Length == 1
                        && gameObjectInfo.NewPath == path
                        && newProp.AllCopiedTo[0].InstanceId == instanceId
                        && newProp.AllCopiedTo[0].Name == propertyName)
                        return null;

                    // there are mapping for property
                    var mappedBindings = new (string path, Type type, string propertyName)[newProp.AllCopiedTo.Length];
                    var copiedToIndex = 0;
                    for (var i = 0; i < newProp.AllCopiedTo.Length; i++)
                    {
                        var descriptor = newProp.AllCopiedTo[copiedToIndex++];
                        var component = new ComponentOrGameObject(EditorUtility.InstanceIDToObject(descriptor.InstanceId));
                        // this means removed.
                        if (!component)
                        {
                            copiedToIndex -= 1;
                            continue;
                        }

                        var newPath = Utils.RelativePath(_rootGameObject.transform, component.transform);

                        // this means moved to out of the animator scope
                        // TODO: add warning
                        if (newPath == null) return Array.Empty<(string path, Type type, string propertyName)>();

                        mappedBindings[i] = (newPath, descriptor.Type, descriptor.Name);
                    }

                    if (copiedToIndex != mappedBindings.Length)
                        return mappedBindings.AsSpan().Slice(0, copiedToIndex).ToArray();
                    return mappedBindings;
                }
                else
                {
                    var component = new ComponentOrGameObject(EditorUtility.InstanceIDToObject(componentInfo.MergedInto));
                    if (!component) return Array.Empty<(string path, Type type, string propertyName)>(); // this means removed.

                    var newPath = Utils.RelativePath(_rootGameObject.transform, component.transform);
                    if (newPath == null) return Array.Empty<(string path, Type type, string propertyName)>(); // this means moved to out of the animator scope
                    if (path == newPath) return null;
                    return new []{ (newPath, type, propertyName) };
                }
            }
            else
            {
                // The component is not merged & no prop mapping so process GameObject mapping

                var component = EditorUtility.InstanceIDToObject(instanceId);
                if (!component) return Array.Empty<(string path, Type type, string propertyName)>(); // this means removed

                if (gameObjectInfo.NewPath == null) return Array.Empty<(string path, Type type, string propertyName)>();
                if (path == gameObjectInfo.NewPath) return null;
                return new[] { (gameObjectInfo.NewPath, type, propertyName) };
            }
        }
    }
}
