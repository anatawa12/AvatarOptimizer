using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal class ObjectMapping
    {
        private readonly IReadOnlyDictionary<int, BeforeGameObjectTree> _beforeTree;
        private readonly IReadOnlyDictionary<int, ComponentInfo> _componentMapping;

        public ObjectMapping(
            IReadOnlyDictionary<int, BeforeGameObjectTree> beforeTree, 
            IReadOnlyDictionary<int, ComponentInfo> componentMapping)
        {
            _beforeTree = beforeTree;
            _componentMapping = componentMapping;
        }

        public bool MapComponentInstance(int instanceId, out Component component)
        {
            if (instanceId == 0)
            {
                component = null;
                return false;
            }
            var mergedInto = _componentMapping.TryGetValue(instanceId, out var info) ? info.MergedInto : instanceId;

            var found = EditorUtility.InstanceIDToObject(mergedInto);
            if (!found)
            {
                component = null;
                return true;
            }

            if (found is Component c)
            {
                component = c;
                return instanceId != mergedInto;
            }

            component = default;
            return false;
        }

        // null means nothing to map
        [CanBeNull]
        public ComponentInfo GetComponentMapping(int instanceId) =>
            _componentMapping.TryGetValue(instanceId, out var info) ? info : null;

        [CanBeNull]
        public AnimationObjectMapper CreateAnimationMapper(GameObject rootGameObject)
        {
            if (!_beforeTree.TryGetValue(rootGameObject.GetInstanceID(), out var beforeTree)) return null;
            return new AnimationObjectMapper(rootGameObject, beforeTree, this);
        }
    }
    class BeforeGameObjectTree
    {
        public readonly int InstanceId;
        public readonly int ParentInstanceId;
        [NotNull] public readonly string Name;
        [NotNull] public readonly IReadOnlyDictionary<Type, int> ComponentInstanceIdByType;
        [NotNull] public readonly int[] ComponentInstanceIds;
        [NotNull] public readonly BeforeGameObjectTree[] Children;

        public BeforeGameObjectTree(GameObject gameObject)
        {
            var parentTransform = gameObject.transform.parent;
            InstanceId = gameObject.GetInstanceID();
            Name = gameObject.name;
            ParentInstanceId = parentTransform ? parentTransform.gameObject.GetInstanceID() : 0;
            Children = new BeforeGameObjectTree[gameObject.transform.childCount];

            var components = gameObject.GetComponents<Component>();
            ComponentInstanceIds = components.Select(x => x.GetInstanceID()).ToArray();
            
            var componentByType = new Dictionary<Type, int>();
            foreach (var component in components)
                if (!componentByType.ContainsKey(component.GetType()))
                    componentByType.Add(component.GetType(), component.GetInstanceID());
            ComponentInstanceIdByType = componentByType;
        }
    }

    class ComponentInfo
    {
        public readonly int InstanceId;
        public readonly int MergedInto;
        public readonly Type Type;
        public readonly IReadOnlyDictionary<string, string> PropertyMapping;

        public ComponentInfo(int instanceId, int mergedInto, Type type, IReadOnlyDictionary<string, string> propertyMapping)
        {
            InstanceId = instanceId;
            MergedInto = mergedInto;
            Type = type;
            PropertyMapping = propertyMapping;
        }
    }

    static class VProp
    {
        private const string ExtraProps = "AvatarOptimizerExtraProps";
        public static string BlendShapeIndex(int index) => $"{ExtraProps}.BlendShapeIndex.{index}";

        public static int ParseBlendShapeIndex(string prop)
        {
            if (!prop.StartsWith($"{ExtraProps}.BlendShapeIndex.", StringComparison.Ordinal))
                throw new ArgumentException($"The property {prop} is not BlendShapeIndex", nameof(prop));
            var indexStr = prop.Substring($"{ExtraProps}.BlendShapeIndex.".Length);
            if (!int.TryParse(indexStr, out var index))
                throw new ArgumentException($"The property {prop} is not BlendShapeIndex", nameof(prop));
            return index;
        }
    }
}
