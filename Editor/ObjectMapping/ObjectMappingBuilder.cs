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

        class BuildingComponentInfo
        {
            private readonly int _instanceId;
            private readonly Type _type;
            private readonly List<BuildingComponentInfo> MergeSources = new List<BuildingComponentInfo>();

            // id in this -> id in merged
            private BuildingComponentInfo _mergedInto;

            // renaming property tracker
            private int _nextPropertyId = 1;
            private readonly Dictionary<string, int> _beforePropertyIds = new Dictionary<string, int>();
            private readonly Dictionary<string, int> _afterPropertyIds = new Dictionary<string, int>();

            public BuildingComponentInfo(Component component)
            {
                _instanceId = component.GetInstanceID();
                _type = component.GetType();
            }

            public void MergedTo([NotNull] BuildingComponentInfo mergeTo)
            {
                if (_type == typeof(Transform)) throw new Exception("Merging Transform is not supported!");
                if (mergeTo == null) throw new ArgumentNullException(nameof(mergeTo));
                if (_mergedInto != null) throw new InvalidOperationException("Already merged");
                mergeTo.MergeSources.Add(this);
                _mergedInto = mergeTo;
            }

            public void MoveProperties(params (string old, string @new)[] props)
            {
                if (_type == typeof(Transform)) throw new Exception("Move properties of Transform is not supported!");
                foreach (var mergeSource in MergeSources) mergeSource.MoveProperties(props);

                var propertyIds = new int[props.Length];
                for (var i = 0; i < props.Length; i++)
                {
                    var (oldProp, newProp) = props[i];
                    if (_afterPropertyIds.TryGetValue(oldProp, out var propId))
                    {
                        propertyIds[i] = propId;
                    }
                    else
                    {
                        if (!_beforePropertyIds.ContainsKey(oldProp))
                        {
                            propertyIds[i] = _nextPropertyId++;
                        }
                    }
                }

                for (var i = 0; i < propertyIds.Length; i++)
                {
                    var propId = propertyIds[i];
                    var (oldProp, _) = props[i];
                    if (propId == 0) continue;
                    _afterPropertyIds.Remove(oldProp);
                }

                for (var i = 0; i < propertyIds.Length; i++)
                {
                    var propId = propertyIds[i];
                    var (oldProp, newProp) = props[i];
                    if (propId == 0) continue;
                    _afterPropertyIds[newProp] = propId;
                    if (!_beforePropertyIds.ContainsKey(oldProp))
                        _beforePropertyIds.Add(oldProp, propId);
                }
            }

            public void RemoveProperty(string oldProp)
            {
                if (_type == typeof(Transform)) throw new Exception("Removing properties of Transform is not supported!");
                foreach (var mergeSource in MergeSources) mergeSource.RemoveProperty(oldProp);
                // if (_afterPropertyIds.ContainsKey(oldProp))
                //     _afterPropertyIds.Remove(oldProp);
                // else
                //     if (!_beforePropertyIds.ContainsKey(oldProp))
                //         _beforePropertyIds.Add(oldProp, _nextPropertyId++);
                if (!_afterPropertyIds.Remove(oldProp))
                    if (!_beforePropertyIds.ContainsKey(oldProp))
                        _beforePropertyIds.Add(oldProp, _nextPropertyId++);
            }

            public ComponentInfo Build()
            {
                var mergedInfo = this;
                while (mergedInfo._mergedInto != null)
                    mergedInfo = mergedInfo._mergedInto;

                var idToAfterName = _afterPropertyIds.ToDictionary(p => p.Value, p => p.Key);
                var propertyMapping = _beforePropertyIds.ToDictionary(p => p.Key, 
                    p => idToAfterName.TryGetValue(p.Value, out var name) ? name : null);

                return new ComponentInfo(_instanceId, mergedInfo._instanceId, _type, propertyMapping);
            }
        }
    }
}
