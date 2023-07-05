using Anatawa12.ApplyOnPlay;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    /// <summary>
    /// Initializes Error Reporting System
    /// </summary>
    internal class ErrorReportingInitializerProcessor : IVRCSDKPreprocessAvatarCallback, IApplyOnPlayCallback
    {
        public int callbackOrder => int.MinValue;

        public string CallbackName => "Error Reporting Initialization";
        public string CallbackId => "com.anatawa12.error-reporting";

        public bool ApplyOnPlay(GameObject avatarGameObject) => OnPreprocessAvatar(avatarGameObject);

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            BuildReport.Clear();
            BuildReport.CurrentReport.Initialize(avatarGameObject.GetComponent<VRCAvatarDescriptor>());
            return true;
        }
    }
}
