using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.AnimatorParsers;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal readonly partial struct GCComponentInfoHolder
    {
        private readonly BuildContext _context;
        private readonly Dictionary<Component, GCComponentInfo> _dependencies;

        public GCComponentInfoHolder(BuildContext context)
        {
            _context = context;
            // initialize _dependencies
            _dependencies = new Dictionary<Component, GCComponentInfo>();
            InitializeDependencies(_context.AvatarRootTransform.transform, true);
        }

        private void InitializeDependencies(Transform transform, bool? parentActiveness)
        {
            // add & process the GameObject itself
            var transformActiveness = ComputeActiveness(transform, parentActiveness);
            _dependencies.Add(transform, new GCComponentInfo(transform, transformActiveness));

            // process components on the GameObject
            foreach (var component in transform.GetComponents<Component>())
            {
                if (component is Transform) continue;
                var activeness = ComputeActiveness(component, transformActiveness);
                _dependencies.Add(component, new GCComponentInfo(component, activeness));
            }

            // process children
            foreach (var child in transform.DirectChildrenEnumerable())
            {
                InitializeDependencies(child, transformActiveness);
            }
        }

        public IEnumerable<GCComponentInfo> AllInformation => _dependencies.Values;

        [CanBeNull]
        public GCComponentInfo TryGetInfo(Component dependent) =>
            _dependencies.TryGetValue(dependent, out var dependencies) ? dependencies : null;

        [NotNull]
        public GCComponentInfo GetInfo(Component dependent) => _dependencies[dependent];
    }

    internal class GCComponentInfo
    {
        /// <summary>
        /// True if this component has Active side-effect Meaning on the Avatar.
        /// </summary>
        public bool EntrypointComponent = false;
        
        /// <summary>
        /// True if activeness of this component has meaning and inactive is lighter
        /// </summary>
        public bool HeavyBehaviourComponent = false;

        /// <summary>
        /// Dependencies of this component
        /// </summary>
        [NotNull] internal readonly Dictionary<Component, DependencyType> Dependencies =
            new Dictionary<Component, DependencyType>();

        /// <summary>
        /// Dependants entrypoint components 
        /// </summary>
        [NotNull] internal readonly Dictionary<Component, DependencyType> DependantEntrypoint =
            new Dictionary<Component, DependencyType>();

        internal readonly Component Component;

        public DependencyType AllUsages
        {
            get
            {
                DependencyType type = default;
                foreach (var usage in DependantEntrypoint.Values)
                    type |= usage;
                return type;
            }
        }

        public bool IsEntrypoint => EntrypointComponent && Activeness != false;

        public readonly bool? Activeness;

        public GCComponentInfo(Component component, bool? activeness)
        {
            Component = component;
            Dependencies[component.gameObject.transform] = DependencyType.ComponentToTransform;
            Activeness = activeness;
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

    internal readonly partial struct GCComponentInfoHolder
    {
        private bool? ComputeActiveness(Component component, bool? parentActiveness)
        {
            if (parentActiveness == false) return false;

            bool? activeness;
            switch (component)
            {
                case Transform transform:
                    var gameObject = transform.gameObject;
                    activeness = _context.GetConstantValue(gameObject, "m_IsActive", gameObject.activeSelf);
                    break;
                case Behaviour behaviour:
                    activeness = _context.GetConstantValue(behaviour, "m_Enabled", behaviour.enabled);
                    break;
                case Cloth cloth:
                    activeness = _context.GetConstantValue(cloth, "m_Enabled", cloth.enabled);
                    break;
                case Collider collider:
                    activeness = _context.GetConstantValue(collider, "m_Enabled", collider.enabled);
                    break;
                case LODGroup lodGroup:
                    activeness = _context.GetConstantValue(lodGroup, "m_Enabled", lodGroup.enabled);
                    break;
                case Renderer renderer:
                    activeness = _context.GetConstantValue(renderer, "m_Enabled", renderer.enabled);
                    break;
                // components without isEnable
                case CanvasRenderer _:
                case Joint _:
                case MeshFilter _:
                case OcclusionArea _:
                case OcclusionPortal _:
                case ParticleSystem _:
#if !UNITY_2021_3_OR_NEWER
                case ParticleSystemForceField _:
#endif
                case Rigidbody _:
                case Rigidbody2D _:
                case TextMesh _:
                case Tree _:
                case WindZone _:
#if !UNITY_2020_2_OR_NEWER
                case UnityEngine.XR.WSA.WorldAnchor _:
#endif
                    activeness = true;
                    break;
                case Component _:
                case null:
                    // fallback: all components type should be proceed with above switch
                    activeness = null;
                    break;
            }

            if (activeness == false) return false;
            if (parentActiveness == true && activeness == true) return true;

            return null;
        }
    }
}