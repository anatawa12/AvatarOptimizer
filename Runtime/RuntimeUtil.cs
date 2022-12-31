using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Anatawa12.Merger
{
    // https://github.com/bdunderscore/modular-avatar/blob/db49e2e210bc070671af963ff89df853ae4514a5/Packages/nadena.dev.modular-avatar/Runtime/RuntimeUtil.cs
    // Originally under MIT License
    // Copyright (c) 2022 bd_
    internal static class RuntimeUtil
    {
        // Initialized in Util
        public static Action<Action> delayCall = (_) => { };
        public static event Action OnHierarchyChanged;

        public enum OnDemandSource
        {
            Awake,
            Start
        }

        public delegate void OnDemandProcessAvatarDelegate(OnDemandSource source, MonoBehaviour component);

        public static OnDemandProcessAvatarDelegate OnDemandProcessAvatar = (_0, _1) => { };

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

        [CanBeNull]
        public static string AvatarRootPath(GameObject child)
        {
            var avatar = FindAvatarInParents(child.transform);
            if (avatar == null) return null;
            return RelativePath(avatar.gameObject, child);
        }

        public static VRCAvatarDescriptor FindAvatarInParents(Transform target)
        {
            while (target != null)
            {
                var av = target.GetComponent<VRCAvatarDescriptor>();
                if (av != null) return av;
                target = target.parent;
            }

            return null;
        }

        public static void MarkDirty(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(obj))
            {
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
            }

            UnityEditor.EditorUtility.SetDirty(obj);
#endif
        }

#if UNITY_EDITOR
        private static UnityEngine.Object cachedAnimationWindowState;

        private static readonly Type animationWindowStateType
            = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditorInternal.AnimationWindowState");

        private static readonly PropertyInfo recordingProp = animationWindowStateType.GetProperty(
            "recording",
            BindingFlags.Instance | BindingFlags.Public
        );

        private static readonly PropertyInfo previewingProp = animationWindowStateType.GetProperty(
            "previewing",
            BindingFlags.Instance | BindingFlags.Public
        );

        private static readonly PropertyInfo playingProp = animationWindowStateType.GetProperty(
            "playing",
            BindingFlags.Instance | BindingFlags.Public
        );
#endif

        public static bool IsAnimationEditMode()
        {
#if !UNITY_EDITOR
            return false;
#else
            if (cachedAnimationWindowState == null)
            {
                foreach (var obj in Resources.FindObjectsOfTypeAll(animationWindowStateType))
                {
                    cachedAnimationWindowState = obj;
                }
            }

            if (cachedAnimationWindowState == null) return false;

            return (bool) recordingProp.GetValue(cachedAnimationWindowState, null)
                   || (bool) previewingProp.GetValue(cachedAnimationWindowState, null)
                   || (bool) playingProp.GetValue(cachedAnimationWindowState, null);
#endif
        }

#if UNITY_EDITOR
        public static bool isPlaying => UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#else
        public static bool isPlaying => true;
#endif
        public static void InvokeHierarchyChanged()
        {
            OnHierarchyChanged?.Invoke();
        }
    }
}
