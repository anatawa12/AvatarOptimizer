using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    internal struct PropertyInfo : IPropertyInfo<PropertyInfo>
    {
        private RootPropModNode<ValueInfo<float>>? _floatNode;
        private RootPropModNode<ValueInfo<Object>>? _objectNode;

        public RootPropModNode<ValueInfo<float>>? FloatNode => _floatNode?.Normalize();
        public RootPropModNode<ValueInfo<Object>>? ObjectNode => _objectNode?.Normalize();

        public void MergeTo(ref PropertyInfo property)
        {
            MergeNode(ref property._floatNode, ref _floatNode);
            MergeNode(ref property._objectNode, ref _objectNode);
        }

        private static void MergeNode<T>(ref RootPropModNode<ValueInfo<T>>? mergeTo, ref RootPropModNode<ValueInfo<T>>? merge)
            where T : notnull
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

        private static void CopyNode<T>(ref RootPropModNode<ValueInfo<T>>? mergeTo, RootPropModNode<ValueInfo<T>>? merge)
            where T : notnull
        {
            if (merge == null || merge.IsEmpty) return;
            if (mergeTo == null || merge.IsEmpty)
            {
                mergeTo = merge;
                return;
            }

            mergeTo.Add(merge);
        }

        public void ImportProperty(RootPropModNode<ValueInfo<float>> node)
        {
            if (FloatNode != null) throw new InvalidOperationException();
            _floatNode = node;
        }

        public void ImportProperty(RootPropModNode<ValueInfo<Object>> node)
        {
            if (ObjectNode != null) throw new InvalidOperationException();
            _objectNode = node;
        }

        public void AddModification(ComponentPropModNodeBase<ValueInfo<float>> node, bool alwaysApplied)
        {
            if (_floatNode == null) _floatNode = new RootPropModNode<ValueInfo<float>>();
            _floatNode.Add(node, alwaysApplied);
        }

        public void AddModification(ComponentPropModNodeBase<ValueInfo<Object>> node, bool alwaysApplied)
        {
            if (_objectNode == null) _objectNode = new RootPropModNode<ValueInfo<Object>>();
            _objectNode.Add(node, alwaysApplied);
        }
    }

    internal static class AnimationComponentInfoExtensions
    {
        public static void ImportModifications(this ObjectMappingBuilder<PropertyInfo> builder,
            RootPropModNodeContainer modifications)
        {
            foreach (var ((target, prop), value) in modifications.FloatNodes)
                builder.GetAnimationComponent(target).GetPropertyInfo(prop).ImportProperty(value);

            foreach (var ((target, prop), value) in modifications.ObjectNodes)
                builder.GetAnimationComponent(target).GetPropertyInfo(prop).ImportProperty(value);
        }

        public static bool ContainsFloat(this AnimationComponentInfo<PropertyInfo> info, string property)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            return info.TryGetPropertyInfo(property).FloatNode != null;
        }

        [JetBrains.Annotations.Pure]
        public static bool TryGetFloat(this AnimationComponentInfo<PropertyInfo> info, string property, 
            [NotNullWhen(true)] out RootPropModNode<ValueInfo<float>>? animation)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            animation = info.TryGetPropertyInfo(property).FloatNode;
            return animation != null;
        }

        public static void AddModification(this AnimationComponentInfo<PropertyInfo> info, string property,
            ComponentPropModNodeBase<ValueInfo<float>> node, bool alwaysApplied)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            info.GetPropertyInfo(property).AddModification(node, alwaysApplied);
        }

        public static bool ContainsObject(this AnimationComponentInfo<PropertyInfo> info, string property)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            return info.TryGetPropertyInfo(property).ObjectNode != null;
        }

        public static bool TryGetObject(this AnimationComponentInfo<PropertyInfo> info, string property,
            [NotNullWhen(true)] out RootPropModNode<ValueInfo<Object>>? animation)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            animation = info.TryGetPropertyInfo(property).ObjectNode;
            return animation != null;
        }

        public static void AddModification(this AnimationComponentInfo<PropertyInfo> info, string property,
            ComponentPropModNodeBase<ValueInfo<Object>> node, bool alwaysApplied)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            info.GetPropertyInfo(property).AddModification(node, alwaysApplied);
        }

        public static IEnumerable<(string, RootPropModNode<ValueInfo<float>>)> GetAllFloatProperties(
            this AnimationComponentInfo<PropertyInfo> info)
        {
            return info.GetAllPropertyInfo.Where(x => x.info.FloatNode != null)
                .Select(x => (x.name, x.info.FloatNode!));
        }

        public static IEnumerable<(string, RootPropModNode<ValueInfo<Object>>)> GetAllObjectProperties(
            this AnimationComponentInfo<PropertyInfo> info)
        {
            return info.GetAllPropertyInfo.Where(x => x.info.ObjectNode != null)
                .Select(x => (x.name, x.info.ObjectNode!));
        }
    }
}
