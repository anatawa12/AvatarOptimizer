using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    /// <summary>
    /// Implements legacyGC of Find Unused Objects
    /// </summary>
    internal struct LegacyGC
    {
        private readonly ImmutableModificationsContainer _modifications;
        private readonly BuildContext _context;
        private readonly HashSet<GameObject> _exclusions;

        public LegacyGC(BuildContext context, TraceAndOptimizeState state)
        {
            _context = context;

            _modifications = state.Modifications;
            _exclusions = state.Exclusions;
        }

        public void Process() {
            // mark & sweep
            var gameObjects = new HashSet<GameObject>(_context.GetComponents<Transform>().Select(x => x.gameObject));
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
                        foreach (var iter in serialized.ObjectReferenceProperties())
                        {
                            var value = iter.objectReferenceValue;
                            if (value is Component c && !EditorUtility.IsPersistent(value))
                                AddGameObject(c.gameObject);
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