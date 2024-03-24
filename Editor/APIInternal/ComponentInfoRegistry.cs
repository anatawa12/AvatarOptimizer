using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.APIInternal.Externals;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIInternal
{
    internal static class ComponentInfoRegistry
    {
        private static readonly Dictionary<Type, ComponentInformation> InformationByType =
            new Dictionary<Type, ComponentInformation>();

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

        internal static bool TryGetInformation(Type type, out ComponentInformation information) =>
            InformationByType.TryGetValue(type, out information);
    }
}

