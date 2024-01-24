using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
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
        // Responsibility of this class can be classified to the following parts
        // - Track moving GameObjects
        // - Track renaming Properties in Component
        // - Track merging Components

        private readonly IReadOnlyDictionary<int, BeforeGameObjectTree> _beforeGameObjectInfos;

        // key: instanceId
        private readonly Dictionary<int, BuildingComponentInfo> _originalComponentInfos = new Dictionary<int, BuildingComponentInfo>();
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

            _beforeGameObjectInfos[rootObject.GetInstanceID()].InitializeRecursive();
        }

        public void RecordMergeComponent<T>(T from, T mergeTo) where T: Component
        {
            if (!_componentInfos.TryGetValue(mergeTo.GetInstanceID(), out var mergeToInfo))
            {
                var newMergeToInfo = new BuildingComponentInfo(mergeTo);
                _originalComponentInfos.Add(mergeTo.GetInstanceID(), newMergeToInfo);
                _componentInfos.Add(mergeTo.GetInstanceID(), newMergeToInfo);
                GetComponentInfo(from).MergedTo(newMergeToInfo);
            }
            else
            {
                var newMergeToInfo = new BuildingComponentInfo(mergeTo);
                _componentInfos[mergeTo.GetInstanceID()]= newMergeToInfo;
                mergeToInfo.MergedTo(newMergeToInfo);
                GetComponentInfo(from).MergedTo(newMergeToInfo);
            }
        }

        public void RecordMoveProperties(ComponentOrGameObject from, params (string old, string @new)[] props) =>
            GetComponentInfo(from).MoveProperties(props);

        public void RecordMoveProperty(ComponentOrGameObject from, string oldProp, string newProp) =>
            GetComponentInfo(from).MoveProperties((oldProp, newProp));

        public void RecordMoveProperty(ComponentOrGameObject fromComponent, string oldProp, ComponentOrGameObject toComponent, string newProp) =>
            GetComponentInfo(fromComponent).MoveProperty(GetComponentInfo(toComponent), oldProp, newProp);

        public void RecordCopyProperty(ComponentOrGameObject fromComponent, string oldProp, ComponentOrGameObject toComponent, string newProp) =>
            GetComponentInfo(fromComponent).CopyProperty(GetComponentInfo(toComponent), oldProp, newProp);

        public void RecordRemoveProperty(ComponentOrGameObject from, string oldProp) =>
            GetComponentInfo(from).RemoveProperty(oldProp);

        public AnimationComponentInfo GetAnimationComponent(ComponentOrGameObject component)
            => GetComponentInfo(component);

        private BuildingComponentInfo GetComponentInfo(ComponentOrGameObject component)
        {
            if (!_componentInfos.TryGetValue(component.GetInstanceID(), out var info))
            {
                info = new BuildingComponentInfo(component);
                _originalComponentInfos.Add(component.GetInstanceID(), info);
                _componentInfos.Add(component.GetInstanceID(), info);
            }
            return info;
        }

        public void ImportModifications(
            RootPropModNodeContainer modifications
        )
        {
            foreach (var ((target, prop), value) in modifications.FloatNodes)
                GetComponentInfo(target).ImportProperty(prop, value);
        }

        public ObjectMapping BuildObjectMapping()
        {
            return new ObjectMapping(
                _beforeGameObjectInfos, 
                _originalComponentInfos.ToDictionary(p => p.Key, p => p.Value.Build()));
        }

        class AnimationPropertyInfo
        {
            [CanBeNull] public readonly BuildingComponentInfo Component;
            [CanBeNull] public readonly string Name;
            [CanBeNull] public AnimationPropertyInfo MergedTo { get; private set; }
            private MappedPropertyInfo? _mappedPropertyInfo;
            [CanBeNull] private RootPropModNode<float> _floatNode;
            [CanBeNull] public List<AnimationPropertyInfo> CopiedTo { get; private set; }
            [CanBeNull]
            public RootPropModNode<float> FloatNode => _floatNode?.Normalize();

            public AnimationPropertyInfo([NotNull] BuildingComponentInfo component, [NotNull] string name)
            {
                Component = component ?? throw new ArgumentNullException(nameof(component));
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            private AnimationPropertyInfo()
            {
            }

            public static readonly AnimationPropertyInfo RemovedMarker = new AnimationPropertyInfo();

            public void MergeTo(AnimationPropertyInfo property)
            {
                MergedTo = property;
                MergeNode(ref property._floatNode, ref _floatNode);
            }

            public void CopyTo(AnimationPropertyInfo property)
            {
                if (CopiedTo == null)
                    CopiedTo = new List<AnimationPropertyInfo>();
                CopiedTo.Add(property);
                MergeNode(ref property._floatNode, ref _floatNode);
            }

            public MappedPropertyInfo GetMappedInfo()
            {
                if (_mappedPropertyInfo is MappedPropertyInfo property) return property;
                property = ComputeMappedInfo();
                _mappedPropertyInfo = property;
                return property;
            }

            private MappedPropertyInfo ComputeMappedInfo()
            {
                if (this == RemovedMarker) return MappedPropertyInfo.Removed;
                
                System.Diagnostics.Debug.Assert(Component != null, nameof(Component) + " != null");

                if (MergedTo != null)
                {
                    var merged = MergedTo.GetMappedInfo();

                    if (CopiedTo == null || CopiedTo.Count == 0)
                        return merged;

                    var copied = new List<PropertyDescriptor>();
                    copied.AddRange(merged.AllCopiedTo);
                    foreach (var copiedTo in CopiedTo)
                        copied.AddRange(copiedTo.GetMappedInfo().AllCopiedTo);
                    
                    return new MappedPropertyInfo(merged.MappedProperty, copied.ToArray());
                }
                else
                {
                    // this is edge
                    if (CopiedTo == null || CopiedTo.Count == 0)
                        return new MappedPropertyInfo(Component.InstanceId, Component.Type, Name);

                    var descriptor = new PropertyDescriptor(Component.InstanceId, Component.Type, Name);

                    var copied = new List<PropertyDescriptor> { descriptor };
                    foreach (var copiedTo in CopiedTo)
                        copied.AddRange(copiedTo.GetMappedInfo().AllCopiedTo);

                    return new MappedPropertyInfo(descriptor, copied.ToArray());
                }
            }

            private static void MergeNode<T>([CanBeNull] ref RootPropModNode<T> mergeTo,
                [CanBeNull] ref RootPropModNode<T> merge)
            {
                if (merge == null || merge.IsEmpty) return;
                if (mergeTo == null || merge.IsEmpty)
                {
                    mergeTo = merge;
                    return;
                }

                mergeTo.Add(merge);
                merge.Invalidate();
                merge = null;
            }

            public void ImportProperty(RootPropModNode<float> node)
            {
                if (FloatNode != null) throw new InvalidOperationException();
                _floatNode = node;
            }

            public void AddModification(ComponentPropModNode<float> node, bool alwaysApplied)
            {
                if (_floatNode == null) _floatNode = new RootPropModNode<float>();
                _floatNode.Add(node, alwaysApplied);
            }
        }

        class BuildingComponentInfo : AnimationComponentInfo
        {
            internal readonly int InstanceId;
            internal readonly Type Type;

            // id in this -> id in merged
            private BuildingComponentInfo _mergedInto;

            private readonly Dictionary<string, AnimationPropertyInfo> _beforePropertyIds =
                new Dictionary<string, AnimationPropertyInfo>();

            private readonly Dictionary<string, AnimationPropertyInfo> _afterPropertyIds =
                new Dictionary<string, AnimationPropertyInfo>();

            public BuildingComponentInfo(ComponentOrGameObject component)
            {
                InstanceId = component.GetInstanceID();
                Type = component.Value.GetType();
            }

            internal bool IsMerged => _mergedInto != null;

            [NotNull]
            private AnimationPropertyInfo GetProperty(string name, bool remove = false)
            {
                if (_afterPropertyIds.TryGetValue(name, out var prop))
                {
                    if (remove) _afterPropertyIds.Remove(name);
                    return prop;
                }
                else
                {
                    var newProp = new AnimationPropertyInfo(this, name);
                    if (!remove) _afterPropertyIds.Add(name, newProp);
                    if (!_beforePropertyIds.ContainsKey(name))
                        _beforePropertyIds.Add(name, newProp);
                    return newProp;
                }
            }

            public void MergedTo([NotNull] BuildingComponentInfo mergeTo)
            {
                if (Type == typeof(Transform)) throw new Exception("Merging Transform is not supported!");
                if (_mergedInto != null) throw new InvalidOperationException("Already merged");
                _mergedInto = mergeTo ?? throw new ArgumentNullException(nameof(mergeTo));
                foreach (var property in _afterPropertyIds.Values)
                    property.MergeTo(mergeTo.GetProperty(property.Name));
                _afterPropertyIds.Clear();
            }

            public void MoveProperties(params (string old, string @new)[] props)
            {
                if (Type == typeof(Transform)) throw new Exception("Move properties of Transform is not supported!");
                if (_mergedInto != null) throw new Exception("Already Merged");

                var propertyIds = new AnimationPropertyInfo[props.Length];
                for (var i = 0; i < props.Length; i++)
                    propertyIds[i] = GetProperty(props[i].old, remove: true);

                for (var i = 0; i < propertyIds.Length; i++)
                    propertyIds[i].MergeTo(GetProperty(props[i].@new));
            }

            public void MoveProperty(BuildingComponentInfo toComponent, string oldProp, string newProp)
            {
                if (Type == typeof(Transform)) throw new Exception("Move properties of Transform is not supported!");
                GetProperty(oldProp, remove: true).MergeTo(toComponent.GetProperty(newProp));
            }

            public void CopyProperty(BuildingComponentInfo toComponent, string oldProp, string newProp)
            {
                var prop = GetProperty(oldProp);
                prop.CopyTo(toComponent.GetProperty(newProp));
            }

            public void RemoveProperty(string property)
            {
                if (Type == typeof(Transform)) throw new Exception("Removing properties of Transform is not supported!");
                if (_mergedInto != null) throw new Exception("Already Merged");

                GetProperty(property, remove: true).MergeTo(AnimationPropertyInfo.RemovedMarker);
            }

            public ComponentInfo Build()
            {
                var propertyMapping = _beforePropertyIds.ToDictionary(p => p.Key,
                    p => p.Value.GetMappedInfo());
                var mergedInfo = this;
                while (mergedInfo._mergedInto != null)
                {
                    mergedInfo = mergedInfo._mergedInto;
                    foreach (var (key, value) in mergedInfo._beforePropertyIds)
                        if (!propertyMapping.ContainsKey(key))
                            propertyMapping.Add(key, value.GetMappedInfo());
                }

                return new ComponentInfo(InstanceId, mergedInfo.InstanceId, Type, propertyMapping);
            }

            public override bool ContainsFloat(string property) =>
                _afterPropertyIds.TryGetValue(property, out var info) && info.FloatNode != null;

            public override bool TryGetFloat(string propertyName, out RootPropModNode<float> animation)
            {
                animation = default;
                if (!_afterPropertyIds.TryGetValue(propertyName, out var info))
                    return false;
                animation = info.FloatNode;
                return animation != null;
            }

            public override void AddModification(string prop, ComponentPropModNode<float> node, bool alwaysApplied) =>
                GetProperty(prop).AddModification(node, alwaysApplied);

            public void ImportProperty(string prop, RootPropModNode<float> node) =>
                GetProperty(prop).ImportProperty(node);
        }
    }

    internal abstract class AnimationComponentInfo
    {
        public abstract bool ContainsFloat(string property);
        public abstract bool TryGetFloat(string propertyName, out RootPropModNode<float> animation);
        public abstract void AddModification(string prop, ComponentPropModNode<float> node, bool alwaysApplied);
    }
}
