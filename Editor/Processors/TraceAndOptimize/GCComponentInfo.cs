using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class GCComponentInfo
    {
        /// <summary>
        /// True if this component has Active Meaning on the Avatar.
        /// </summary>
        public bool EntrypointComponent = false;

        /// <summary>
        /// Dependencies of this component
        /// </summary>
        [NotNull] internal readonly Dictionary<Component, DependencyType> Dependencies =
            new Dictionary<Component, DependencyType>();

        internal readonly Component Component;

        public GCComponentInfo(Component component)
        {
            Component = component;
            Dependencies[component.gameObject.transform] = DependencyType.ComponentToTransform;
        }
        
        [Flags]
        public enum DependencyType : byte
        {
            Normal = 1 << 0,
            Parent = 1 << 1,
            ComponentToTransform = 1 << 2,
            Bone = 1 << 3,
        }
    }
}