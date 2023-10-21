using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class FindUnusedObjects : Pass<FindUnusedObjects>
    {
        public override string DisplayName => "T&O: FindUnusedObjects";

        protected override void Execute(BuildContext context)
        {
            var state = context.GetState<TraceAndOptimizeState>();
            if (!state.RemoveUnusedObjects) return;

            if (state.UseLegacyGC)
            {
                new LegacyGC().Process();
            }
            else
            {
                var processor = new FindUnusedObjectsProcessor(context, state);
                //if (state.GCDebug)
                //    processor.CollectDataForGc();
                //else
                    processor.ProcessNew();
            }
        }
    }

    internal readonly struct MarkObjectContext {
        private readonly GCComponentInfoHolder _componentInfos;

        private readonly Queue<Component> _processPending;
        private readonly Component _entrypoint;

        public MarkObjectContext(GCComponentInfoHolder componentInfos, Component entrypoint)
        {
            _componentInfos = componentInfos;
            _processPending = new Queue<Component>();
            _entrypoint = entrypoint;
        }

        public void MarkComponent(Component component,
            GCComponentInfo.DependencyType type)
        {
            var dependencies = _componentInfos.TryGetInfo(component);
            if (dependencies == null) return;

            if (dependencies.DependantEntrypoint.TryGetValue(_entrypoint, out var existingFlags))
            {
                dependencies.DependantEntrypoint[_entrypoint] = existingFlags | type;
            }
            else
            {
                _processPending.Enqueue(component);
                dependencies.DependantEntrypoint.Add(_entrypoint, type);
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
        private readonly ImmutableModificationsContainer _modifications;
        private readonly BuildContext _context;
        private readonly HashSet<GameObject> _exclusions;
        private readonly bool _preserveEndBone;
        private readonly bool _noConfigureMergeBone;

        public FindUnusedObjectsProcessor(BuildContext context, TraceAndOptimizeState state)
        {
            _context = context;

            _modifications = state.Modifications;
            _preserveEndBone = state.PreserveEndBone;
            _noConfigureMergeBone = state.NoConfigureMergeBone;
            _exclusions = state.Exclusions;
        }

        public void ProcessNew()
        {
            var componentInfos = new GCComponentInfoHolder(_modifications, _context.AvatarRootObject);

            // first, collect usages
            var collector = new ComponentDependencyCollector(_context, _preserveEndBone, componentInfos);
            collector.CollectAllUsages();

            // then, mark and sweep.

            // entrypoint for mark & sweep is active-able GameObjects
            foreach (var gameObject in CollectAllActiveAbleGameObjects())
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (componentInfos.GetInfo(component).EntrypointComponent)
                {
                    var markContext = new MarkObjectContext(componentInfos, component);
                    markContext.MarkComponent(component, GCComponentInfo.DependencyType.Normal);
                    markContext.MarkRecursively();
                }
            }

            if (_exclusions.Count != 0) {
                // excluded GameObjects must be exists
                var markContext = new MarkObjectContext(componentInfos, _context.AvatarRootTransform);

                foreach (var gameObject in _exclusions)
                foreach (var component in gameObject.GetComponents<Component>())
                    markContext.MarkComponent(component, GCComponentInfo.DependencyType.Normal);

                markContext.MarkRecursively();
            }


            foreach (var component in _context.GetComponents<Component>())
            {
                // null values are ignored
                if (!component) continue;

                if (componentInfos.GetInfo(component).DependantEntrypoint.Count == 0)
                {
                    if (component is Transform)
                    {
                        // Treat Transform Component as GameObject because they are two sides of the same coin
                        Object.DestroyImmediate(component.gameObject);
                    }
                    else
                    {
                        Object.DestroyImmediate(component);
                    }
                }
            }

            if (_noConfigureMergeBone) return;

            ConfigureRecursive(_context.AvatarRootTransform, _modifications);

            // returns (original mergedChildren, list of merged children if merged, and null if not merged)
            //[CanBeNull]
            (bool, List<Transform>) ConfigureRecursive(Transform transform, ImmutableModificationsContainer modifications)
            {
                var mergedChildren = true;
                var afterChildren = new List<Transform>();
                foreach (var child in transform.DirectChildrenEnumerable())
                {
                    var (newMergedChildren, newChildren) = ConfigureRecursive(child, modifications);
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
                (bool, List<Transform>) YesMerge() => (mergedChildren, afterChildren);
                (bool, List<Transform>) NotMerged() => (mergedChildren, null);

                // Already Merged
                if (transform.GetComponent<MergeBone>()) return YesMerge();
                // Components must be Transform Only
                if (transform.GetComponents<Component>().Length != 1) return NotMerged();
                // The bone cannot be used generally
                if ((componentInfos.GetInfo(transform).AllUsages & ~AllowedUsages) != 0) return NotMerged();
                // must not be animated
                if (TransformAnimated(transform, modifications)) return NotMerged();

                if (!mergedChildren)
                {
                    if (GameObjectAnimated(transform, modifications)) return NotMerged();

                    var localScale = transform.localScale;
                    var identityTransform = localScale == Vector3.one && transform.localPosition == Vector3.zero &&
                                            transform.localRotation == Quaternion.identity;

                    if (!identityTransform)
                    {
                        var childrenTransformAnimated = afterChildren.Any(x => TransformAnimated(x, modifications));
                        if (childrenTransformAnimated)
                            // if this is not identity transform, animating children is not good
                            return NotMerged();

                        if (!MergeBoneProcessor.ScaledEvenly(localScale))
                            // non even scaling is not possible to reproduce in children
                            return NotMerged();
                    }
                }

                if (!transform.gameObject.GetComponent<MergeBone>())
                    transform.gameObject.AddComponent<MergeBone>().avoidNameConflict = true;

                return YesMerge();
            }

            bool TransformAnimated(Transform transform, ImmutableModificationsContainer modifications)
            {
                var transformProperties = modifications.GetModifiedProperties(transform);
                if (transformProperties.Count != 0)
                {
                    // TODO: constant animation detection
                    foreach (var transformProperty in TransformProperties)
                        if (transformProperties.ContainsKey(transformProperty))
                            return true;
                }

                return false;
            }

            bool GameObjectAnimated(Transform transform, ImmutableModificationsContainer modifications)
            {
                var objectProperties = modifications.GetModifiedProperties(transform.gameObject);

                if (objectProperties.ContainsKey("m_IsActive"))
                    return true;

                return false;
            }
        }

        private static readonly string[] TransformProperties =
        {
            "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w",
            "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", 
            "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z", 
            "localEulerAnglesRaw.x", "localEulerAnglesRaw.y", "localEulerAnglesRaw.z"
        };

        private IEnumerable<GameObject> CollectAllActiveAbleGameObjects()
        {
            var queue = new Queue<GameObject>();
            queue.Enqueue(_context.AvatarRootTransform.gameObject);

            while (queue.Count != 0)
            {
                var gameObject = queue.Dequeue();
                var activeNess = _modifications.GetConstantValue(gameObject, "m_IsActive", gameObject.activeSelf);
                switch (activeNess)
                {
                    case null:
                    case true:
                        // This GameObject can be active
                        yield return gameObject;
                        foreach (var transform in gameObject.transform.DirectChildrenEnumerable())
                            queue.Enqueue(transform.gameObject);
                        break;
                    case false:
                        // This GameObject and their children will never be active
                        break;
                }
            }
        }
    }
}
