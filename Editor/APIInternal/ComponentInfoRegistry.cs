using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.APIInternal.Externals;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIInternal
{
    internal static class ComponentInfoRegistry
    {
        private static readonly Dictionary<Type, ComponentInformation> InformationByType = new();

        [InitializeOnLoadMethod]
        static void FindAllInfoImplements()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var type in assembly.GetTypes())
            foreach (ComponentInformationAttributeBase attribute in type.GetCustomAttributes(
                         typeof(ComponentInformationAttributeBase), false))
            {
                try
                {
                    LoadType(type, attribute);
                }
                catch (Exception e)
                {
                    try
                    {
                        Debug.LogError($"Processing type {type}");
                        Debug.LogException(e);
                    }
                    catch (Exception e1)
                    {
                        Debug.LogException(new AggregateException(e, e1));
                    }
                }
            }
        }

        [InitializeOnLoadMethod]
        static void NDMFComponents()
        {
            var contextHolder = Type.GetType("nadena.dev.ndmf.VRChat.ContextHolder, nadena.dev.ndmf", false);
            // since NDMF 1.4.0, ContextHolder is moved to nadena.dev.ndmf.vrchat assembly
            if (contextHolder == null)
                contextHolder = Type.GetType("nadena.dev.ndmf.VRChat.ContextHolder, nadena.dev.ndmf.vrchat", false);

            // nadena.dev.ndmf.VRChat.ContextHolder is internal so I use reflection
            if (contextHolder != null)
            {
                InformationByType.Add(contextHolder, new EntrypointComponentInformation());
            }
        }

        private static void LoadType(Type type, ComponentInformationAttributeBase attribute)
        {
            var targetType = attribute.GetTargetType();
            if (targetType == null) return;
            var informationType = typeof(IComponentInformation<>).MakeGenericType(targetType);
            if (type.ContainsGenericParameters)
            {
                if (type.GetGenericArguments().Length != 1)
                    throw new Exception("Unsupported type : More than 1 Generic Parameters");

                type = type.MakeGenericType(targetType);
            }

            if (!informationType.IsAssignableFrom(type))
                throw new Exception("Unsupported type : Not Extends from ComponentInformation<Target>");
            if (InformationByType.TryGetValue(targetType, out var existing))
            {
                if (existing is IExternalMarker)
                {
                    // if existing is fallback, use new one
                    var instance = (ComponentInformation)System.Activator.CreateInstance(type);
                    InformationByType[targetType] = instance;
                }
                else if (typeof(IExternalMarker).IsAssignableFrom(targetType))
                {
                    // if adding is fallback, use existing one
                }
                else
                {
                    // otherwise, in other words, If both are not fallback, throw exception
                    throw new Exception($"Target Type duplicated: {targetType}");
                }
            }
            else
            {
                var instance = (ComponentInformation)System.Activator.CreateInstance(type);
                InformationByType.Add(targetType, instance);
            }
        }

        internal static bool TryGetInformation(Type type, out ComponentInformation information)
        {
            if (InformationByType.TryGetValue(type, out information)) return true;

            // we could not find registered type so check if the type is meaningless
            if (IsMeaninglessType(type))
            {
                information = MeaninglessComponentInformation.Instance;
                return true;
            }

            return false;
        }

        private static bool IsMeaninglessType(Type type)
        {
            var meaninglessTypes = AssetDescription.GetMeaninglessComponents();

            // fast path: simple check
            if (meaninglessTypes.Contains(type)) return true;
            // slow path: check for parent class
            for (var current = type.BaseType; current != null; current = current.BaseType)
                if (meaninglessTypes.Contains(current))
                    return true;

            return false;
        }

        class MeaninglessComponentInformation : ComponentInformation
        {
            public static MeaninglessComponentInformation Instance { get; } = new();

            private MeaninglessComponentInformation()
            {
            }

            internal override void CollectDependencyInternal(Component component,
                API.ComponentDependencyCollector collector)
            {
            }

            internal override void CollectMutationsInternal(Component component,
                API.ComponentMutationsCollector collector)
            {
            }

            internal override void ApplySpecialMappingInternal(Component component, API.MappingSource mappingSource)
            {
            }
        }
    }
}

