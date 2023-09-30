using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.API;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIBackend
{
    public static class ComponentInfoRegistry
    {
        private static readonly Dictionary<Type, IComponentInformation> InformationByType =
            new Dictionary<Type, IComponentInformation>();

        [InitializeOnLoadMethod]
        static void FindAllInfoImplements()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var type in assembly.GetTypes())
            foreach (ComponentInformationAttribute attribute in type.GetCustomAttributes(
                         typeof(ComponentInformationAttribute), false))
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

        private static void LoadType(Type type, ComponentInformationAttribute attribute)
        {
            var targetType = attribute.TargetType;
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

            var instance = (IComponentInformation)System.Activator.CreateInstance(type);
            InformationByType.Add(targetType, instance);
        }
    }
}

