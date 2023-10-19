using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

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

        [PublicAPI]
        public abstract void ModifyProperties([NotNull] Component component, [NotNull] IEnumerable<string> properties);
    }
}
