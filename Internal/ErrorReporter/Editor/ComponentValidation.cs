using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    internal static class ComponentValidation
    {
        internal static List<ErrorLog> ValidateAll(GameObject root)
        {
            List<ErrorLog> logs = new List<ErrorLog>();
            // TODO: search component & check component for each component
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                //var componentLogs = component.CheckComponent();
                //if (componentLogs != null)
                //{
                //    logs.AddRange(componentLogs);
                //}
            }

            return logs;
        }
    }
}
