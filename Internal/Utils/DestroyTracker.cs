using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// This class allows users to track destroyed objects
    /// </summary>
    public class DestroyTracker
    {
        private static DestroyTracker? _tracker;

        private readonly Dictionary<int, Action<int>?> _handlers = new ();

        public static void Track(Object obj, Action<int> handler)
        {
            var tracker = _tracker;
            if (tracker == null) return;

            var instanceId = obj.GetInstanceID();
            if (tracker._handlers.TryGetValue(instanceId, out var oldHandler))
                tracker._handlers[instanceId] = oldHandler + handler;
            else
                tracker._handlers[instanceId] = handler;
        }

        public static void Untrack(Object obj, Action<int> handler)
        {
            var tracker = _tracker;
            if (tracker == null) return;

            var instanceId = obj.GetInstanceID();
            if (tracker._handlers.TryGetValue(instanceId, out var oldHandler))
                tracker._handlers[instanceId] = oldHandler - handler;
        }

        public static void DestroyImmediate(Object obj)
        {
            if (obj == null) return;
            var instanceId = obj.GetInstanceID();
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
