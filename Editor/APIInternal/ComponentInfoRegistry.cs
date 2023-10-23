using System;
using System.Collections.Generic;
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
            var contextHolder = typeof(nadena.dev.ndmf.BuildContext).Assembly
                .GetType("nadena.dev.ndmf.VRChat.ContextHolder");
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
            if (InformationByType.ContainsKey(targetType))
                throw new Exception($"Target Type duplicated: {targetType}");

            var instance = (ComponentInformation)System.Activator.CreateInstance(type);
            InformationByType.Add(targetType, instance);
        }

        internal static bool TryGetInformation(Type type, out ComponentInformation information) =>
            InformationByType.TryGetValue(type, out information);
    }
}

