using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    internal struct PropertyInfo : IPropertyInfo<PropertyInfo>
    {
        [CanBeNull] private RootPropModNode<float> _floatNode;
        [CanBeNull] private RootPropModNode<Object> _objectNode;

        [CanBeNull] public RootPropModNode<float> FloatNode => _floatNode?.Normalize();
        [CanBeNull] public RootPropModNode<Object> ObjectNode => _objectNode?.Normalize();

        public void MergeTo(ref PropertyInfo property)
        {
            MergeNode(ref property._floatNode, ref _floatNode);
            MergeNode(ref property._objectNode, ref _objectNode);
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

        public void CopyTo(ref PropertyInfo property)
        {
            CopyNode(ref property._floatNode, _floatNode);
            CopyNode(ref property._objectNode, _objectNode);
        }

        private static void CopyNode<T>([CanBeNull] ref RootPropModNode<T> mergeTo,
            [CanBeNull] RootPropModNode<T> merge)
        {
            if (merge == null || merge.IsEmpty) return;
            if (mergeTo == null || merge.IsEmpty)
            {
                mergeTo = merge;
                return;
            }

            mergeTo.Add(merge);
        }

        public void ImportProperty(RootPropModNode<float> node)
        {
            if (FloatNode != null) throw new InvalidOperationException();
            _floatNode = node;
        }

        public void ImportProperty(RootPropModNode<Object> node)
        {
            if (ObjectNode != null) throw new InvalidOperationException();
            _objectNode = node;
        }

        public void AddModification(ComponentPropModNodeBase<float> node, bool alwaysApplied)
        {
            if (_floatNode == null) _floatNode = new RootPropModNode<float>();
            _floatNode.Add(node, alwaysApplied);
        }

        public void AddModification(ComponentPropModNodeBase<Object> node, bool alwaysApplied)
        {
            if (_objectNode == null) _objectNode = new RootPropModNode<Object>();
            _objectNode.Add(node, alwaysApplied);
        }
    }

    internal static class AnimationComponentInfoExtensions
    {
        public static void ImportModifications([NotNull] this ObjectMappingBuilder<PropertyInfo> builder,
            RootPropModNodeContainer modifications)
        {
            foreach (var ((target, prop), value) in modifications.FloatNodes)
                builder.GetAnimationComponent(target).GetPropertyInfo(prop).ImportProperty(value);

            foreach (var ((target, prop), value) in modifications.ObjectNodes)
                builder.GetAnimationComponent(target).GetPropertyInfo(prop).ImportProperty(value);
        }

        public static bool ContainsFloat([NotNull] this AnimationComponentInfo<PropertyInfo> info, string property)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            return info.TryGetPropertyInfo(property).FloatNode != null;
        }

        [Pure]
        [ContractAnnotation("=> true, animation: notnull; => false, animation: null")]
        public static bool TryGetFloat([NotNull] this AnimationComponentInfo<PropertyInfo> info, string property,
            [CanBeNull] out RootPropModNode<float> animation)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            animation = info.TryGetPropertyInfo(property).FloatNode;
            return animation != null;
        }

        public static void AddModification([NotNull] this AnimationComponentInfo<PropertyInfo> info, string property,
            ComponentPropModNodeBase<float> node, bool alwaysApplied)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            info.GetPropertyInfo(property).AddModification(node, alwaysApplied);
        }

        public static bool ContainsObject([NotNull] this AnimationComponentInfo<PropertyInfo> info, string property)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            return info.TryGetPropertyInfo(property).ObjectNode != null;
        }

        public static bool TryGetObject([NotNull] this AnimationComponentInfo<PropertyInfo> info, string property,
            out RootPropModNode<Object> animation)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            animation = info.TryGetPropertyInfo(property).ObjectNode;
            return animation != null;
        }

        public static void AddModification([NotNull] this AnimationComponentInfo<PropertyInfo> info, string property,
            ComponentPropModNodeBase<Object> node, bool alwaysApplied)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            info.GetPropertyInfo(property).AddModification(node, alwaysApplied);
        }

        public static IEnumerable<(string, RootPropModNode<float>)> GetAllFloatProperties(
            [NotNull] this AnimationComponentInfo<PropertyInfo> info)
        {
            return info.GetAllPropertyInfo.Where(x => x.info.FloatNode != null)
                .Select(x => (x.name, x.info.FloatNode));
        }

        public static IEnumerable<(string, RootPropModNode<Object>)> GetAllObjectProperties(
            [NotNull] this AnimationComponentInfo<PropertyInfo> info)
        {
            return info.GetAllPropertyInfo.Where(x => x.info.ObjectNode != null)
                .Select(x => (x.name, x.info.ObjectNode));
        }
    }
}