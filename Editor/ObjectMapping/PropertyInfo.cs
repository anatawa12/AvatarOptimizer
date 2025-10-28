using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    internal struct PropertyInfo : IPropertyInfo<PropertyInfo>
    {
        private RootPropModNode<FloatValueInfo>? _floatNode;
        private RootPropModNode<ObjectValueInfo>? _objectNode;

        public RootPropModNode<FloatValueInfo> FloatNode => _floatNode ??= new RootPropModNode<FloatValueInfo>();
        public RootPropModNode<ObjectValueInfo> ObjectNode => _objectNode ??= new RootPropModNode<ObjectValueInfo>();

        public void MergeTo(ref PropertyInfo property)
        {
            MergeNode(ref property._floatNode, ref _floatNode);
            MergeNode(ref property._objectNode, ref _objectNode);
        }

        private static void MergeNode<TValueInfo>(ref RootPropModNode<TValueInfo>? mergeTo, ref RootPropModNode<TValueInfo>? merge)
            where TValueInfo : struct, IValueInfo<TValueInfo>
        {
            if (merge == null || merge.IsEmpty) return;
            if (mergeTo == null || mergeTo.IsEmpty)
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

        private static void CopyNode<TValueInfo>(ref RootPropModNode<TValueInfo>? mergeTo, RootPropModNode<TValueInfo>? merge)
            where TValueInfo: struct, IValueInfo<TValueInfo>
        {
            if (merge == null || merge.IsEmpty) return;
            mergeTo ??= new RootPropModNode<TValueInfo>();
            mergeTo.Add(merge);
        }

        public void ImportProperty(RootPropModNode<FloatValueInfo> node)
        {
            if (_floatNode != null) throw new InvalidOperationException();
            _floatNode = node;
        }

        public void ImportProperty(RootPropModNode<ObjectValueInfo> node)
        {
            if (_objectNode != null) throw new InvalidOperationException();
            _objectNode = node;
        }

        public void AddModification(ComponentPropModNodeBase<FloatValueInfo> node, ApplyState applyState)
        {
            if (_floatNode == null) _floatNode = new RootPropModNode<FloatValueInfo>();
            _floatNode.Add(node, applyState);
        }

        public void AddModification(ComponentPropModNodeBase<ObjectValueInfo> node, ApplyState applyState)
        {
            if (_objectNode == null) _objectNode = new RootPropModNode<ObjectValueInfo>();
            _objectNode.Add(node, ApplyState.Always);
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

        /// <summary>
        /// Check if there is animation for the property.
        /// This returns true even if the animation animating the property is in layer with weight 0.
        /// </summary>
        /// <param name="info">The animation component info.</param>
        /// <param name="property">The property name.</param>
        /// <returns>Returns true if there is animation for the property.</returns>
        public static bool ContainsAnimationForFloat(this AnimationComponentInfo<PropertyInfo> info, string property)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            return info.TryGetPropertyInfo(property).FloatNode.ComponentNodes.Any();
        }

        /// <summary>
        /// Check if the property is animated by some component.
        ///
        /// This returns false if the animation animating the property is in layer with weight 0.
        /// Be careful when using this method with some properties like "material.XXX" or Animator Animated Paramaeter.
        /// </summary>
        /// <param name="info">The animation component info.</param>
        /// <param name="property">The property name.</param>
        /// <returns>Returns true if the property is animated by some component.</returns>
        public static bool IsAnimatedFloat(this AnimationComponentInfo<PropertyInfo> info, string property)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            return info.TryGetPropertyInfo(property).FloatNode.ApplyState != ApplyState.Never;
        }

        public static RootPropModNode<FloatValueInfo> GetFloatNode(this AnimationComponentInfo<PropertyInfo> info, string property)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            return info.GetPropertyInfo(property).FloatNode;
        }

        public static void AddModification(this AnimationComponentInfo<PropertyInfo> info, string property,
            ComponentPropModNodeBase<FloatValueInfo> node, ApplyState applyState)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            info.GetPropertyInfo(property).AddModification(node, applyState);
        }

        /// <summary>
        /// Check if there is animation for the property.
        /// This returns true even if the animation animating the property is in layer with weight 0.
        /// </summary>
        /// <param name="info">The animation component info.</param>
        /// <param name="property">The property name.</param>
        /// <returns>Returns true if there is animation for the property.</returns>
        public static bool ContainsAnimationForObject(this AnimationComponentInfo<PropertyInfo> info, string property)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            return info.TryGetPropertyInfo(property).ObjectNode.ComponentNodes.Any();
        }

        /// <summary>
        /// Check if the property is animated by some component.
        /// This returns false if the animation animating the property is in layer with weight 0.
        /// </summary>
        /// <param name="info">Animation component info.</param>
        /// <param name="property">The property name.</param>
        /// <returns>Returns true if the property is animated by some component.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsAnimatedObject(this AnimationComponentInfo<PropertyInfo> info, string property)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            return info.TryGetPropertyInfo(property).ObjectNode.ApplyState != ApplyState.Never;
        }

        public static RootPropModNode<ObjectValueInfo> GetObjectNode(this AnimationComponentInfo<PropertyInfo> info, string property)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            return info.GetPropertyInfo(property).ObjectNode;
        }

        public static void AddModification(this AnimationComponentInfo<PropertyInfo> info, string property,
            ComponentPropModNodeBase<ObjectValueInfo> node, ApplyState applyState)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            info.GetPropertyInfo(property).AddModification(node, applyState);
        }

        public static IEnumerable<(string property, RootPropModNode<FloatValueInfo> node)> GetAllFloatProperties(
            this AnimationComponentInfo<PropertyInfo> info)
        {
            return info.GetAllPropertyInfo.Select(x => (x.name, x.info.FloatNode));
        }

        public static IEnumerable<(string property, RootPropModNode<ObjectValueInfo> node)> GetAllObjectProperties(
            this AnimationComponentInfo<PropertyInfo> info)
        {
            return info.GetAllPropertyInfo.Select(x => (x.name, x.info.ObjectNode));
        }
    }
}
