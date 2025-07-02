using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class FindUnusedObjects : TraceAndOptimizePass<FindUnusedObjects>
    {
        public override string DisplayName => "T&O: FindUnusedObjects";

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            if (!state.RemoveUnusedObjects) return;

            var processor = new FindUnusedObjectsProcessor(context, state);
            processor.ProcessNew();
        }
    }

    internal readonly struct MarkObjectContext {
        private readonly GCComponentInfoHolder _componentInfos;

        private readonly Func<GCComponentInfo, Dictionary<Component, GCComponentInfo.DependencyType>> _getDependantMap;
        private readonly Queue<Component> _processPending;
        private readonly Component _entrypoint;

        public MarkObjectContext(GCComponentInfoHolder componentInfos, Component entrypoint,
            Func<GCComponentInfo, Dictionary<Component, GCComponentInfo.DependencyType>> getDependantMap)
        {
            _componentInfos = componentInfos;
            _processPending = new Queue<Component>();
            _entrypoint = entrypoint;
            _getDependantMap = getDependantMap;
        }

        public void MarkComponent(Component component,
            GCComponentInfo.DependencyType type)
        {
            var dependencies = _componentInfos.TryGetInfo(component);
            if (dependencies == null) return;

            var dependantMap = _getDependantMap(dependencies);
            if (dependantMap.TryGetValue(_entrypoint, out var existingFlags))
            {
                dependantMap[_entrypoint] = existingFlags | type;
            }
            else
            {
                _processPending.Enqueue(component);
                dependantMap.Add(_entrypoint, type);
            }
        }

        public void MarkRecursively()
        {
            while (_processPending.Count != 0)
            {
                var component = _processPending.Dequeue();
                var dependencies = _componentInfos.TryGetInfo(component);
                if (dependencies == null) continue; // not part of this Hierarchy Tree

                foreach (var (dependency, type) in dependencies.Dependencies)
                    MarkComponent(dependency, type);
            }
        }
    }

    internal readonly struct FindUnusedObjectsProcessor
    {
        private readonly BuildContext _context;
        private readonly HashSet<GameObject?> _exclusions;
        private readonly bool _preserveEndBone;
        private readonly bool _noConfigureMergeBone;
        private readonly bool _noActivenessAnimation;
        private readonly bool _skipRemoveUnusedSubMesh;
        private readonly bool _gcDebug;

        public FindUnusedObjectsProcessor(BuildContext context, TraceAndOptimizeState state)
        {
            _context = context;

            _preserveEndBone = state.PreserveEndBone;
            _noConfigureMergeBone = state.NoConfigureMergeBone;
            _noActivenessAnimation = state.NoActivenessAnimation;
            _skipRemoveUnusedSubMesh = state.SkipRemoveUnusedSubMesh;
            _gcDebug = state.GCDebug;
            _exclusions = state.Exclusions;
        }

        public void ProcessNew()
        {
            var componentInfos = new GCComponentInfoHolder(_context);
            Mark(componentInfos);
            if (_gcDebug)
            {
                GCDebug.AddGCDebugInfo(componentInfos, _context.AvatarRootObject);
                return;
            }
            Sweep(componentInfos);
            if (!_noConfigureMergeBone)
                MergeBone(componentInfos);
            MarkDependant(componentInfos);
            if (!_noActivenessAnimation)
                ActivenessAnimation(componentInfos);
            if (!_skipRemoveUnusedSubMesh)
                RemoveUnusedSubMeshes(componentInfos);
        }

        private void ActivenessAnimation(GCComponentInfoHolder componentInfos)
        {
            // entrypoint -> affected activeness animated components / GameObjects
            Dictionary<Component, HashSet<Component>> entryPointActiveness =
                new Dictionary<Component, HashSet<Component>>();

            foreach (var componentInfo in componentInfos.AllInformation)
            { 
                if (!componentInfo.Component) continue; // swept
                if (componentInfo.IsEntrypoint) continue;
                if (!componentInfo.HeavyBehaviourComponent) continue;
                if (_context.GetAnimationComponent(componentInfo.Component).ContainsAnimationForFloat(Props.EnabledFor(componentInfo.Component)))
                    continue; // enabled is animated so we will not generate activeness animation

                HashSet<Component> resultSet;
                using (var enumerator = componentInfo.DependantComponents.GetEnumerator())
                {
                    Utils.Assert(enumerator.MoveNext());
                    resultSet = GetEntrypointActiveness(enumerator.Current, _context);

                    // resultSet.Count == 0 => no longer meaning
                    if (enumerator.MoveNext() && resultSet.Count != 0)
                    {
                        resultSet = new HashSet<Component>(resultSet);

                        do
                        {
                            var component = enumerator.Current;
                            if (component == null) continue;
                            if (component == componentInfo.Component) continue;
                            var current = GetEntrypointActiveness(component, _context);
                            resultSet.IntersectWith(current);
                        } while (enumerator.MoveNext() && resultSet.Count != 0);
                    }
                }

                if (resultSet.Count == 0)
                    continue; // there are no common activeness animation

                resultSet.Remove(componentInfo.Component.transform);
                resultSet.ExceptWith(componentInfo.Component.transform.ParentEnumerable());

                Component? commonActiveness;
                // TODO: we may use all activeness with nested identity transform
                // if activeness animation is not changed
                if (resultSet.Count == 0)
                {
                    // the only activeness is parent of this component so adding animation is not required
                    continue;
                }
                if (resultSet.Count == 1)
                {
                    commonActiveness = resultSet.First();
                }
                else
                {
                    // TODO: currently this is using most-child component but I don't know is this the best.
                    var nonTransform = resultSet.FirstOrDefault(x => !(x is Transform));
                    if (nonTransform != null)
                    {
                        // unlikely: deepest common activeness is the entrypoint component.
                        commonActiveness = nonTransform;
                    }
                    else
                    {
                        // likely: deepest common activeness is parent
                        commonActiveness = null;
                        foreach (var component in resultSet)
                        {
                            var transform = (Transform)component;
                            if (commonActiveness == null) commonActiveness = transform;
                            else
                            {
                                // if commonActiveness is parent of transform, transform is children of commonActiveness
                                if (transform.ParentEnumerable().Contains(commonActiveness))
                                    commonActiveness = transform;
                            }
                        }

                        Utils.Assert(commonActiveness != null);
                    }
                }

                if (commonActiveness is Transform)
                {
                    _context.Extension<ObjectMappingContext>().MappingBuilder!
                        .RecordCopyProperty(commonActiveness.gameObject, Props.IsActive,
                            componentInfo.Component, Props.EnabledFor(componentInfo.Component));
                }
                else
                {
                    _context.Extension<ObjectMappingContext>().MappingBuilder!
                        .RecordCopyProperty(commonActiveness, Props.EnabledFor(commonActiveness),
                            componentInfo.Component, Props.EnabledFor(componentInfo.Component));
                }
            }

            return;

            HashSet<Component> GetEntrypointActiveness(Component entryPoint, BuildContext context)
            {
                if (entryPointActiveness.TryGetValue(entryPoint, out var found))
                    return found;
                var set = new HashSet<Component>();

                if (context.GetAnimationComponent(entryPoint).ContainsAnimationForFloat(Props.EnabledFor(entryPoint)))
                    set.Add(entryPoint);

                for (var transform = entryPoint.transform;
                     transform != context.AvatarRootTransform;
                     transform = transform.parent)
                    if (context.GetAnimationComponent(transform.gameObject).ContainsAnimationForFloat(Props.IsActive))
                        set.Add(transform);

                entryPointActiveness.Add(entryPoint, set);
                return set;
            }
        }

        private void RemoveUnusedSubMeshes(GCComponentInfoHolder componentInfos)
        {
            foreach (var renderer in _context.AvatarRootObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!renderer) continue;
                var meshInfo2 = _context.GetMeshInfoFor(renderer);
                if (componentInfos.TryGetInfo(renderer) is not {} componentInfo) continue;
                if (componentInfo.DependantEntrypoint.Count == 1 &&
                    componentInfo.DependantEntrypoint.ContainsKey(renderer))
                {
                    // The SkinnedMeshRenderer is only used by itself, therefore it is safe to remove subMeshes without materials.
                    // Removing subMeshes without materials was performed in MeshInfo2 until 1.8.7 (inclusive).
                    // However, it broke particle system with SkinnedMeshRenderer so we remove here instead.
                    // TODO: move to early as possible to perform removing unused blendShapes and bones after this deletion
                    meshInfo2.SubMeshes.RemoveAll(x => x.SharedMaterials.Length == 0);
                }
            }
        }

        private void Mark(GCComponentInfoHolder componentInfos)
        {
            // first, collect usages
            
            new ComponentDependencyCollector(_context, _preserveEndBone, componentInfos).CollectAllUsages();

            // then, mark and sweep.

            // entrypoint for mark & sweep is active-able GameObjects
            foreach (var componentInfo in componentInfos.AllInformation)
            {
                if (componentInfo.IsEntrypoint)
                {
                    var component = componentInfo.Component;

                    var markContext = new MarkObjectContext(componentInfos, component, GetDependantMap);
                    markContext.MarkComponent(component, GCComponentInfo.DependencyType.Normal);
                    markContext.MarkRecursively();
                }
            }

            if (_exclusions.Count != 0) {
                // excluded GameObjects must be exists
                var markContext = new MarkObjectContext(componentInfos, _context.AvatarRootTransform, GetDependantMap);

                foreach (var gameObject in _exclusions)
                    if (gameObject != null)
                        foreach (var component in gameObject.GetComponents<Component>())
                            markContext.MarkComponent(component, GCComponentInfo.DependencyType.Normal);

                markContext.MarkRecursively();
            }

            Dictionary<Component, GCComponentInfo.DependencyType> GetDependantMap(GCComponentInfo x) =>
                x.DependantEntrypoint;
        }

        private void Sweep(GCComponentInfoHolder componentInfos)
        {
            foreach (var componentInfo in componentInfos.AllInformation)
            {
                // null values are ignored
                if (!componentInfo.Component) continue;

                if (componentInfo.DependantEntrypoint.Count == 0)
                {
                    if (componentInfo.Component is Transform)
                    {
                        // Treat Transform Component as GameObject because they are two sides of the same coin
                        DestroyTracker.DestroyImmediate(componentInfo.Component.gameObject);
                    }
                    else
                    {
                        DestroyWithDependencies(componentInfo.Component);
                    }
                }
            }
        }

        private static void DestroyWithDependencies(Component component)
        {
            if (component == null) return;
            foreach (var dependantType in RequireComponentCache.GetDependantComponents(component.GetType()))
            foreach (var child in component.GetComponents(dependantType))
                DestroyWithDependencies(child);
            DestroyTracker.DestroyImmediate(component);
        }

        private void MarkDependant(GCComponentInfoHolder componentInfos)
        {
            // entrypoint for mark & sweep is active-able GameObjects
            foreach (var componentInfo in componentInfos.AllInformation)
            {
                if (componentInfo.BehaviourComponent)
                {
                    var component = componentInfo.Component;
                    var markContext = new MarkObjectContext(componentInfos, component, GetDependantMap);
                    markContext.MarkComponent(component, GCComponentInfo.DependencyType.Normal);
                    markContext.MarkRecursively();
                }
            }

            if (_exclusions.Count != 0) {
                // excluded GameObjects must be exists
                var markContext = new MarkObjectContext(componentInfos, _context.AvatarRootTransform, GetDependantMap);

                foreach (var gameObject in _exclusions)
                    if (gameObject != null)
                        foreach (var component in gameObject.GetComponents<Component>())
                            markContext.MarkComponent(component, GCComponentInfo.DependencyType.Normal);

                markContext.MarkRecursively();
            }

            Dictionary<Component, GCComponentInfo.DependencyType> GetDependantMap(GCComponentInfo x) =>
                x.DependantBehaviours;
        }

        private void MergeBone(GCComponentInfoHolder componentInfos)
        {
            ConfigureRecursive(_context.AvatarRootTransform, _context);

            // returns (original mergedChildren, list of merged children if merged, and null if not merged)
            (bool, List<Transform>?) ConfigureRecursive(Transform transform, BuildContext context)
            {
                var mergedChildren = true;
                var afterChildren = new List<Transform>();
                foreach (var child in transform.DirectChildrenEnumerable())
                {
                    var (newMergedChildren, newChildren) = ConfigureRecursive(child, context);
                    if (newChildren == null)
                    {
                        mergedChildren = false;
                        afterChildren.Add(child);
                    }
                    else
                    {
                        mergedChildren &= newMergedChildren;
                        afterChildren.AddRange(newChildren);
                    }
                }

                const GCComponentInfo.DependencyType AllowedUsages =
                    GCComponentInfo.DependencyType.Bone
                    | GCComponentInfo.DependencyType.Parent
                    | GCComponentInfo.DependencyType.ComponentToTransform;

                // functions for make it easier to know meaning of result
                (bool, List<Transform>?) YesMerge() => (mergedChildren, afterChildren);
                (bool, List<Transform>?) NotMerged() => (mergedChildren, null);

                // Already Merged
                if (transform.TryGetComponent<MergeBone>(out _)) return YesMerge();
                // Components must be Transform Only
                if (transform.GetComponents<Component>().Length != 1) return NotMerged();
                // The bone cannot be used generally
                if ((componentInfos.GetInfo(transform).AllEntrypointUsages & ~AllowedUsages) != 0) return NotMerged();
                // must not be animated
                if (TransformAnimated(transform, context)) return NotMerged();

                if (!mergedChildren)
                {
                    if (GameObjectAnimated(transform, context)) return NotMerged();

                    var localScale = transform.localScale;
                    var identityTransform = localScale == Vector3.one && transform.localPosition == Vector3.zero &&
                                            transform.localRotation == Quaternion.identity;

                    if (!identityTransform)
                    {
                        var childrenTransformAnimated = afterChildren.Any(x => TransformAnimated(x, context));
                        if (childrenTransformAnimated)
                            // if this is not identity transform, animating children is not good
                            return NotMerged();

                        if (!Utils.ScaledEvenly(localScale))
                            // non even scaling is not possible to reproduce in children
                            return NotMerged();
                    }
                }

                if (!transform.gameObject.GetComponent<MergeBone>())
                    transform.gameObject.AddComponent<MergeBone>().avoidNameConflict = true;

                return YesMerge();
            }

            bool TransformAnimated(Transform transform, BuildContext context)
            {
                var transformProperties = context.GetAnimationComponent(transform);
                // TODO: constant animation detection
                foreach (var transformProperty in TransformProperties)
                    if (transformProperties.IsAnimatedFloat(transformProperty))
                        return true;

                return false;
            }

            bool GameObjectAnimated(Transform transform, BuildContext context)
            {
                var objectProperties = context.GetAnimationComponent(transform.gameObject);

                if (objectProperties.IsAnimatedFloat(Props.IsActive))
                    return true;

                return false;
            }
        }

        private static readonly string[] TransformProperties =
        {
            "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w",
            "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", 
            "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z", 
            // Animator Window won't create the following properties, but generated by some scripts and works in runtime
            "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w",
            "localPosition.x", "localPosition.y", "localPosition.z", 
            "localScale.x", "localScale.y", "localScale.z", 
            "localEulerAnglesRaw.x", "localEulerAnglesRaw.y", "localEulerAnglesRaw.z"
        };
    }
}
