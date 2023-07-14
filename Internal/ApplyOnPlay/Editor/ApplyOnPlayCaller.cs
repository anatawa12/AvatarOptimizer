using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;

namespace Anatawa12.ApplyOnPlay
{
    internal class ApplyOnPlayCaller : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var components = scene.GetRootGameObjects()
                .SelectMany(x => x.GetComponentsInChildren<VRCAvatarDescriptor>(true)).ToArray();

            var callbacks = ApplyOnPlayCallbackRegistry.GetCallbacks();

            foreach (var vrcAvatarDescriptor in components)
            {
                ProcessAvatar(vrcAvatarDescriptor.gameObject, ApplyReason.EnteringPlayMode, callbacks);
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