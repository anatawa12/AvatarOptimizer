using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// The class manages Object location mapping
    /// </summary>
    internal class ObjectMappingBuilder
    {
        // Responsibility of this class can be classified to the following parts
        // - Track moving GameObjects
        // - Track renaming Properties in Component
        // - Track merging Components

        private readonly IReadOnlyDictionary<int, BeforeGameObjectTree> _beforeGameObjectInfos;

        // key: instanceId
        private readonly Dictionary<int, BuildingComponentInfo> _componentInfos = new Dictionary<int, BuildingComponentInfo>();

        public ObjectMappingBuilder([NotNull] GameObject rootObject)
        {
            if (!rootObject) throw new ArgumentNullException(nameof(rootObject));
            var transforms = rootObject.GetComponentsInChildren<Transform>(true);

            _beforeGameObjectInfos = transforms
                .ToDictionary(t => t.gameObject.GetInstanceID(), t => new BeforeGameObjectTree(t.gameObject));

            foreach (var transform in transforms)
            {
                if (!transform.parent) continue;
                if (!_beforeGameObjectInfos.TryGetValue(transform.parent.gameObject.GetInstanceID(),
                        out var parentInfo)) continue;
                var selfInfo = _beforeGameObjectInfos[transform.gameObject.GetInstanceID()];
                parentInfo.Children[transform.GetSiblingIndex()] = selfInfo;
            }

#if DEBUG
            // assertion
            foreach (var info in _beforeGameObjectInfos.Values)
                System.Diagnostics.Debug.Assert(info.Children.All(x => x != null), "info.Children.All(x => x != null)");
#endif
        }

        public void RecordMergeComponent<T>(T from, T mergeTo) where T: Component =>
            GetComponentInfo(from).MergedTo(GetComponentInfo(mergeTo));

        public void RecordMoveProperties(Component from, params (string old, string @new)[] props) =>
            GetComponentInfo(from).MoveProperties(props);

        public void RecordMoveProperty(Component from, string oldProp, string newProp) =>
            GetComponentInfo(from).MoveProperties((oldProp, newProp));

        public void RecordRemoveProperty(Component from, string oldProp) =>
            GetComponentInfo(from).RemoveProperty(oldProp);

        private BuildingComponentInfo GetComponentInfo(Component component)
        {
            if (!_componentInfos.TryGetValue(component.GetInstanceID(), out var info))
                _componentInfos.Add(component.GetInstanceID(), info = new BuildingComponentInfo(component));
            return info;
        }

        public ObjectMapping BuildObjectMapping()
        {
            return new ObjectMapping(
                _beforeGameObjectInfos, 
                _componentInfos.ToDictionary(p => p.Key, p => p.Value.Build()));
        }

        class AnimationProperty
        {
            [CanBeNull] public readonly BuildingComponentInfo Component;
            [CanBeNull] public readonly string Name;
            [CanBeNull] public AnimationProperty MergedTo;
            // TODO: add AnimationProperty[] CopiedTo and process later

            public AnimationProperty([NotNull] BuildingComponentInfo component, [NotNull] string name)
            {
                Component = component ?? throw new ArgumentNullException(nameof(component));
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            private AnimationProperty()
            {
            }

            public static readonly AnimationProperty RemovedMarker = new AnimationProperty();
        }

        class BuildingComponentInfo
        {
            private readonly int _instanceId;
            private readonly Type _type;

            // id in this -> id in merged
            private BuildingComponentInfo _mergedInto;

            private readonly Dictionary<string, AnimationProperty> _beforePropertyIds =
                new Dictionary<string, AnimationProperty>();

            private readonly Dictionary<string, AnimationProperty> _afterPropertyIds =
                new Dictionary<string, AnimationProperty>();

            public BuildingComponentInfo(Component component)
            {
                _instanceId = component.GetInstanceID();
                _type = component.GetType();
            }

            [NotNull]
            private AnimationProperty GetProperty(string name, bool remove = false)
            {
                if (_afterPropertyIds.TryGetValue(name, out var prop))
                {
                    if (remove) _afterPropertyIds.Remove(name);
                    return prop;
                }
                else
                {
                    var newProp = new AnimationProperty(this, name);
                    if (!remove) _afterPropertyIds.Add(name, newProp);
                    if (!_beforePropertyIds.ContainsKey(name))
                        _beforePropertyIds.Add(name, newProp);
                    return newProp;
                }
            }

            public void MergedTo([NotNull] BuildingComponentInfo mergeTo)
            {
                if (_type == typeof(Transform)) throw new Exception("Merging Transform is not supported!");
                if (_mergedInto != null) throw new InvalidOperationException("Already merged");
                _mergedInto = mergeTo ?? throw new ArgumentNullException(nameof(mergeTo));
                foreach (var property in _afterPropertyIds.Values)
                    property.MergedTo = mergeTo.GetProperty(property.Name);
                _afterPropertyIds.Clear();
            }

            public void MoveProperties(params (string old, string @new)[] props)
            {
                if (_type == typeof(Transform)) throw new Exception("Move properties of Transform is not supported!");
                if (_mergedInto != null) throw new Exception("Already Merged");

                var propertyIds = new AnimationProperty[props.Length];
                for (var i = 0; i < props.Length; i++)
                    propertyIds[i] = GetProperty(props[i].old, remove: true);

                for (var i = 0; i < propertyIds.Length; i++)
                    propertyIds[i].MergedTo = GetProperty(props[i].@new);
            }

            public void RemoveProperty(string property)
            {
                if (_type == typeof(Transform)) throw new Exception("Removing properties of Transform is not supported!");
                if (_mergedInto != null) throw new Exception("Already Merged");

                GetProperty(property, remove: true).MergedTo = AnimationProperty.RemovedMarker;
            }

            public ComponentInfo Build()
            {
                var mergedInfo = this;
                while (mergedInfo._mergedInto != null)
                    mergedInfo = mergedInfo._mergedInto;

                var propertyMapping = _beforePropertyIds.ToDictionary(p => p.Key, p => GetName(p.Value));

                return new ComponentInfo(_instanceId, mergedInfo._instanceId, _type, propertyMapping);

                string GetName(AnimationProperty property)
                {
                    while (property.MergedTo != null)
                        property = property.MergedTo;
                    if (property.Name == null) return null;
                    Debug.Assert(property.Component == mergedInfo);
                    return property.Name;
                }
            }
        }
    }
}
