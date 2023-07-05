using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    // https://github.com/bdunderscore/modular-avatar/blob/db49e2e210bc070671af963ff89df853ae4514a5/Packages/nadena.dev.modular-avatar/Runtime/RuntimeUtil.cs
    // Originally under MIT License
    // Copyright (c) 2022 bd_
    internal static class RuntimeUtil
    {
        [CanBeNull]
        public static string RelativePath(GameObject root, GameObject child)
        {
            if (root == child) return "";

            List<string> pathSegments = new List<string>();
            while (child != root && child != null)
            {
                pathSegments.Add(child.name);
                child = child.transform.parent != null ? child.transform.parent.gameObject : null;
            }

            if (child == null) return null;

            pathSegments.Reverse();
            return String.Join("/", pathSegments);
        }

#if UNITY_EDITOR
        public static bool isPlaying => UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#else
        public static bool isPlaying => true;
#endif

        public static Action<EditSkinnedMeshComponent> OnAwakeEditSkinnedMesh;
        public static Action<EditSkinnedMeshComponent> OnDestroyEditSkinnedMesh;
    }
}
