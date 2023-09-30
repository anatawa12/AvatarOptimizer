using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.API
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    [MeansImplicitUse]
    [BaseTypeRequired(typeof(ComponentInformation<>))]
    public sealed class ComponentInformationAttribute : Attribute
    {
        public Type TargetType { get; }

        public ComponentInformationAttribute(Type targetType)
        {
            TargetType = targetType;
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

        IComponentDependencyInfo AsBone();
    }

    internal interface IComponentInformation
    {
        void CollectDependency(Component component, IComponentDependencyCollector collector);
    }

    internal interface IComponentInformation<in T> : IComponentInformation
    {
    }

    [MeansImplicitUse]
    public abstract class ComponentInformation<T> : IComponentInformation<T>
        where T : Component
    {
        void IComponentInformation.CollectDependency(Component component, IComponentDependencyCollector collector) =>
            CollectDependency((T)component, collector);

        protected abstract void CollectDependency(T component, IComponentDependencyCollector collector);
    }
}
