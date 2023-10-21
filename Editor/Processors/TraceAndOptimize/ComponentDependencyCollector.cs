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
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    /// <summary>
    /// This class collects ALL dependencies of each component
    /// </summary>
    class ComponentDependencyCollector
    {
        private readonly bool _preserveEndBone;
        private readonly BuildContext _session;
        private readonly ActivenessCache  _activenessCache;

        public ComponentDependencyCollector(BuildContext session, bool preserveEndBone, ActivenessCache activenessCache)
        {
            _preserveEndBone = preserveEndBone;
            _session = session;
            _activenessCache = activenessCache;
        }

        private readonly Dictionary<Component, ComponentDependencies> _dependencies =
            new Dictionary<Component, ComponentDependencies>();

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

            var collector = new Collector(this, _activenessCache);
            // second iteration: process parsers
            BuildReport.ReportingObjects(components, component =>
            {
                // component requires GameObject.
                collector.Init(component);
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

        private void FallbackDependenciesParser(Component component, API.ComponentDependencyCollector collector)
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
            private ComponentDependencies _deps;
            private Component _component;
            [NotNull] private readonly ComponentDependencyInfo _dependencyInfoSharedInstance;

            public Collector(ComponentDependencyCollector collector, ActivenessCache activenessCache)
            {
                _collector = collector;
                _dependencyInfoSharedInstance = new ComponentDependencyInfo(activenessCache);
            }
            
            public void Init(Component component)
            {
                Debug.Assert(_deps == null, "Init on not finished");
                _component = component;
                _deps = _collector.GetDependencies(component);
            }

            public bool PreserveEndBone => _collector._preserveEndBone;

            public MeshInfo2 GetMeshInfoFor(SkinnedMeshRenderer renderer) =>
                _collector._session.GetMeshInfoFor(renderer);

            public override void MarkEntrypoint() => _deps.EntrypointComponent = true;

            private API.ComponentDependencyInfo AddDependencyInternal(
                [NotNull] ComponentDependencies dependencies,
                [CanBeNull] Component dependency,
                ComponentDependencies.DependencyType type = ComponentDependencies.DependencyType.Normal)
            {
                _dependencyInfoSharedInstance.Finish();
                _dependencyInfoSharedInstance.Init(dependencies.Component, dependencies.Dependencies, dependency, type);
                return _dependencyInfoSharedInstance;
            }

            public override API.ComponentDependencyInfo AddDependency(Component dependant, Component dependency) =>
                AddDependencyInternal(_collector.GetDependencies(dependant), dependency);

            public override API.ComponentDependencyInfo AddDependency(Component dependency) =>
                AddDependencyInternal(_deps, dependency);

            public void AddParentDependency(Transform component) =>
                AddDependencyInternal(_deps, component.parent, ComponentDependencies.DependencyType.Parent)
                    .EvenIfDependantDisabled();

            public void AddBoneDependency(Transform bone) =>
                AddDependencyInternal(_deps, bone, ComponentDependencies.DependencyType.Bone);

            public void FinalizeForComponent()
            {
                _dependencyInfoSharedInstance.Finish();
                _deps = null;
            }

            private class ComponentDependencyInfo : API.ComponentDependencyInfo
            {
                private readonly ActivenessCache _activenessCache;

                [NotNull] private Dictionary<Component, ComponentDependencies.DependencyType> _dependencies;
                [CanBeNull] private Component _dependency;
                private Component _dependant;
                private ComponentDependencies.DependencyType _type;

                private bool _evenIfTargetIsDisabled;
                private bool _evenIfThisIsDisabled;

                // ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
                public ComponentDependencyInfo(ActivenessCache activenessCache)
                {
                    _activenessCache = activenessCache;
                }

                internal void Init(Component dependant,
                    [NotNull] Dictionary<Component, ComponentDependencies.DependencyType> dependencies,
                    [CanBeNull] Component component,
                    ComponentDependencies.DependencyType type = ComponentDependencies.DependencyType.Normal)
                {
                    Debug.Assert(_dependency == null, "Init on not finished");
                    _dependencies = dependencies;
                    _dependency = component;
                    _dependant = dependant;
                    _evenIfTargetIsDisabled = true;
                    _evenIfThisIsDisabled = false;
                    _type = type;
                }

                internal void Finish()
                {
                    if (_dependency == null) return;
                    SetToDictionary();
                    _dependency = null;
                }

                private void SetToDictionary()
                {
                    Debug.Assert(_dependency != null, nameof(_dependency) + " != null");

                    if (!_evenIfThisIsDisabled)
                    {
                        // dependant must can be able to be enable
                        if (_activenessCache.GetActiveness(_dependant) == false) return;
                    }
                    
                    if (!_evenIfTargetIsDisabled)
                    {
                        // dependency must can be able to be enable
                        if (_activenessCache.GetActiveness(_dependency) == false) return;
                    }

                    _dependencies.TryGetValue(_dependency, out var type);
                    _dependencies[_dependency] = type | _type;
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

