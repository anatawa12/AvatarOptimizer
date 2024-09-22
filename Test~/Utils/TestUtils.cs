using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Test
{
    public static class TestUtils
    {
        public static GameObject NewAvatar(string name = null)
        {
            var root = new GameObject();
            root.name = name ?? "Test Avatar";
            var animator = root.AddComponent<Animator>();
            animator.avatar = AvatarBuilder.BuildGenericAvatar(root, "");
#if AAO_VRCSDK3_AVATARS
            var descriptor = root.AddComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
#endif
            return root;
        }

        public static void SetFxLayer(GameObject root, RuntimeAnimatorController controller)
        {
#if AAO_VRCSDK3_AVATARS
            var descriptor = root.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            descriptor.customizeAnimationLayers = true;
            descriptor.specialAnimationLayers ??= new VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.CustomAnimLayer[]
            {
                new()
                {
                    type = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.Sitting,
                },
                new()
                {
                    type = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.TPose,
                },
                new()
                {
                    type = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.IKPose,
                },
            };
            descriptor.baseAnimationLayers ??= new VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.CustomAnimLayer[]
            {
                new()
                {
                    type = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.Base,
                },
                new()
                {
                    type = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.Action,
                },
                new()
                {
                    type = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX,
                },
            };
            var index = Array.FindIndex(descriptor.baseAnimationLayers,
                x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX);
            if (index <= 0)
                throw new InvalidOperationException("FX Layer not found");

            descriptor.baseAnimationLayers[index].animatorController = controller;
            descriptor.baseAnimationLayers[index].isDefault = false;
#else
            var animator = root.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
#endif
        }

        public static string GetAssetPath(string testRelativePath)
        {
            var path = AssetDatabase.GUIDToAssetPath("801b64144a3842adb8909fd2d209241a");
            var baseDir = path.Substring(0, path.LastIndexOf('/'));
            return $"{baseDir}/{testRelativePath}";
        }

        public static T GetAssetAt<T>(string testRelativePath) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(GetAssetPath(testRelativePath));
    }
}
