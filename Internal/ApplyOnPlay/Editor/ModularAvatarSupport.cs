#if MODULAR_AVATAR
using System.Reflection;
using nadena.dev.modular_avatar.core.editor;
using UnityEditor;
using UnityEngine;

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
            return _processor.OnPreprocessAvatar(avatarGameObject);
        }
    }
}
#endif
