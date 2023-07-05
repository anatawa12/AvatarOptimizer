#if MODULAR_AVATAR
using System.Reflection;
using nadena.dev.modular_avatar.core.editor;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.ApplyOnPlay
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

        public int callbackOrder => _processor.callbackOrder;

        public bool ApplyOnPlay(GameObject avatarGameObject)
        {
            return _processor.OnPreprocessAvatar(avatarGameObject);
        }
    }
}
#endif
