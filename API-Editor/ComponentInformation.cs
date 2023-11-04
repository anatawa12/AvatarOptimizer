using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.API
{
    /// <summary>
    /// This attribute declares the class provides information about the type.
    /// Your class MUST derive <see cref="ComponentInformation{TComponent}"/> and MUST have constructor without parameters.
    /// The <see cref="TargetType"/> must be assignable to <c>TComponent</c> of <see cref="ComponentInformation{TComponent}"/>
    ///
    /// Your class MAY have one type parameter.
    /// When your class have type parameter, the type parameter MUST be passed to <see cref="ComponentInformation{TComponent}"/>
    /// and must be assignable from <see cref="TargetType"/>.
    ///
    /// It's REQUIRED to be exists only one ComponentInformation for one type.
    /// So, you SHOULD NOT declare ComponentInformation for external components.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    [MeansImplicitUse]
    [BaseTypeRequired(typeof(ComponentInformation<>))]
    public sealed class ComponentInformationAttribute : APIInternal.ComponentInformationAttributeBase
    {
        public Type TargetType { get; }

        [PublicAPI]
        public ComponentInformationAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        internal override Type GetTargetType() => TargetType;
    }

    [PublicAPI]
    public abstract class ComponentInformation<TComponent> :
        APIInternal.ComponentInformation,
        APIInternal.IComponentInformation<TComponent>
        where TComponent : Component
    {
        internal sealed override void CollectDependencyInternal(Component component,
            ComponentDependencyCollector collector) =>
            CollectDependency((TComponent)component, collector);

        internal sealed override void CollectMutationsInternal(Component component,
            ComponentMutationsCollector collector) =>
            CollectMutations((TComponent)component, collector);

        internal override void ApplySpecialMappingInternal(Component component, MappingSource collector) =>
            ApplySpecialMapping((TComponent)component, collector);

        /// <summary>
        /// Collects runtime mutations by <see cref="component"/>.
        /// You have to call <see cref="collector"/>.<see cref="ComponentMutationsCollector.ModifyProperties"/>
        /// for all property mutations by the component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="collector">The callback.</param>
        [PublicAPI]
        protected abstract void CollectDependency(TComponent component, ComponentDependencyCollector collector);

        /// <summary>
        /// Collects runtime mutations by <see cref="component"/>.
        /// You have to call <see cref="collector"/>.<see cref="ComponentMutationsCollector.ModifyProperties"/>
        /// for all property mutations by the component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="collector">The callback.</param>
        [PublicAPI]
        protected virtual void CollectMutations(TComponent component, ComponentMutationsCollector collector)
        {
        }

        /// <summary>
        /// Applies some property mapping to your component.
        /// For object replacements, AAO processes automatically so you don't have to implement that in this method.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="mappingSource">The mapping source</param>
        [PublicAPI]
        protected virtual void ApplySpecialMapping(TComponent component, MappingSource mappingSource)
        {
        }

        // Important note for future AAO developer
        // 1. You MUST NOT add abstract method onto this class
        // 2. When you added some virtual methods onto this class, implementer MAY NOT implement that method
        //    so we have to add some way to detect if implemented AND must fallback to default implementation.
        //    One way I can implement is add some internal method to the collector, call it in default implementation,
        //    and do fallback process in that method.
    }

    public abstract class ComponentDependencyCollector
    {
        internal ComponentDependencyCollector()
        {
        }

        /// <summary>
        /// Marks this component as EntryPoint component, which means has some effects to non-avatar components.
        /// For example, Renderer components have side-effects because it renders something.
        /// VRC Contacts have some effects to other avatars in the instance so they have side-effects.
        /// 
        /// If your component has some effects only for some specific component(s), you should not mark your component
        /// as EntryPoint. You should make dependency relationship from affecting component to your component instead.
        /// 
        /// One of such components is VRCPhysBone without animating parameters.
        /// VRCPhysBone has effects if and only if some other EntryPoint components are attached to PB-affected
        /// transforms or their children, or PB-affected transforms are dependencies of some EntryPoint components.
        /// So, for VRCPhysBone, There are bidirectional dependency relationship between PB and PB-affected transforms
        /// and VRCPhysBone is not a EntryPoint component.
        ///
        /// With same reasons, Constraints are not treated as EntryPoint, they have bidirectional
        /// dependency relationship between Constraintas and transform instead.
        /// </summary>
        [PublicAPI]
        public abstract void MarkEntrypoint();

        /// <summary>
        /// Marks this component as HeavyBehaviour component, which means the component uses some resources while
        /// enabled but doesn't eat if not enabled and AAO can (almost) freely change enablement of the component.
        /// When you mark some components Behaviour component, Avatar Optimizer will generate animation that disables
        /// the component when entrypoint is not active / enabled.
        ///
        /// If your component have some runtime load and can be skipped if your component is not needed by all
        /// enabled EntryPoint components, you should mark your component as Behaviour for runtime-load optimization.
        ///
        /// For example, VRCPhysBone and Constraints are marked as HeavyBehaviour.
        /// </summary>
        [PublicAPI]
        public abstract void MarkHeavyBehaviour();

        /// <summary>
        /// Marks this component as Behaviour component, which means the activeness of the component has meaning.
        /// When you mark your some components Behaviour component, Avatar Optimizer will never change activeness of
        /// the component.
        ///
        /// If your component is not a Behaviour component, AAO may change enablement of the component.
        ///
        /// NOTE: In AAO 1.6.0, AAO will not change enablement of non-Behaviour components but in the feature releases,
        /// AAO may change enablement of non-Behaviour components to change enablement of HeavyBehaviour components
        /// effectively.
        /// </summary>
        [PublicAPI]
        public abstract void MarkBehaviour();

        /// <summary>
        /// Adds <see cref="dependency"/> as dependencies of <see cref="dependant"/>.
        /// The dependency will be assumed as the dependant will have dependency if dependant is enabled and
        /// even if dependency is disabled. You can change the settings by <see cref="ComponentDependencyInfo"/>.
        ///
        /// WARNING: the <see cref="ComponentDependencyInfo"/> instance can be shared between multiple AddDependency
        /// invocation so you MUST NOT call methods on IComponentDependencyInfo after calling AddDependency other time.
        /// </summary>
        /// <param name="dependant">The dependant</param>
        /// <param name="dependency">The dependency</param>
        /// <returns>The object to configure the dependency</returns>
        [PublicAPI]
        public abstract ComponentDependencyInfo AddDependency(Component dependant, Component dependency);

        /// <summary>
        /// Adds <see cref="dependency"/> as dependencies of current component.
        /// Same as calling <see cref="AddDependency(UnityEngine.Component,UnityEngine.Component)"/>
        /// with current component but this might be optimized more.
        /// </summary>
        /// <seealso cref="AddDependency(UnityEngine.Component,UnityEngine.Component)"/>
        /// <param name="dependency">The dependency</param>
        /// <returns>The object to configure the dependency</returns>
        [PublicAPI]
        public abstract ComponentDependencyInfo AddDependency(Component dependency);
    }

    public abstract class ComponentDependencyInfo
    {
        internal ComponentDependencyInfo()
        {
        }

        /// <summary>
        /// Indicates the dependency is required even if dependant component is disabled.
        /// </summary>
        /// <returns>this object for method chain</returns>
        [PublicAPI]
        public abstract ComponentDependencyInfo EvenIfDependantDisabled();

        /// <summary>
        /// Indicates the dependency is not required if dependency component is disabled.
        /// </summary>
        /// <returns>this object for method chain</returns>
        [PublicAPI]
        public abstract ComponentDependencyInfo OnlyIfTargetCanBeEnable();
    }

    /// <example>
    /// <code>
    /// protected override void CollectMutations(AimConstraint component, IComponentMutationsCollector collector)
    /// {
    ///     collector.ModifyProperties(component.transform, new [] { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" });
    /// }
    /// </code>
    /// </example>
    public abstract class ComponentMutationsCollector
    {
        internal ComponentMutationsCollector()
        {
        }

        /// <summary>
        /// Registers <paramref name="properties"/> of the <paramref name="component"/> will be changed by current component.
        /// </summary>
        /// <param name="component">The component current component will modifies</param>
        /// <param name="properties">The list of properties current component will modifies</param>
        [PublicAPI]
        public abstract void ModifyProperties([NotNull] Component component,
            [NotNull] [ItemNotNull] IEnumerable<string> properties);

        /// <inheritdoc cref="ModifyProperties(UnityEngine.Component,System.Collections.Generic.IEnumerable{string})"/>
        [PublicAPI]
        public void ModifyProperties([NotNull] Component component,
            [NotNull] [ItemNotNull] params string[] properties) =>
            ModifyProperties(component, (IEnumerable<string>)properties);
    }

    /// <summary>
    /// The class provides object and property replaced by Avatar Optimizer.
    ///
    /// Avatar Optimizer may replaces or merges component to another component.
    /// This class provide the information about the replacement.
    /// In addition, Avatar Optimizer may replace or merges some properties of the component.
    /// This class also provide the information about the property replacement.
    /// </summary>
    public abstract class MappingSource
    {
        internal MappingSource()
        {
        }

        /// <summary>
        /// Returns <see cref="MappedComponentInfo{T}"/> about the component instance.
        /// The instance can be a missing component.
        /// </summary>
        /// <param name="component">The component to get information about</param>
        /// <typeparam name="T">The type of component</typeparam>
        [PublicAPI]
        public abstract MappedComponentInfo<T> GetMappedComponent<T>(T component) where T : Component;

        /// <summary>
        /// Returns <see cref="MappedComponentInfo{T}"/> about the GameObject instance.
        /// The instance can be a missing component.
        /// </summary>
        /// <param name="component">The component to get information about</param>
        /// <typeparam name="T">The type of component</typeparam>
        [PublicAPI]
        public abstract MappedComponentInfo<GameObject> GetMappedGameObject(GameObject component);
    }

    public abstract class MappedComponentInfo<T> where T : Object
    {
        internal MappedComponentInfo()
        {
        }

        /// <summary>
        /// The mapped component (or GameObject).
        /// The component may be removed without mapped component.
        /// If there are no mapped component, this will be null.
        ///
        /// Even if the component is removed without mapped component, some animation property can be mapped
        /// to a property on another component so you should use <see cref="TryMapProperty"/> if the component is highly related
        /// to animation property, for example, blendShape related SkinnedMeshRenderer.
        /// </summary>
        [PublicAPI]
        public abstract T MappedComponent { get; }

        /// <summary>
        /// Maps animation property name to component and MappedPropertyInfo.
        /// If the property is not removed, returns true and <paramref name="found"/> is set.
        /// If the property is removed, returns false and <paramref name="found"/> will be default.
        ///
        /// To get mapped property probably, you must register the property as modified property by
        /// <see cref="ComponentMutationsCollector.ModifyProperties(UnityEngine.Component,System.Collections.Generic.IEnumerable{string})"/>.
        /// Unless that, renaming or moving the property may not be tracked by Avatar Optimizer.
        /// </summary>
        /// <param name="property">The name of property will be mapped</param>
        /// <param name="found">The result parameter</param>
        /// <returns>Whether if the property is successfully mapped or removed</returns>
        [PublicAPI]
        public abstract bool TryMapProperty(string property, out MappedPropertyInfo found);
    }

    public readonly struct MappedPropertyInfo
    {
        /// <summary>
        /// The Component or GameObject the property is on.
        /// </summary>
        [PublicAPI]
        public Object Component { get; }

        /// <summary>
        /// The name of the mapped property.
        /// </summary>
        [PublicAPI]
        public string Property { get; }

        internal MappedPropertyInfo(Object component, string property)
        {
            Component = component;
            Property = property;
        }
    }
}
