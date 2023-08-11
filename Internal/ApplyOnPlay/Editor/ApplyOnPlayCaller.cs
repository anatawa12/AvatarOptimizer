using System;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Anatawa12.ApplyOnPlay
{
    [InitializeOnLoad]
    internal static class ApplyOnPlayCaller
    {
        public const string SkipApplyingIfInactiveName = "com.anatawa12.apply-on-play.skip-if-inactive";

        static ApplyOnPlayCaller()
        {
            GlobalActivator.activate = activator => OnProcessScene(activator.gameObject.scene);
            ApplyOnPlayActivator.processAvatar = activator =>
            {
                var component = activator.gameObject.GetComponent<VRCAvatarDescriptor>();
                Object.DestroyImmediate(activator);
                if (component)
                    ProcessAvatar(component.gameObject, ApplyReason.EnteringPlayMode,
                        ApplyOnPlayCallbackRegistry.GetCallbacks());
            };

            EditorSceneManager.sceneOpened += (scene, _) => GlobalActivator.CreateIfNotPresent(scene);
            EditorApplication.delayCall += () =>
            {
                foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount)
                             .Select(SceneManager.GetSceneAt))
                    GlobalActivator.CreateIfNotPresent(scene);
            };
        }

        public static bool SkipApplyingIfInactive
        {
            get => EditorPrefs.GetBool(SkipApplyingIfInactiveName, true);
            set => EditorPrefs.SetBool(SkipApplyingIfInactiveName, value);
        }

        public static void OnProcessScene(Scene scene)
        {
            var components = scene.GetRootGameObjects()
                .SelectMany(x => x.GetComponentsInChildren<VRCAvatarDescriptor>(true)).ToArray();

            var callbacks = ApplyOnPlayCallbackRegistry.GetCallbacks();

            var skipIfInactive = SkipApplyingIfInactive;

            var stopwatch = Stopwatch.StartNew();
            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var vrcAvatarDescriptor in components)
                {
                    if (skipIfInactive && !vrcAvatarDescriptor.gameObject.activeInHierarchy)
                    {
                        var activator = vrcAvatarDescriptor.gameObject.AddComponent<ApplyOnPlayActivator>();
                        activator.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    }
                    else
                    {
                        ProcessAvatar(vrcAvatarDescriptor.gameObject, ApplyReason.EnteringPlayMode, callbacks);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                Debug.Log($"time to process apply on play: {stopwatch.Elapsed}");
            }
        }

        internal static bool ProcessAvatar(GameObject avatarGameObject, ApplyReason reason, IApplyOnPlayCallback[] callbacks)
        {
            string action;
            switch (reason)
            {
                case ApplyReason.EnteringPlayMode:
                    action = "Apply on Play";
                    break;
                case ApplyReason.ManualBake:
                    action = "Manual Bake";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
            }

            foreach (var applyOnPlayCallback in callbacks)
            {
                try
                {
                    if (!applyOnPlayCallback.ApplyOnPlay(avatarGameObject, reason))
                    {
                        var message = $"The {action} for {avatarGameObject} was aborted because " +
                                      $"'{applyOnPlayCallback.CallbackName}' reported a failure.";
                        Debug.LogError(message);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    var message = $"The {action} for {avatarGameObject} was aborted because " +
                                  $"'{applyOnPlayCallback.CallbackName}' threw an exception.";
                    Debug.LogError(message);
                    Debug.LogException(ex);
                    return false;
                }
            }

            return true;
        }

        public static void CallManualBakeFinalizer(IManualBakeFinalizer[] finalizers, GameObject original, GameObject avatar)
        {
            foreach (var manualBakeFinalizer in finalizers)
            {
                try
                {
                    manualBakeFinalizer.FinalizeManualBake(original, avatar);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}