#nullable enable

using System;
using Anatawa12.AvatarOptimizer.API;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIInternal
{
    public abstract class ComponentInformationAttributeBase : Attribute
    {
        internal ComponentInformationAttributeBase() { }
        internal abstract Type? GetTargetType();
    }

    /// <summary>
    /// Marker Interface for type check
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IComponentInformation<in T>
    {
    }
    
    public abstract class ComponentInformation
    {
        internal ComponentInformation()
        {
        }
        
        internal abstract void CollectDependencyInternal(Component component, ComponentDependencyCollector collector);
        internal abstract void CollectMutationsInternal(Component component, ComponentMutationsCollector collector);
        internal abstract void ApplySpecialMappingInternal(Component component, MappingSource mappingSource);
    }

    internal class AllowInheritAttribute : Attribute
    {
    }
}
