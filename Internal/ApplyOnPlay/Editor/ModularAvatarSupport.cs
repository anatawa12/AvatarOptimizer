#if MODULAR_AVATAR
using System;
using System.IO;
using System.Reflection;
using nadena.dev.modular_avatar.core.editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.ApplyOnPlay
{
    [InitializeOnLoad]
    internal class ModularAvatarSupport : IApplyOnPlayCallback
    {
        static ModularAvatarSupport()
        {
            // if apply on play framework integration is embed to modular avatar itself, do not use this support module 
            if (typeof(AvatarProcessor).GetField("ApplyOnPlaySupported", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) != null)
                return;

            ApplyOnPlayCallbackRegistry.RegisterCallback(new ModularAvatarSupport(new AvatarProcessor()));
        }

        private readonly AvatarProcessor _processor;

        public ModularAvatarSupport(AvatarProcessor processor)
        {
            _processor = processor;
        }

        [MenuItem("Tools/Modular Avatar/Apply on Play config has been moved", false, 1000 - 1)]
        private static void ToggleApplyOnPlay() => ApplyOnPlayConfiguration.OpenWindow();

        public int callbackOrder => _processor.callbackOrder;

        public string CallbackName => "Modular Avatar";
        public string CallbackId => "com.anatawa12.apply-on-play.modular-avatar";

        public bool ApplyOnPlay(GameObject avatarGameObject, ApplyReason reason)
        {
            try
            {
                return _processor.OnPreprocessAvatar(avatarGameObject);
            }
            finally
            {
                if (reason == ApplyReason.ManualBake)
                {
                    MoveModularAvatarAssets(avatarGameObject);
                }
            }
        }

        private static void MoveModularAvatarAssets(GameObject avatarGameObject)
        {
            try
            {
                var maGeneratedPath = "Assets/999_Modular_Avatar_Generated";
                var outputPath = $"Assets/ModularAvatarOutput";
                if (!Directory.Exists(maGeneratedPath)) return;

                var avatarName = avatarGameObject.name;
                if (avatarName.EndsWith("(Clone)", StringComparison.Ordinal))
                    avatarName = avatarName.Substring(0, avatarName.Length - "(Clone)".Length);

                avatarName = string.Join("_", avatarName.Split(Path.GetInvalidFileNameChars()));

                var basePath = $"Assets/ModularAvatarOutput/{avatarName}";
                var savePath = basePath;

                var extension = 0;

                while (File.Exists(savePath) || Directory.Exists(savePath))
                {
                    savePath = basePath + " " + (++extension);
                }

                Directory.CreateDirectory(outputPath);
                Directory.Move(maGeneratedPath, savePath);
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
#endif
