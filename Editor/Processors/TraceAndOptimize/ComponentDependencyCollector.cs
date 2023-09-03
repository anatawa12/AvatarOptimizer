using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    /// <summary>
    /// This class collects ALL dependencies of each component
    /// </summary>
    class ComponentDependencyCollector
    {
        static ComponentDependencyCollector()
        {
            InitByTypeParsers();
        }

        private readonly Dictionary<ComponentOrGameObject, ComponentDependencies> _dependencies =
            new Dictionary<ComponentOrGameObject, ComponentDependencies>();

        private class ComponentDependencies
        {
            /// <summary>
            /// If this is true, even if this component is required by some component,
            /// this component will never be collected.
            /// </summary>
            public bool NoMeaningIfDisabled = false;

            /// <summary>
            /// Dependencies if this component can be Active or Enabled
            /// </summary>
            [NotNull] public readonly HashSet<ComponentOrGameObject> ActiveDependency =
                new HashSet<ComponentOrGameObject>();

            /// <summary>
            /// Dependencies regardless this component can be Active/Enabled or not.
            /// </summary>
            [NotNull] public readonly HashSet<ComponentOrGameObject> AlwaysDependency =
                new HashSet<ComponentOrGameObject>();
        }

        [CanBeNull]
        private ComponentDependencies TryGetDependencies(ComponentOrGameObject dependent) =>
            _dependencies.TryGetValue(dependent, out var dependencies) ? dependencies : null;

        [NotNull]
        private ComponentDependencies GetDependencies(ComponentOrGameObject dependent) => _dependencies[dependent];

        public void CollectAllUsages(OptimizerSession session)
        {
            var components = session.GetComponents<Component>().ToArray();
            // first iteration: create mapping
            foreach (var component in components) _dependencies.Add(component, new ComponentDependencies());
            foreach (var transform in session.GetComponents<Transform>()) _dependencies.Add(transform.gameObject, new ComponentDependencies());

            // second iteration: process parsers
            BuildReport.ReportingObjects(components, component =>
            {
                // component requires GameObject.
                GetDependencies(component).AlwaysDependency.Add(component.gameObject);

                if (_byTypeParser.TryGetValue(component.GetType(), out var parser))
                {
                    parser(this, component);
                }
                else
                {
                    BuildReport.LogWarning(
                        "Unknown Component Type Found. This will reduce optimization performance. If possible, Please Report this for AvatarOptimizer!: {0}",
                        component.GetType().Name);

                    FallbackDependenciesParser(component);
                }
            });
        }

        private void FallbackDependenciesParser(Component component)
        {
            // fallback dependencies: All References are Always Dependencies.
            GetDependencies(component.gameObject).AlwaysDependency.Add(component);
            var dependencies = GetDependencies(component);
            using (var serialized = new SerializedObject(component))
            {
                var iterator = serialized.GetIterator();
                var enterChildren = true;
                while (iterator.Next(enterChildren))
                {
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (iterator.objectReferenceValue is GameObject go)
                            dependencies.AlwaysDependency.Add(go);
                        else if (iterator.objectReferenceValue is Component com)
                            dependencies.AlwaysDependency.Add(com);
                    }

                    switch (iterator.propertyType)
                    {
                        case SerializedPropertyType.String:
                        case SerializedPropertyType.Integer:
                        case SerializedPropertyType.Boolean:
                        case SerializedPropertyType.Float:
                        case SerializedPropertyType.Color:
                        case SerializedPropertyType.ObjectReference:
                        case SerializedPropertyType.LayerMask:
                        case SerializedPropertyType.Enum:
                        case SerializedPropertyType.Vector2:
                        case SerializedPropertyType.Vector3:
                        case SerializedPropertyType.Vector4:
                        case SerializedPropertyType.Rect:
                        case SerializedPropertyType.ArraySize:
                        case SerializedPropertyType.Character:
                        case SerializedPropertyType.AnimationCurve:
                        case SerializedPropertyType.Bounds:
                        case SerializedPropertyType.Gradient:
                        case SerializedPropertyType.Quaternion:
                        case SerializedPropertyType.FixedBufferSize:
                        case SerializedPropertyType.Vector2Int:
                        case SerializedPropertyType.Vector3Int:
                        case SerializedPropertyType.RectInt:
                        case SerializedPropertyType.BoundsInt:
                            enterChildren = false;
                            break;
                        case SerializedPropertyType.Generic:
                        case SerializedPropertyType.ExposedReference:
                        case SerializedPropertyType.ManagedReference:
                        default:
                            enterChildren = true;
                            break;
                    }
                }
            }
        }

        #region ByComponentMappingGeneration

        private static readonly Dictionary<Type, Action<ComponentDependencyCollector, Component>> _byTypeParser =
            new Dictionary<Type, Action<ComponentDependencyCollector, Component>>();

        private static void AddParser<T>(Action<ComponentDependencyCollector, T> parser) where T : Component
        {
            _byTypeParser.Add(typeof(T), (collector, component) => parser(collector, (T)component));
        }

        private static void UseParserOfParentClass<TParent, TChild>()
            where TParent : Component
            where TChild : TParent
        {
            _byTypeParser.Add(typeof(TChild), _byTypeParser[typeof(TParent)]);
        }

        #endregion

        #region ByType Parser

        /// <summary>
        /// Initializes _byTypeParser. This includes huge amount of definition for components.
        /// </summary>
        private static void InitByTypeParsers()
        {
            AddParser<Transform>((collector, transform) =>
            {
                collector.GetDependencies(transform.gameObject).AlwaysDependency.Add(transform);
            });
        }

        #endregion
    }
}