using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    internal static class Utils
    {
        [CanBeNull]
        public static VRCAvatarDescriptor FindAvatarInParents([CanBeNull] Transform transform)
        {
            while (transform != null)
            {
                if (transform.GetComponent<VRCAvatarDescriptor>() is VRCAvatarDescriptor descriptor)
                    return descriptor;
                transform = transform.parent;
            }

            return null;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        /// <returns>relative path to child. null if parent is not parent of child</returns>
        /// <exception cref="ArgumentNullException"></exception>
        [CanBeNull]
        [ContractAnnotation("parent:null, child:notnull => notnull")]
        public static string RelativePath([CanBeNull] GameObject parent, [NotNull] GameObject child)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));

            // fast path: parent is child
            if (parent == child)
                return "";

            var components = new List<string>();

            var parentTransform = parent != null ? parent.transform : null;
            var transform = child.transform;
            for (;;)
            {
                if (transform == parentTransform) break;
                if (transform == null) return null;
                components.Add(transform.name);
                transform = transform.parent;
            }

            components.Reverse();

            return string.Join("/", components);
        }

        internal static Func<IEnumerable<ObjectRef>> GetCurrentReportActiveReferences = () => Array.Empty<ObjectRef>();

        internal static IEnumerable<T> OnEach<T>(this IEnumerable<T> self, Action<T> action)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            foreach (var value in self)
            {
                action(value);
                yield return value;
            }
        }
    }
}
