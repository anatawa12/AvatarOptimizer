using System;
using System.Collections.Generic;
using System.Linq;
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

            var tree = _beforeGameObjectTree;

            if (path != "")
            {
                foreach (var pathSegment in path.Split('/'))
                {
                    tree = tree.Children.FirstOrDefault(x => x.Name == pathSegment);
                    if (tree == null) break;
                }
            }

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

        public EditorCurveBinding MapBinding(EditorCurveBinding binding)
        {
            var gameObjectInfo = GetGameObjectInfo(binding.path);
            if (gameObjectInfo == null) return binding;
            var (instanceId, componentInfo) = gameObjectInfo.GetComponentByType(binding.type);

            if (componentInfo != null)
            {
                var component = EditorUtility.InstanceIDToObject(componentInfo.MergedInto) as Component;
                // there's mapping about component.
                // this means the component is merged or some prop has mapping
                if (!component) return default; // this means removed.

                var newPath = Utils.RelativePath(_rootGameObject.transform, component.transform);
                if (newPath == null) return default; // this means moved to out of the animator scope

                binding.path = newPath;

                if (componentInfo.PropertyMapping.TryGetValue(binding.propertyName, out var newProp))
                    binding.propertyName = newProp;
            }
            else
            {
                // The component is not merged & no prop mapping so process GameObject mapping

                if (binding.type != typeof(GameObject))
                {
                    var component = EditorUtility.InstanceIDToObject(instanceId) as Component;
                    if (!component) return default; // this means removed
                }

                if (gameObjectInfo.NewPath == null) return default;
                binding.path = gameObjectInfo.NewPath;
            }

            return binding;
        }
    }
}
