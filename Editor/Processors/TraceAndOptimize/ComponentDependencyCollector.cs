using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.APIInternal;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    /// <summary>
    /// This class collects ALL dependencies of each component
    /// </summary>
    readonly struct ComponentDependencyCollector
    {
        private readonly bool _preserveEndBone;
        private readonly BuildContext _session;
        private readonly GCComponentInfoHolder _componentInfos;

        public ComponentDependencyCollector(BuildContext session, bool preserveEndBone,
            GCComponentInfoHolder componentInfos)
        {
            _preserveEndBone = preserveEndBone;
            _session = session;
            _componentInfos = componentInfos;
        }


        public void CollectAllUsages()
        {
            var isParameterUsed = GetRootAnimatorParameters(_session.AvatarRootObject);

            var collector = new Collector(this, _componentInfos, isParameterUsed);
            var unknownComponents = new Dictionary<Type, List<Object>>();
            // second iteration: process parsers
            foreach (var componentInfo in _componentInfos.AllInformation)
            {
                var component = componentInfo.Component;
                using (ErrorReport.WithContextObject(component))
                {
                    // component requires GameObject.
                    collector.Init(componentInfo);
                    if (ComponentInfoRegistry.TryGetInformation(component.GetType(), out var information))
                    {
                        information.CollectDependencyInternal(component, collector);
                    }
                    else
                    {
                        if (!unknownComponents.TryGetValue(component.GetType(), out var list))
                            unknownComponents.Add(component.GetType(), list = new List<Object>());
                        list.Add(component);

                        FallbackDependenciesParser(component, collector);
                    }

                    foreach (var requiredComponent in RequireComponentCache.GetRequiredComponents(component.GetType()))
                    foreach (var required in component.GetComponents(requiredComponent))
                            collector.AddDependency(component, required)
                                .EvenIfDependantDisabled();

                    collector.FinalizeForComponent();
                }
            }
            foreach (var (type, objects) in unknownComponents)
                BuildLog.LogWarning("TraceAndOptimize:warn:unknown-type", type, objects);
        }

        private static void FallbackDependenciesParser(Component component, API.ComponentDependencyCollector collector)
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

        private static Predicate<string> GetRootAnimatorParameters(GameObject rootGameObject)
        {
            var parameters = new HashSet<string>();

            var animator = rootGameObject.GetComponent<Animator>();
            if (animator)
            {
                var runtimeAnimatorController = animator.runtimeAnimatorController;
                if (runtimeAnimatorController)
                {
                    var (controller, _) = ACUtils.GetControllerAndOverrides(runtimeAnimatorController);
                    foreach (var parameter in controller.parameters)
                        parameters.Add(parameter.name);
                }
            }

#if AAO_VRCSDK3_AVATARS
            var descriptor = rootGameObject.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            if (descriptor)
            {
                if (descriptor.customizeAnimationLayers)
                {
                    foreach (var layer in descriptor.baseAnimationLayers.Concat(descriptor.specialAnimationLayers))
                    {
                        if (layer.isDefault || !layer.animatorController) continue;

                        var (controller, _) = ACUtils.GetControllerAndOverrides(layer.animatorController);
                        foreach (var parameter in controller.parameters)
                            parameters.Add(parameter.name);
                    }
                }

                if (descriptor is { customExpressions: true, expressionParameters: var expressionParameters }
                    && expressionParameters != null)
                {
                    if (expressionParameters.parameters != null)
                    {
                        foreach (var parameter in expressionParameters.parameters)
                            if (parameter != null)
                                parameters.Add(parameter.name);
                    }
                }
                else
                {
                    parameters.Add("VRCEmote");
                    parameters.Add("VRCFaceBlendH");
                    parameters.Add("VRCFaceBlendV");
                }
            }
#endif

            // OSC and other External Tools Parameters
            var externalParameters = AssetDescription.GetParametersReadByExternalTools();
            parameters.UnionWith(externalParameters.ExactMatch);

            return parameter =>
            {
                return parameters.Contains(parameter) ||
                       externalParameters.RegexMatch.Any(regex => regex.IsMatch(parameter));
            };
        }

        internal class Collector : API.ComponentDependencyCollector
        {
            private readonly ComponentDependencyCollector _collector;
            private readonly GCComponentInfoHolder _componentInfos;
            private readonly Predicate<string> _isParameterUsed;
            private GCComponentInfo? _info;
            private IDependencyInfo? _dependencyInfo;

            public Collector(ComponentDependencyCollector collector, GCComponentInfoHolder componentInfos,
                Predicate<string> isParameterUsed)
            {
                _collector = collector;
                _componentInfos = componentInfos;
                _isParameterUsed = isParameterUsed;
            }
            
            public void Init(GCComponentInfo info)
            {
                Utils.Assert(_info == null, "Init on not finished");
                _info = info;
            }

            public bool PreserveEndBone => _collector._preserveEndBone;

            public MeshInfo2 GetMeshInfoFor(SkinnedMeshRenderer renderer) =>
                _collector._session.GetMeshInfoFor(renderer);

            public override void MarkEntrypoint() => _info!.MarkEntrypoint();
            public override void MarkHeavyBehaviour() => _info!.MarkHeavyBehaviour();
            public override void MarkBehaviour() => _info!.MarkBehaviour();

            private API.ComponentDependencyInfo AddDependencyInternal(
                GCComponentInfo? info,
                Component? dependency,
                GCComponentInfo.DependencyType type = GCComponentInfo.DependencyType.Normal)
            {
                _dependencyInfo?.Finish();
                _dependencyInfo = null;

                if (dependency == null) return DummyComponentDependencyInfo.Instance;
                if (info == null) return DummyComponentDependencyInfo.Instance;
                if (!dependency.transform.IsChildOf(_collector._session.AvatarRootTransform)) return DummyComponentDependencyInfo.Instance;

                var dependencyInfo = new ComponentDependencyInfo(_componentInfos, info, dependency, type);
                _dependencyInfo = dependencyInfo;
                return dependencyInfo;
            }

            public override API.ComponentDependencyInfo AddDependency(Component? dependant, Component? dependency) =>
                AddDependencyInternal(_collector._componentInfos.TryGetInfo(dependant), dependency);

            public override API.ComponentDependencyInfo AddDependency(Component? dependency) =>
                AddDependencyInternal(_info, dependency);

            internal override bool? GetAnimatedFlag(Component component, string animationProperty, bool currentValue) =>
                _collector._session.GetConstantValue(component, animationProperty, currentValue);

            internal override bool IsParameterUsed(string parameterName) => _isParameterUsed(parameterName);

            public override API.PathDependencyInfo AddPathDependency(Transform dependency, Transform root)
            {
                _dependencyInfo?.Finish();
                _dependencyInfo = null;

                if (dependency == null) throw new ArgumentNullException(nameof(dependency));
                if (root == null) throw new ArgumentNullException(nameof(root));

                var transforms = new List<Transform>();
                foreach (var transform in dependency.ParentEnumerable(root, includeMe: true))
                    transforms.Add(transform);

                if (transforms.Count == 0)
                    throw new ArgumentException("dependency is not child of root");
                if (transforms[^1].parent != root)
                    throw new ArgumentException("dependency is not child of root");

                if (!dependency.transform.IsChildOf(_collector._session.AvatarRootTransform))
                    return DummyPathDependencyInfo.Instance;

                var dependencyInfo = new PathDependencyInfo(_info!, transforms.ToArray());
                _dependencyInfo = dependencyInfo;
                return dependencyInfo;
            }

            public void AddParentDependency(Transform component) =>
                AddDependencyInternal(_info, component.parent, GCComponentInfo.DependencyType.Parent)
                    .EvenIfDependantDisabled();

            public void AddBoneDependency(Transform? bone) =>
                AddDependencyInternal(_info, bone, GCComponentInfo.DependencyType.Bone)
                    .EvenIfDependantDisabled();

            public void FinalizeForComponent()
            {
                _dependencyInfo?.Finish();
                _dependencyInfo = null;
                _info = null;
            }

            internal interface IDependencyInfo
            {
                void Finish();
            }

            private class DummyComponentDependencyInfo : API.ComponentDependencyInfo
            {
                public static DummyComponentDependencyInfo Instance { get; } = new();

                public override API.ComponentDependencyInfo EvenIfDependantDisabled() => this;
                public override API.ComponentDependencyInfo OnlyIfTargetCanBeEnable() => this;
            }

            private class ComponentDependencyInfo : API.ComponentDependencyInfo, IDependencyInfo
            {
                private readonly GCComponentInfoHolder _componentInfos;

                private Component? _dependency;
                private readonly GCComponentInfo _dependantInformation;
                private readonly GCComponentInfo.DependencyType _type;

                private bool _evenIfTargetIsDisabled = true;
                private bool _evenIfThisIsDisabled = false;

                // ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
                public ComponentDependencyInfo(
                    GCComponentInfoHolder componentInfos,
                    GCComponentInfo dependantInformation,
                    Component component,
                    GCComponentInfo.DependencyType type = GCComponentInfo.DependencyType.Normal)
                {
                    _componentInfos = componentInfos;
                    _dependency = component;
                    _dependantInformation = dependantInformation;
                    _type = type;
                }

                public void Finish()
                {
                    if (_dependency == null) return;
                    SetToDictionary();
                    _dependency = null;
                }

                private void SetToDictionary()
                {
                    if (_dependency == null) throw new InvalidOperationException("Called after another call");

                    if (!_evenIfThisIsDisabled)
                    {
                        // dependant must can be able to be enable
                        if (_dependantInformation.Activeness == false) return;
                    }
                    
                    if (!_evenIfTargetIsDisabled)
                    {
                        // dependency must can be able to be enable
                        if (_componentInfos.GetInfo(_dependency).Activeness == false) return;
                    }

                    _dependantInformation.Dependencies.TryGetValue(_dependency, out var type);
                    _dependantInformation.Dependencies[_dependency] = type | _type;
                }

                public override API.ComponentDependencyInfo EvenIfDependantDisabled()
                {
                    if (_dependency == null) throw new InvalidOperationException("Called after another call");
                    _evenIfThisIsDisabled = true;
                    return this;
                }

                public override API.ComponentDependencyInfo OnlyIfTargetCanBeEnable()
                {
                    if (_dependency == null) throw new InvalidOperationException("Called after another call");
                    _evenIfTargetIsDisabled = false;
                    return this;
                }
            }

            private class DummyPathDependencyInfo : API.PathDependencyInfo
            {
                public static DummyPathDependencyInfo Instance { get; } = new DummyPathDependencyInfo();

                public override API.PathDependencyInfo EvenIfDependantDisabled() => this;
            }

            private class PathDependencyInfo : API.PathDependencyInfo, IDependencyInfo
            {
                private Transform[]? _dependencies;
                private readonly GCComponentInfo _dependantInformation;

                private bool _evenIfThisIsDisabled;

                // ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
                public PathDependencyInfo(
                    GCComponentInfo dependantInformation,
                    Transform[] component)
                {
                    _dependencies = component;
                    _dependantInformation = dependantInformation;
                    _evenIfThisIsDisabled = false;
                }

                public void Finish()
                {
                    if (_dependencies == null) return;
                    SetToDictionary();
                    _dependencies = null;
                }

                private void SetToDictionary()
                {
                    if (_dependencies == null) throw new InvalidOperationException("Called after another call");

                    if (!_evenIfThisIsDisabled)
                    {
                        // dependant must can be able to be enable
                        if (_dependantInformation.Activeness == false) return;
                    }

                    foreach (var dependency in _dependencies)
                    {
                        _dependantInformation.Dependencies.TryGetValue(dependency, out var type);
                        _dependantInformation.Dependencies[dependency] = type | GCComponentInfo.DependencyType.Normal;
                    }
                }

                public override API.PathDependencyInfo EvenIfDependantDisabled()
                {
                    if (_dependencies == null) throw new InvalidOperationException("Called after another call");
                    _evenIfThisIsDisabled = true;
                    return this;
                }
            }
        }
    }
}

