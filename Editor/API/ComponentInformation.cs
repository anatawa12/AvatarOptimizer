using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.API
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    [MeansImplicitUse]
    [BaseTypeRequired(typeof(ComponentInformation<>))]
    public sealed class ComponentInformationAttribute : APIInternal.ComponentInformationAttributeBase
    {
        public Type TargetType { get; }

        public ComponentInformationAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        internal override Type GetTargetType() => TargetType;
    }

    public abstract class ComponentInformation<TComponent> :
        APIInternal.ComponentInformation,
        APIInternal.IComponentInformation<TComponent>
        where TComponent : Component
    {
        internal sealed override void CollectDependencyInternal(Component component,
            IComponentDependencyCollector collector) =>
            CollectDependency((TComponent)component, collector);

        internal sealed override void CollectMutationsInternal(Component component,
            IComponentMutationsCollector collector) =>
            CollectMutations((TComponent)component, collector);

        protected abstract void CollectDependency(TComponent component, IComponentDependencyCollector collector);

        protected virtual void CollectMutations(TComponent component, IComponentMutationsCollector collector)
        {
        }
    }

    public interface IComponentDependencyCollector
    {
        void MarkEntrypoint();

        IComponentDependencyInfo AddDependency(Component dependant, Component dependency);
        IComponentDependencyInfo AddDependency(Component dependency);
    }

    /// <summary>
    /// This interface will never be stable for implement. This interface is stable for calling methods.
    /// </summary>
    public interface IComponentDependencyInfo
    {
        IComponentDependencyInfo EvenIfDependantDisabled();
        IComponentDependencyInfo OnlyIfTargetCanBeEnable();
    }

    public interface IComponentMutationsCollector
    {
        void ModifyProperties([NotNull] Component component, [NotNull] IEnumerable<string> properties);
    }
}
