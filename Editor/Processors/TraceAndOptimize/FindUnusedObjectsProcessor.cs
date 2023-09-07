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
        private readonly bool _useLegacyGC;

        public FindUnusedObjectsProcessor(ImmutableModificationsContainer modifications, OptimizerSession session,
            bool useLegacyGC,
            HashSet<GameObject> exclusions)
        {
            _modifications = modifications;
            _session = session;
            _useLegacyGC = useLegacyGC;
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

        private void MarkComponent(Component component,
            bool ifTargetCanBeEnabled,
            ComponentDependencyCollector.DependencyType type)
        {
            bool? activeNess;
            switch (component)
            {
                case Transform transform:
                    var gameObject = transform.gameObject;
                    activeNess = _modifications.GetConstantValue(gameObject, "m_IsActive", gameObject.activeSelf);
                    break;
                case Cloth cloth:
                    activeNess = _modifications.GetConstantValue(cloth, "m_IsEnable", cloth.enabled);
                    break;
                case Renderer cloth:
                    activeNess = _modifications.GetConstantValue(cloth, "m_IsEnable", cloth.enabled);
                    break;
                case Behaviour behaviour:
                    activeNess = _modifications.GetConstantValue(behaviour, "m_IsEnable", behaviour.enabled);
                    break;
                case Component _:
                    activeNess = null;
                    break;
                default:
                    throw new Exception($"Unexpected type: {component.GetType().Name}");
            }

            if (ifTargetCanBeEnabled && activeNess == false)
                return; // The Target is not active so not dependency

            if (_marked.TryGetValue(component, out var existingFlags))
            {
                _marked[component] = existingFlags | type;
            }
            else
            {
                _processPending.Enqueue((component, activeNess != false));
                _marked.Add(component, type);
            }
        }

        private void ProcessNew()
        {
            // first, collect usages
            var collector = new ComponentDependencyCollector(_session);
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