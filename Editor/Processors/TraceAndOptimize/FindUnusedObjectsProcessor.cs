using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
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

    internal class ActivenessCache
    {
        private readonly ImmutableModificationsContainer _modifications;
        private readonly Dictionary<Component, bool?> _activeNessCache;
        private Transform _avatarRoot;

        public ActivenessCache(ImmutableModificationsContainer modifications, Transform avatarRoot)
        {
            _modifications = modifications;
            _avatarRoot = avatarRoot;
            _activeNessCache = new Dictionary<Component, bool?>();
        }

        public bool? GetActiveness(Component component)
        {
            if (_activeNessCache.TryGetValue(component, out var activeness))
                return activeness;
            activeness = ComputeActiveness(component);
            _activeNessCache.Add(component, activeness);
            return activeness;
        }

        private bool? ComputeActiveness(Component component)
        {
            if (_avatarRoot == component) return true;
            bool? parentActiveness;
            if (component is Transform t)
                parentActiveness = t.parent == null ? true : GetActiveness(t.parent);
            else
                parentActiveness = GetActiveness(component.transform);
            if (parentActiveness == false) return false;

            bool? activeness;
            switch (component)
            {
                case Transform transform:
                    var gameObject = transform.gameObject;
                    activeness = _modifications.GetConstantValue(gameObject, "m_IsActive", gameObject.activeSelf);
                    break;
                case Behaviour behaviour:
                    activeness = _modifications.GetConstantValue(behaviour, "m_Enabled", behaviour.enabled);
                    break;
                case Cloth cloth:
                    activeness = _modifications.GetConstantValue(cloth, "m_Enabled", cloth.enabled);
                    break;
                case Collider collider:
                    activeness = _modifications.GetConstantValue(collider, "m_Enabled", collider.enabled);
                    break;
                case LODGroup lodGroup:
                    activeness = _modifications.GetConstantValue(lodGroup, "m_Enabled", lodGroup.enabled);
                    break;
                case Renderer renderer:
                    activeness = _modifications.GetConstantValue(renderer, "m_Enabled", renderer.enabled);
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

    internal readonly struct MarkObjectContext {
        private readonly ComponentDependencyCollector _dependencies;
        private readonly ActivenessCache _activenessCache;

        public readonly Dictionary<Component, ComponentDependencyCollector.DependencyType> _marked;
        private readonly Queue<(Component, bool)> _processPending;

        public MarkObjectContext(ComponentDependencyCollector dependencies, ActivenessCache activenessCache)
        {
            _dependencies = dependencies;
            _activenessCache = activenessCache;

            _marked = new Dictionary<Component, ComponentDependencyCollector.DependencyType>();
            _processPending = new Queue<(Component, bool)>();
        }

        public void MarkComponent(Component component,
            bool ifTargetCanBeEnabled,
            ComponentDependencyCollector.DependencyType type)
        {
            bool? activeness = _activenessCache.GetActiveness(component);

            if (ifTargetCanBeEnabled && activeness == false)
                return; // The Target is not active so not dependency

            if (_marked.TryGetValue(component, out var existingFlags))
            {
                _marked[component] = existingFlags | type;
            }
            else
            {
                _processPending.Enqueue((component, activeness != false));
                _marked.Add(component, type);
            }
        }

        public void MarkRecursively()
        {
            while (_processPending.Count != 0)
            {
                var (component, canBeActive) = _processPending.Dequeue();
                var dependencies = _dependencies.TryGetDependencies(component);
                if (dependencies == null) continue; // not part of this Hierarchy Tree

                foreach (var (dependency, flags) in dependencies.Dependencies)
                {
                    var ifActive =
                        (flags.flags & ComponentDependencyCollector.DependencyFlags.EvenIfThisIsDisabled) == 0;
                    if (ifActive && !canBeActive) continue;
                    var ifTargetCanBeEnabled =
                        (flags.flags & ComponentDependencyCollector.DependencyFlags.EvenIfTargetIsDisabled) == 0;
                    MarkComponent(dependency, ifTargetCanBeEnabled, flags.type);
                }
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
            // first, collect usages
            var collector = new ComponentDependencyCollector(_context, _preserveEndBone);
            collector.CollectAllUsages();

            var activenessCache = new ActivenessCache(_modifications, _context.AvatarRootTransform);

            var markContext = new MarkObjectContext(collector, activenessCache);
            // then, mark and sweep.

            // entrypoint for mark & sweep is active-able GameObjects
            foreach (var gameObject in CollectAllActiveAbleGameObjects())
            foreach (var component in gameObject.GetComponents<Component>())
                if (collector.GetDependencies(component).EntrypointComponent)
                    markContext.MarkComponent(component, true, ComponentDependencyCollector.DependencyType.Normal);

            // excluded GameObjects must be exists
            foreach (var gameObject in _exclusions)
            foreach (var component in gameObject.GetComponents<Component>())
                markContext.MarkComponent(component, true, ComponentDependencyCollector.DependencyType.Normal);

            markContext.MarkRecursively();

            foreach (var component in _context.GetComponents<Component>())
            {
                // null values are ignored
                if (!component) continue;

                if (component is Transform)
                {
                    // Treat Transform Component as GameObject because they are two sides of the same coin
                    if (!markContext._marked.ContainsKey(component))
                        Object.DestroyImmediate(component.gameObject);
                }
                else
                {
                    if (!markContext._marked.ContainsKey(component))
                        Object.DestroyImmediate(component);
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

                const ComponentDependencyCollector.DependencyType AllowedUsages =
                    ComponentDependencyCollector.DependencyType.Bone
                    | ComponentDependencyCollector.DependencyType.Parent
                    | ComponentDependencyCollector.DependencyType.ComponentToTransform;

                // functions for make it easier to know meaning of result
                (bool, List<Transform>) YesMerge() => (mergedChildren, afterChildren);
                (bool, List<Transform>) NotMerged() => (mergedChildren, null);

                // Already Merged
                if (transform.GetComponent<MergeBone>()) return YesMerge();
                // Components must be Transform Only
                if (transform.GetComponents<Component>().Length != 1) return NotMerged();
                // The bone cannot be used generally
                if ((markContext._marked[transform] & ~AllowedUsages) != 0) return NotMerged();
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
