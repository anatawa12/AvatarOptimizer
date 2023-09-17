#if !NADEMOFU
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.ApplyOnPlay
{
    [InitializeOnLoad]
    public static class ApplyOnPlayCallbackRegistry
    {
        internal static readonly List<IApplyOnPlayCallback> Callbacks = new List<IApplyOnPlayCallback>();
        internal static readonly List<IManualBakeFinalizer> Finalizers = new List<IManualBakeFinalizer>();
        internal const string ENABLE_EDITOR_PREFS_PREFIX = "com.anatawa12.apply-on-play.enabled.";

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
                        DoCallback<IApplyOnPlayCallback>(type, RegisterCallback);
                        DoCallback<IManualBakeFinalizer>(type, RegisterFinalizer);
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

        private static void DoCallback<T>(Type type, Action<T> add)
        {
            if (!typeof(T).IsAssignableFrom(type)) return;
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor == null) return;
            try
            {
                add((T)constructor.Invoke(Array.Empty<object>()));
            }
            catch (Exception e)
            {
                LogException($"Instantiating {type.Name}", e);
            }
        }

        [PublicAPI]
        public static void RegisterCallback(IApplyOnPlayCallback callback)
        {
            Callbacks.Add(callback);
        }

        [PublicAPI]
        public static void RegisterFinalizer(IManualBakeFinalizer callback)
        {
            Finalizers.Add(callback);
        }

        internal static IApplyOnPlayCallback[] GetCallbacks()
        {
            var copied = Callbacks
                .Where(x => EditorPrefs.GetBool(ENABLE_EDITOR_PREFS_PREFIX + x.CallbackId, true))
                .ToArray();
            Array.Sort(copied, (a, b) => a.callbackOrder.CompareTo(b.callbackOrder));
            return copied;
        }

        internal static IManualBakeFinalizer[] GetFinalizers()
        {
            var copied = Finalizers.ToArray();
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
#endif