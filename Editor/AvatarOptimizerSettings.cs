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
    internal class AvatarOptimizerSettings : ScriptableSingleton<AvatarOptimizerSettings>
    {
        [SerializeField]
        private MonoScript[] ignoredComponents = Array.Empty<MonoScript>();

        /// <summary>
        /// Get the set of ignored component types
        /// </summary>
        public HashSet<Type> GetIgnoredComponentTypes()
        {
            var types = new HashSet<Type>();
            foreach (var script in ignoredComponents)
            {
                if (script != null)
                {
                    var type = script.GetClass();
                    if (type != null)
                        types.Add(type);
                }
            }
            return types;
        }

        /// <summary>
        /// Check if a component type is ignored
        /// </summary>
        public bool IsIgnored(Type type)
        {
            foreach (var script in ignoredComponents)
            {
                if (script != null && script.GetClass() == type)
                    return true;
            }
            return false;
        }

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
            var list = ignoredComponents.ToList();
            list.Add(script);
            ignoredComponents = list.ToArray();
            Save(true);
        }

        /// <summary>
        /// Remove a MonoScript from the ignored list
        /// </summary>
        public void RemoveIgnoredComponent(MonoScript script)
        {
            if (script == null) return;

            Undo.RecordObject(this, "Remove Ignored Component");
            var list = ignoredComponents.ToList();
            list.Remove(script);
            ignoredComponents = list.ToArray();
            Save(true);
        }

        /// <summary>
        /// Get all ignored MonoScripts
        /// </summary>
        public MonoScript[] GetIgnoredComponents()
        {
            return ignoredComponents.ToArray();
        }

        /// <summary>
        /// Clear all ignored components
        /// </summary>
        public void ClearIgnoredComponents()
        {
            Undo.RecordObject(this, "Clear Ignored Components");
            ignoredComponents = Array.Empty<MonoScript>();
            Save(true);
        }
    }
}
