using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.ApplyOnPlay
{
    [InitializeOnLoad]
    public static class ApplyOnPlayCallbackRegistry
    {
        private static readonly List<IApplyOnPlayCallback> _callbacks = new List<IApplyOnPlayCallback>();

        static ApplyOnPlayCallbackRegistry()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (!typeof(IApplyOnPlayCallback).IsAssignableFrom(type)) continue;
                        var constructor = type.GetConstructor(Type.EmptyTypes);
                        if (constructor == null) continue;
                        try
                        {
                            RegisterCallback((IApplyOnPlayCallback)constructor.Invoke(Array.Empty<object>()));
                        }
                        catch (Exception e)
                        {
                            LogException($"Instantiating {type.Name}", e);
                        }
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        LogException($"Discovering types in {assembly.GetName().Name}", e);
                    }
                    catch (Exception e2)
                    {
                        LogException("Discovering types in some assembly", e, e2);
                    }
                }
            }
        }

        [PublicAPI]
        public static void RegisterCallback(IApplyOnPlayCallback callback)
        {
            _callbacks.Add(callback);
        }

        internal static IApplyOnPlayCallback[] GetCallbacks()
        {
            var copied = _callbacks.ToArray();
            Array.Sort(copied, (a, b) => a.callbackOrder.CompareTo(b.callbackOrder));
            return copied;
        }

        private static void LogException(string message, params Exception[] exceptions)
        {
            message = $"[ApplyOnPlay] {message}";

            if (exceptions.Length == 1)
            {
                Debug.LogException(new Exception(message, exceptions[0]));
            }
            else
            {
                Debug.LogException(new AggregateException(message, exceptions));
            }
        }
    }
}