using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// This class allows users to track destroyed objects
    /// </summary>
    public class DestroyTracker
    {
        private static DestroyTracker? _tracker;

        private readonly Dictionary<EntityId, Action<EntityId>?> _handlers = new ();

        public static void Track(Object obj, Action<EntityId> handler)
        {
            var tracker = _tracker;
            if (tracker == null) return;

            var instanceId = obj.GetEntityIDCompatible();
            if (tracker._handlers.TryGetValue(instanceId, out var oldHandler))
                tracker._handlers[instanceId] = oldHandler + handler;
            else
                tracker._handlers[instanceId] = handler;
        }

        public static void Untrack(Object obj, Action<EntityId> handler)
        {
            var tracker = _tracker;
            if (tracker == null) return;

            var instanceId = obj.GetEntityIDCompatible();
            if (tracker._handlers.TryGetValue(instanceId, out var oldHandler))
                tracker._handlers[instanceId] = oldHandler - handler;
        }

        public static void DestroyImmediate(Object? obj)
        {
            // ここで null 調べてるし ... これは null 許容であると考えていいよね ... ? by Reina_Sakiria
            if (obj == null) return;
            var instanceId = obj.GetEntityIDCompatible();
            Object.DestroyImmediate(obj);
            var tracker = _tracker;
            if (tracker != null)
            {
                if (tracker._handlers.TryGetValue(instanceId, out var handler))
                {
                    handler?.Invoke(instanceId);
                    tracker._handlers.Remove(instanceId);
                }
            }
        }

        public class ExtensionContext : IExtensionContext
        {
            public void OnActivate(BuildContext context) => _tracker = new DestroyTracker();

            public void OnDeactivate(BuildContext context) => _tracker = null;
        }
    }
}
