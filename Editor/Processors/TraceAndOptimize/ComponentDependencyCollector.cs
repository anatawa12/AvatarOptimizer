using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.APIInternal;
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
            internal readonly Dictionary<Component, (DependencyFlags flags, DependencyType type)> Dependencies =
                new Dictionary<Component, (DependencyFlags, DependencyType)>();

            public ComponentDependencies(Component component)
            {
                const DependencyFlags ComponentToTransformFlags =
                    DependencyFlags.EvenIfThisIsDisabled | DependencyFlags.EvenIfTargetIsDisabled;
                Dependencies[component.gameObject.transform] = (ComponentToTransformFlags, DependencyType.ComponentToTransform);
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
                var collector = new Collector(this, component);
                if (ComponentInfoRegistry.TryGetInformation(component.GetType(), out var information))
                {
                    information.CollectDependencyInternal(component, collector);
                }
                else
                {
                    BuildReport.LogWarning("TraceAndOptimize:warn:unknown-type", component.GetType().Name);

                    FallbackDependenciesParser(component, collector);
                }
                collector.FinalizeForComponent();
            });
        }

        private void FallbackDependenciesParser(Component component, Collector collector)
        {
            // fallback dependencies: All References are Always Dependencies.
            collector.MarkEntrypoint();
            using (var serialized = new SerializedObject(component))
            {
                foreach (var property in serialized.ObjectReferenceProperties())
                {
                    if (property.objectReferenceValue is GameObject go)
                        collector.AddDependency(go.transform).EvenIfDependantDisabled();
                    else if (property.objectReferenceValue is Component com)
                        collector.AddDependency(com).EvenIfDependantDisabled();
                }
            }
        }

        internal class Collector : API.ComponentDependencyCollector
        {
            private readonly ComponentDependencyCollector _collector;
            private readonly ComponentDependencies _deps;
            [NotNull] private readonly ComponentDependencyInfo _dependencyInfoSharedInstance;

            public Collector(ComponentDependencyCollector collector, Component component)
            {
                _collector = collector;
                _deps = collector.GetDependencies(component);
                _dependencyInfoSharedInstance = new ComponentDependencyInfo();
            }

            public bool PreserveEndBone => _collector._preserveEndBone;

            public MeshInfo2 GetMeshInfoFor(SkinnedMeshRenderer renderer) =>
                _collector._session.GetMeshInfoFor(renderer);

            public override void MarkEntrypoint() => _deps.EntrypointComponent = true;

            private API.ComponentDependencyInfo AddDependencyInternal(
                [NotNull] ComponentDependencies dependencies, 
                [CanBeNull] Component dependency,
                DependencyType type = DependencyType.Normal)
            {
                _dependencyInfoSharedInstance.Finish();
                _dependencyInfoSharedInstance.Init(dependencies.Dependencies, dependency, type);
                return _dependencyInfoSharedInstance;
            }

            public override API.ComponentDependencyInfo AddDependency(Component dependant, Component dependency) =>
                AddDependencyInternal(_collector.GetDependencies(dependant), dependency);

            public override API.ComponentDependencyInfo AddDependency(Component dependency) =>
                AddDependencyInternal(_deps, dependency);

            public void AddParentDependency(Transform component) =>
                AddDependencyInternal(_deps, component.parent, DependencyType.Parent)
                    .EvenIfDependantDisabled();

            public void AddBoneDependency(Transform bone) =>
                AddDependencyInternal(_deps, bone, DependencyType.Bone);

            public void FinalizeForComponent()
            {
                _dependencyInfoSharedInstance.Finish();
            }

            private class ComponentDependencyInfo : API.ComponentDependencyInfo
            {
                // ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
                [NotNull] private Dictionary<Component, (DependencyFlags, DependencyType)> _dependencies;
                [CanBeNull] private Component _component;
                private DependencyType _type;

                private bool _evenIfTargetIsDisabled;
                private bool _evenIfThisIsDisabled;

                internal void Init(
                    [NotNull] Dictionary<Component, (DependencyFlags, DependencyType)> dependencies,
                    [CanBeNull] Component component,
                    DependencyType type = DependencyType.Normal)
                {
                    System.Diagnostics.Debug.Assert(_component == null, "Init on not finished");
                    _dependencies = dependencies;
                    _component = component;
                    _evenIfTargetIsDisabled = true;
                    _evenIfThisIsDisabled = false;
                    _type = type;
                }

                internal void Finish()
                {
                    if (_component == null) return;

                    _dependencies.TryGetValue(_component, out var pair);
                    var flags = pair.Item1;
                    if (_evenIfTargetIsDisabled) flags |= DependencyFlags.EvenIfTargetIsDisabled;
                    if (_evenIfThisIsDisabled) flags |= DependencyFlags.EvenIfThisIsDisabled;
                    _dependencies[_component] = (flags, pair.Item2 | _type);

                    _component = null;
                }

                public override API.ComponentDependencyInfo EvenIfDependantDisabled()
                {
                    _evenIfThisIsDisabled = true;
                    return this;
                }

                public override API.ComponentDependencyInfo OnlyIfTargetCanBeEnable()
                {
                    _evenIfTargetIsDisabled = false;
                    return this;
                }
            }
        }
    }
}

