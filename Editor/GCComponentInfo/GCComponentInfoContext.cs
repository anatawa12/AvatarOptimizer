using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.ndmf;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    class GCComponentInfoContext : IExtensionContext
    {
        public GCComponentInfoHolder ComponentInfos;

        public void OnActivate(BuildContext context)
        {
            if (!context.GetState<AAOEnabled>().Enabled) return;
            ComponentInfos = new GCComponentInfoHolder(context);
            var preserveEndBone = context.GetState<TraceAndOptimizeState>().PreserveEndBone;
            new ComponentDependencyRetriever(context, preserveEndBone, this).RetriveAllUsages();
        }

        public void OnDeactivate(BuildContext context)
        {
            ComponentInfos = default;
        }

        public IEnumerable<GCComponentInfo> AllInformation => ComponentInfos.AllInformation;
        public GCComponentInfo? TryGetInfo(Component? dependent) => ComponentInfos.TryGetInfo(dependent);
        public GCComponentInfo GetInfo(Component dependent) => ComponentInfos.GetInfo(dependent);
    }

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
                // objects without enabled checkbox are treated as enabled
                // https://github.com/anatawa12/AvatarOptimizer/issues/1241
                if (component is MonoBehaviour && EditorUtility.GetObjectEnabled(component) == -1) activeness = true;
                _dependencies.Add(component, new GCComponentInfo(component, activeness));
            }

            // process children
            foreach (var child in transform.DirectChildrenEnumerable())
            {
                InitializeDependencies(child, transformActiveness);
            }
        }

        public IEnumerable<GCComponentInfo> AllInformation => _dependencies.Values;

        public GCComponentInfo? TryGetInfo(Component? dependent) =>
            dependent != null && _dependencies.TryGetValue(dependent, out var dependencies) ? dependencies : null;

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
        /// True if activeness of this component has meaning
        /// </summary>
        public bool BehaviourComponent => _behaviourComponent || HeavyBehaviourComponent;

        /// <summary>
        /// Dependencies of this component
        /// </summary>
        internal readonly Dictionary<Component, DependencyType> Dependencies = new();

        internal readonly Component Component;

        public bool IsEntrypoint => EntrypointComponent && Activeness != false;

        public readonly bool? Activeness;
        private bool _behaviourComponent = false;

        public GCComponentInfo(Component component, bool? activeness)
        {
            Component = component;
            Dependencies[component.gameObject.transform] = DependencyType.ComponentToTransform;
            Activeness = activeness;
        }


        public void MarkEntrypoint() => EntrypointComponent = true;
        public void MarkHeavyBehaviour() => HeavyBehaviourComponent = true;
        public void MarkBehaviour() => _behaviourComponent = true;

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
                    activeness = _context.GetConstantValue(gameObject, Props.IsActive, gameObject.activeSelf);
                    break;
                case Behaviour behaviour:
                    activeness = _context.GetConstantValue(behaviour, Props.EnabledFor(behaviour), behaviour.enabled);
                    break;
                case Cloth cloth:
                    activeness = _context.GetConstantValue(cloth, Props.EnabledFor(cloth), cloth.enabled);
                    break;
                case Collider collider:
                    activeness = _context.GetConstantValue(collider, Props.EnabledFor(collider), collider.enabled);
                    break;
                case LODGroup lodGroup:
                    activeness = _context.GetConstantValue(lodGroup, Props.EnabledFor(lodGroup), lodGroup.enabled);
                    break;
                case Renderer renderer:
                    activeness = _context.GetConstantValue(renderer, Props.EnabledFor(renderer), renderer.enabled);
                    break;
                // components without isEnable
                case CanvasRenderer _:
                case Joint _:
                case MeshFilter _:
                case OcclusionArea _:
                case OcclusionPortal _:
                case ParticleSystem _:
                case Rigidbody _:
                case Rigidbody2D _:
                case TextMesh _:
                case Tree _:
                case WindZone _:
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
