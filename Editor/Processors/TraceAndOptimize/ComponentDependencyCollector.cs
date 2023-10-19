using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.API;
using Anatawa12.AvatarOptimizer.APIBackend;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    /// <summary>
    /// This class collects ALL dependencies of each component
    /// </summary>
    class ComponentDependencyCollector
    {
        private readonly bool _preserveEndBone;
        private readonly BuildContext _session;

        public ComponentDependencyCollector(BuildContext session, bool preserveEndBone)
        {
            _preserveEndBone = preserveEndBone;
            _session = session;
        }

        private readonly Dictionary<Component, ComponentDependencies> _dependencies =
            new Dictionary<Component, ComponentDependencies>();

        public class ComponentDependencies
        {
            /// <summary>
            /// True if this component has Active Meaning on the Avatar.
            /// </summary>
            public bool EntrypointComponent = false;

            /// <summary>
            /// Dependencies of this component
            /// </summary>
            [NotNull]
            public IReadOnlyDictionary<Component, (DependencyFlags flags, DependencyType type)> Dependencies => _dependencies;

            [NotNull] private readonly Dictionary<Component, (DependencyFlags, DependencyType)> _dependencies =
                new Dictionary<Component, (DependencyFlags, DependencyType)>();

            public ComponentDependencies(Component component)
            {
                const DependencyFlags ComponentToTransformFlags =
                    DependencyFlags.EvenIfThisIsDisabled | DependencyFlags.EvenIfTargetIsDisabled;
                _dependencies[component.gameObject.transform] = (ComponentToTransformFlags, DependencyType.ComponentToTransform);
            }

            public IComponentDependencyInfo AddDependency(Component component)
            {
                if (!component)
                    return EmptyComponentDependencyInfo.Instance;
                return new ComponentDependencyInfo(_dependencies, component).SetFlags();
            }

            public void AddParentDependency(Transform component)
            {
                var parent = component.parent;
                if (parent) new ComponentDependencyInfo(_dependencies, parent, DependencyType.Parent)
                    .EvenIfDependantDisabled();
            }

            public void AddBoneDependency(Transform bone)
            {
                if (bone) new ComponentDependencyInfo(_dependencies, bone, DependencyType.Bone).SetFlags();
            }

            class EmptyComponentDependencyInfo : IComponentDependencyInfo
            {
                public static EmptyComponentDependencyInfo Instance = new EmptyComponentDependencyInfo();

                private EmptyComponentDependencyInfo()
                {
                }

                public IComponentDependencyInfo EvenIfDependantDisabled() => this;
                public IComponentDependencyInfo OnlyIfTargetCanBeEnable() => this;
            }

            private struct ComponentDependencyInfo : IComponentDependencyInfo
            {
                [NotNull] private readonly Dictionary<Component, (DependencyFlags, DependencyType)> _dependencies;
                private readonly Component _component;

                private readonly DependencyFlags _prevFlags;
                private DependencyFlags _flags;
                private readonly DependencyType _types;

                public ComponentDependencyInfo(
                    [NotNull] Dictionary<Component, (DependencyFlags, DependencyType)> dependencies, 
                    [NotNull] Component component,
                    DependencyType type = DependencyType.Normal)
                {
                    _dependencies = dependencies;
                    _component = component;
                    _dependencies.TryGetValue(component, out var pair);
                    _prevFlags = pair.Item1;

                    _flags = DependencyFlags.EvenIfTargetIsDisabled;
                    _types = pair.Item2 | type;
                }

                internal ComponentDependencyInfo SetFlags()
                {
                    _dependencies[_component] = (_prevFlags | _flags, _types);
                    return this;
                }

                public IComponentDependencyInfo EvenIfDependantDisabled()
                {
                    _flags |= DependencyFlags.EvenIfThisIsDisabled;
                    SetFlags();
                    return this;
                }

                public IComponentDependencyInfo OnlyIfTargetCanBeEnable()
                {
                    _flags &= ~DependencyFlags.EvenIfTargetIsDisabled;
                    SetFlags();
                    return this;
                }
            }
        }

        [Flags]
        public enum DependencyFlags : byte
        {
            // dependency flags
            EvenIfTargetIsDisabled = 1 << 0,
            EvenIfThisIsDisabled = 1 << 1,
        }

        [Flags]
        public enum DependencyType : byte
        {
            Normal = 1 << 0,
            Parent = 1 << 1,
            ComponentToTransform = 1 << 2,
            Bone = 1 << 3,
        }

        [CanBeNull]
        public ComponentDependencies TryGetDependencies(Component dependent) =>
            _dependencies.TryGetValue(dependent, out var dependencies) ? dependencies : null;

        [NotNull]
        public ComponentDependencies GetDependencies(Component dependent) => _dependencies[dependent];

        public void CollectAllUsages()
        {
            var components = _session.GetComponents<Component>().ToArray();
            // first iteration: create mapping
            foreach (var component in components) _dependencies.Add(component, new ComponentDependencies(component));

            // second iteration: process parsers
            BuildReport.ReportingObjects(components, component =>
            {
                // component requires GameObject.
                if (ComponentInfoRegistry.TryGetInformation(component.GetType(), out var information))
                {
                    information.CollectDependencyInternal(component, new Collector(this, component));
                }
                else
                {
                    BuildReport.LogWarning("TraceAndOptimize:warn:unknown-type", component.GetType().Name);

                    FallbackDependenciesParser(component);
                }
            });
        }

        private void FallbackDependenciesParser(Component component)
        {
            // fallback dependencies: All References are Always Dependencies.
            var dependencies = GetDependencies(component);
            dependencies.EntrypointComponent = true;
            using (var serialized = new SerializedObject(component))
            {
                foreach (var property in serialized.ObjectReferenceProperties())
                {
                    if (property.objectReferenceValue is GameObject go)
                        dependencies.AddDependency(go.transform).EvenIfDependantDisabled();
                    else if (property.objectReferenceValue is Component com)
                        dependencies.AddDependency(com).EvenIfDependantDisabled();
                }
            }
        }

        internal class Collector : IComponentDependencyCollector
        {
            private readonly ComponentDependencyCollector _collector;
            private readonly ComponentDependencies _deps;

            public Collector(ComponentDependencyCollector collector, Component component)
            {
                _collector = collector;
                _deps = collector.GetDependencies(component);
            }

            public bool PreserveEndBone => _collector._preserveEndBone;

            public MeshInfo2 GetMeshInfoFor(SkinnedMeshRenderer renderer) =>
                _collector._session.GetMeshInfoFor(renderer);

            public void MarkEntrypoint() => _deps.EntrypointComponent = true;
            public IComponentDependencyInfo AddDependency(Component dependant, Component dependency) =>
                _collector.GetDependencies(dependant).AddDependency(dependency);
            public IComponentDependencyInfo AddDependency(Component dependency) => _deps.AddDependency(dependency);

            public void AddParentDependency(Transform component) => _deps.AddParentDependency(component);
            public void AddBoneDependency(Transform bone) => _deps.AddBoneDependency(bone);
        }
    }
}

