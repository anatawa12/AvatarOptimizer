using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using Debug = System.Diagnostics.Debug;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    class FindUnusedObjectsProcessor
    {
        private readonly ImmutableModificationsContainer _modifications;
        private readonly OptimizerSession _session;
        private readonly HashSet<GameObject> _exclusions;
        private readonly bool _preserveEndBone;
        private readonly bool _useLegacyGC;
        private readonly bool _noConfigureMergeBone;

        public FindUnusedObjectsProcessor(ImmutableModificationsContainer modifications, OptimizerSession session,
            bool preserveEndBone,
            bool useLegacyGC,
            bool noConfigureMergeBone,
            HashSet<GameObject> exclusions)
        {
            _modifications = modifications;
            _session = session;
            _preserveEndBone = preserveEndBone;
            _useLegacyGC = useLegacyGC;
            _noConfigureMergeBone = noConfigureMergeBone;
            _exclusions = exclusions;
        }

        public void Process()
        {
            if (_useLegacyGC)
                ProcessLegacy();
            else
                ProcessNew();
        }

        // Mark & Sweep Variables
        private readonly Dictionary<Component, ComponentDependencyCollector.DependencyType> _marked =
            new Dictionary<Component, ComponentDependencyCollector.DependencyType>();
        private readonly Queue<(Component, bool)> _processPending = new Queue<(Component, bool)>();
        private readonly Dictionary<Component, bool?> _activeNessCache = new Dictionary<Component, bool?>();

        private bool? GetActiveness(Component component)
        {
            if (_activeNessCache.TryGetValue(component, out var activenessCached))
                return activenessCached;

            bool? activeness;
            switch (component)
            {
                case Transform transform:
                    var gameObject = transform.gameObject;
                    activeness = _modifications.GetConstantValue(gameObject, "m_IsActive", gameObject.activeSelf);
                    break;
                case Cloth cloth:
                    activeness = _modifications.GetConstantValue(cloth, "m_IsEnable", cloth.enabled);
                    break;
                case Renderer cloth:
                    activeness = _modifications.GetConstantValue(cloth, "m_IsEnable", cloth.enabled);
                    break;
                case Behaviour behaviour:
                    activeness = _modifications.GetConstantValue(behaviour, "m_IsEnable", behaviour.enabled);
                    break;
                case Component _:
                    activeness = null;
                    break;
                default:
                    throw new Exception($"Unexpected type: {component.GetType().Name}");
            }

            _activeNessCache.Add(component, activeness);

            return activeness;
        }

        private void MarkComponent(Component component,
            bool ifTargetCanBeEnabled,
            ComponentDependencyCollector.DependencyType type)
        {
            bool? activeness = GetActiveness(component);

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

        private void ProcessNew()
        {
            MarkAndSweep();
            if (!_noConfigureMergeBone) ConfigureMergeBone();
        }

        private void MarkAndSweep()
        {
            // first, collect usages
            var collector = new ComponentDependencyCollector(_session, _preserveEndBone);
            collector.CollectAllUsages();

            // then, mark and sweep.

            // entrypoint for mark & sweep is active-able GameObjects
            foreach (var gameObject in CollectAllActiveAbleGameObjects())
            foreach (var component in gameObject.GetComponents<Component>())
                if (collector.GetDependencies(component).EntrypointComponent)
                    MarkComponent(component, true, ComponentDependencyCollector.DependencyType.Normal);

            // excluded GameObjects must be exists
            foreach (var gameObject in _exclusions)
            foreach (var component in gameObject.GetComponents<Component>())
                MarkComponent(component, true, ComponentDependencyCollector.DependencyType.Normal);

            while (_processPending.Count != 0)
            {
                var (component, canBeActive) = _processPending.Dequeue();
                var dependencies = collector.TryGetDependencies(component);
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

            foreach (var component in _session.GetComponents<Component>())
            {
                // null values are ignored
                if (!component) continue;

                if (component is Transform)
                {
                    // Treat Transform Component as GameObject because they are two sides of the same coin
                    if (!_marked.ContainsKey(component))
                        Object.DestroyImmediate(component.gameObject);
                }
                else
                {
                    if (!_marked.ContainsKey(component))
                        Object.DestroyImmediate(component);
                }
            }
        }

        private void ConfigureMergeBone()
        {
            ConfigureRecursive(_session.GetRootComponent<Transform>(), _modifications);

            // returns true if merged
            bool ConfigureRecursive(Transform transform, ImmutableModificationsContainer modifications)
            {
                var mergedChildren = true;
                foreach (var child in transform.DirectChildrenEnumerable())
                    mergedChildren &= ConfigureRecursive(child, modifications);

                const ComponentDependencyCollector.DependencyType AllowedUsages =
                    ComponentDependencyCollector.DependencyType.Bone
                    | ComponentDependencyCollector.DependencyType.Parent
                    | ComponentDependencyCollector.DependencyType.ComponentToTransform;

                // Components must be Transform Only
                if (transform.GetComponents<Component>().Length != 1) return false;
                // The bone cannot be used generally
                if ((_marked[transform] & ~AllowedUsages) != 0) return false;
                // must not be animated
                if (Animated(transform, modifications)) return false;

                if (!mergedChildren)
                {
                    var localScale = transform.localScale;
                    if (localScale == Vector3.one)
                    {
                        // if this scale is one, Good.
                    }
                    else if (MergeBoneProcessor.ScaledEvenly(localScale) &&
                               transform.DirectChildrenEnumerable().All(x => !Animated(x, modifications)))
                    {
                        // if scale is even and direct children are not animated

                        // if direct children are animated, we have to adjust animation which is hard
                    }
                    else
                    {
                        return false;
                    }
                }

                transform.gameObject.GetOrAddComponent<MergeBone>();

                return true;
            }

            bool Animated(Transform transform, ImmutableModificationsContainer modifications)
            {
                var properties = modifications.GetModifiedProperties(transform);
                if (properties.Count == 0) return false;

                // TODO: constant animation detection
                
                foreach (var transformProperty in TransformProperties)
                    if (properties.ContainsKey(transformProperty))
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
            queue.Enqueue(_session.GetRootComponent<Transform>().gameObject);

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

        private void ProcessLegacy() {
            // mark & sweep
            var gameObjects = new HashSet<GameObject>(_session.GetComponents<Transform>().Select(x => x.gameObject));
            var referenced = new HashSet<GameObject>();
            var newReferenced = new Queue<GameObject>();

            void AddGameObject(GameObject gameObject)
            {
                if (gameObject && gameObjects.Contains(gameObject) && referenced.Add(gameObject))
                    newReferenced.Enqueue(gameObject);
            }

            // entry points: active GameObjects
            foreach (var component in gameObjects.Where(x => x.activeInHierarchy))
                AddGameObject(component);

            // entry points: modified enable/disable
            foreach (var keyValuePair in _modifications.ModifiedProperties)
            {
                // TODO: if the any of parent is inactive and kept, it should not be assumed as 
                if (!keyValuePair.Key.AsGameObject(out var gameObject)) continue;
                if (!keyValuePair.Value.TryGetValue("m_IsActive", out _)) continue;

                // TODO: if the child is not activeSelf, it should not be assumed as entry point.
                foreach (var transform in gameObject.GetComponentsInChildren<Transform>())
                    AddGameObject(transform.gameObject);
            }

            // entry points: active GameObjects
            foreach (var gameObject in _exclusions)
                AddGameObject(gameObject);

            while (newReferenced.Count != 0)
            {
                var gameObject = newReferenced.Dequeue();

                foreach (var component in gameObject.GetComponents<Component>())
                {
                    if (component is Transform transform)
                    {
                        if (transform.parent)
                            AddGameObject(transform.parent.gameObject);
                        continue;
                    }

                    if (component is VRCPhysBoneBase)
                    {
                        foreach (var child in component.GetComponentsInChildren<Transform>(true))
                            AddGameObject(child.gameObject);
                    }

                    using (var serialized = new SerializedObject(component))
                    {
                        var iter = serialized.GetIterator();
                        var enterChildren = true;
                        while (iter.Next(enterChildren))
                        {
                            if (iter.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                var value = iter.objectReferenceValue;
                                if (value is Component c && !EditorUtility.IsPersistent(value))
                                    AddGameObject(c.gameObject);
                            }

                            switch (iter.propertyType)
                            {
                                case SerializedPropertyType.Integer:
                                case SerializedPropertyType.Boolean:
                                case SerializedPropertyType.Float:
                                case SerializedPropertyType.String:
                                case SerializedPropertyType.Color:
                                case SerializedPropertyType.ObjectReference:
                                case SerializedPropertyType.Enum:
                                case SerializedPropertyType.Vector2:
                                case SerializedPropertyType.Vector3:
                                case SerializedPropertyType.Vector4:
                                case SerializedPropertyType.Rect:
                                case SerializedPropertyType.ArraySize:
                                case SerializedPropertyType.Character:
                                case SerializedPropertyType.Bounds:
                                case SerializedPropertyType.Quaternion:
                                case SerializedPropertyType.FixedBufferSize:
                                case SerializedPropertyType.Vector2Int:
                                case SerializedPropertyType.Vector3Int:
                                case SerializedPropertyType.RectInt:
                                case SerializedPropertyType.BoundsInt:
                                    enterChildren = false;
                                    break;
                                case SerializedPropertyType.Generic:
                                case SerializedPropertyType.LayerMask:
                                case SerializedPropertyType.AnimationCurve:
                                case SerializedPropertyType.Gradient:
                                case SerializedPropertyType.ExposedReference:
                                case SerializedPropertyType.ManagedReference:
                                default:
                                    enterChildren = true;
                                    break;
                            }
                        }
                    }
                }
            }

            // sweep
            foreach (var gameObject in gameObjects.Where(x => !referenced.Contains(x)))
            {
                if (gameObject)
                    Object.DestroyImmediate(gameObject);
            }
        }
    }
}