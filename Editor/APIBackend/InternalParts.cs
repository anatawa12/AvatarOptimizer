using System;
using JetBrains.Annotations;

namespace Anatawa12.AvatarOptimizer.APIBackend
{
    public abstract class ComponentInformationAttributeBase : Attribute
    {
        internal ComponentInformationAttributeBase() { }
        [CanBeNull] internal abstract Type GetTargetType();
    }

    /// <summary>
    /// Marker Interface for type check
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IComponentInformation<in T>
    {
    }
}