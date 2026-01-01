using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// Singleton ScriptableObject to manage Avatar Optimizer global settings.
    /// Stored in ProjectSettings directory.
    /// </summary>
    [FilePath("ProjectSettings/AvatarOptimizerSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class AvatarOptimizerSettings : ScriptableSingleton<AvatarOptimizerSettings>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private MonoScript[] ignoredComponents = Array.Empty<MonoScript>();

        private HashSet<Type>? _ignoredComponentSetCache;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _ignoredComponentSetCache = null;
        }

        public void Save() => Save(false);

        /// <summary>
        /// Get the set of ignored component types
        /// </summary>
        public HashSet<Type> GetIgnoredComponentTypes()
        {
            if (_ignoredComponentSetCache == null)
            {
                _ignoredComponentSetCache = ignoredComponents.Where(script => script != null)
                    .Select(script => script.GetClass())
                    .Where(type => type != null)
                    .ToHashSet();
            }

            return _ignoredComponentSetCache;
        }

        /// <summary>
        /// Check if a component type is ignored
        /// </summary>
        public bool IsIgnored(Type type) => GetIgnoredComponentTypes().Contains(type);

        /// <summary>
        /// Add a MonoScript to the ignored list
        /// </summary>
        public void AddIgnoredComponent(MonoScript script)
        {
            if (script == null) return;
            
            // Check if already exists
            if (ignoredComponents.Any(s => s == script))
                return;

            Undo.RecordObject(this, "Add Ignored Component");
            _ignoredComponentSetCache = null;
            ArrayUtility.Add(ref ignoredComponents, script);
            Save(true);
        }

        /// <summary>
        /// Remove a MonoScript from the ignored list
        /// </summary>
        public void RemoveIgnoredComponent(MonoScript script)
        {
            if (script == null) return;

            Undo.RecordObject(this, "Remove Ignored Component");
            _ignoredComponentSetCache = null;
            ArrayUtility.Remove(ref ignoredComponents, script);
            Save(true);
        }

        /// <summary>
        /// Get all ignored MonoScripts
        /// </summary>
        public MonoScript[] GetIgnoredComponents() => ignoredComponents.ToArray();

        /// <summary>
        /// Clear all ignored components
        /// </summary>
        public void ClearIgnoredComponents()
        {
            Undo.RecordObject(this, "Clear Ignored Components");
            _ignoredComponentSetCache = null;
            ignoredComponents = Array.Empty<MonoScript>();
            Save(true);
        }
    }
}
