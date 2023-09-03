using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

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
                    var deps = GetDependencies(component);
                    parser(this, deps, component);
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

        delegate void ComponentParser<in TComponent>(ComponentDependencyCollector collector, ComponentDependencies deps,
            TComponent component);

        private static readonly Dictionary<Type, ComponentParser<Component>> _byTypeParser =
            new Dictionary<Type, ComponentParser<Component>>();

        private static void AddParser<T>(ComponentParser<T> parser) where T : Component
        {
            _byTypeParser.Add(typeof(T), (collector, deps, component) => parser(collector, deps, (T)component));
        }

        private static void AddParserWithExtends<TParent, TChild>(ComponentParser<TChild> parser) 
            where TParent : Component
            where TChild : TParent
        {
            var parentParser = _byTypeParser[typeof(TParent)];
            _byTypeParser.Add(typeof(TChild), (collector, deps, component) =>
            {
                parentParser(collector, deps, component);
                parser(collector, deps, (TChild)component);
            });
        }

        private static void AddNopParser<T>() where T : Component
        {
            _byTypeParser.Add(typeof(T), (collector, deps, component) => { });
        }

        private static void AddParserWithExtends<TParent, TChild>()
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
            // unity generic
            AddParser<Transform>((collector, deps, transform) =>
            {
                collector.GetDependencies(transform.gameObject).AlwaysDependency.Add(transform);
            });
            // Animator does not do much for motion, just changes states of other components.
            // All State Changes are collected separately
            AddNopParser<Animator>();
            AddNopParser<Animation>();
            AddParser<Renderer>((collector, deps, renderer) =>
            {
                // anchor proves
                if (renderer.reflectionProbeUsage != ReflectionProbeUsage.Off ||
                    renderer.lightProbeUsage != LightProbeUsage.Off)
                    deps.ActiveDependency.Add(renderer.probeAnchor);
                if (renderer.lightProbeUsage != LightProbeUsage.UseProxyVolume)
                    deps.ActiveDependency.Add(renderer.lightProbeProxyVolumeOverride);
            });
            AddParserWithExtends<Renderer, SkinnedMeshRenderer>((collector, deps, skinnedMeshRenderer) =>
            {
                deps.ActiveDependency.UnionWith(skinnedMeshRenderer.bones.Where(x => x)
                    .Select(x => (ComponentOrGameObject)x));
                if (skinnedMeshRenderer.rootBone) deps.ActiveDependency.Add(skinnedMeshRenderer.rootBone);
            });
            AddParserWithExtends<Renderer, MeshRenderer>();
            AddNopParser<MeshFilter>();
            AddParser<ParticleSystem>((collector, deps, particleSystem) =>
            {
                if (particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.Custom)
                    deps.ActiveDependency.Add(particleSystem.main.customSimulationSpace);
                if (particleSystem.shape.enabled)
                {
                    switch (particleSystem.shape.shapeType)
                    {
                        case ParticleSystemShapeType.MeshRenderer:
                            if (particleSystem.shape.meshRenderer)
                                deps.ActiveDependency.Add(particleSystem.shape.meshRenderer);
                            break;
                        case ParticleSystemShapeType.SkinnedMeshRenderer:
                            if (particleSystem.shape.skinnedMeshRenderer)
                                deps.ActiveDependency.Add(particleSystem.shape.skinnedMeshRenderer);
                            break;
                        case ParticleSystemShapeType.SpriteRenderer:
                            if (particleSystem.shape.spriteRenderer)
                                deps.ActiveDependency.Add(particleSystem.shape.spriteRenderer);
                            break;
#pragma warning disable CS0618
                        case ParticleSystemShapeType.Sphere:
                        case ParticleSystemShapeType.SphereShell:
                        case ParticleSystemShapeType.Hemisphere:
                        case ParticleSystemShapeType.HemisphereShell:
                        case ParticleSystemShapeType.Cone:
                        case ParticleSystemShapeType.Box:
                        case ParticleSystemShapeType.Mesh:
                        case ParticleSystemShapeType.ConeShell:
                        case ParticleSystemShapeType.ConeVolume:
                        case ParticleSystemShapeType.ConeVolumeShell:
                        case ParticleSystemShapeType.Circle:
                        case ParticleSystemShapeType.CircleEdge:
                        case ParticleSystemShapeType.SingleSidedEdge:
                        case ParticleSystemShapeType.BoxShell:
                        case ParticleSystemShapeType.BoxEdge:
                        case ParticleSystemShapeType.Donut:
                        case ParticleSystemShapeType.Rectangle:
                        case ParticleSystemShapeType.Sprite:
                        default:
#pragma warning restore CS0618
                            break;
                    }
                }

                if (particleSystem.collision.enabled)
                {
                    switch (particleSystem.collision.type)
                    {
                        case ParticleSystemCollisionType.Planes:
                            for (var i = 0; i < particleSystem.collision.maxPlaneCount; i++)
                            {
                                var plane = particleSystem.collision.GetPlane(i);
                                if (plane) deps.ActiveDependency.Add(plane);
                            }

                            break;
                        case ParticleSystemCollisionType.World:
                        default:
                            break;
                    }
                }

                if (particleSystem.trigger.enabled)
                {
                    for (var i = 0; i < particleSystem.trigger.maxColliderCount; i++)
                    {
                        var collider = particleSystem.trigger.GetCollider(i);
                        if (collider) deps.ActiveDependency.Add(collider);
                    }
                }

                if (particleSystem.subEmitters.enabled)
                {
                    for (int i = 0; i < particleSystem.subEmitters.subEmittersCount; i++)
                    {
                        var subEmitter = particleSystem.subEmitters.GetSubEmitterSystem(i);
                        if (subEmitter) deps.ActiveDependency.Add(subEmitter);
                    }
                }

                if (particleSystem.lights.enabled)
                {
                    var light = particleSystem.lights.light;
                    if (light)
                        deps.ActiveDependency.Add(light);
                }
            });
            AddParserWithExtends<Renderer, ParticleSystemRenderer>();
        }

        #endregion
    }
}