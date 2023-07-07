using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace Anatawa12.ApplyOnPlay
{
    internal static class ManualBake
    {
        private const string ManualBakeMenuName = "Tools/Apply on Play/Manual Bake Avatar";
        private const string ManualBakeContextMenuName = "GameObject/[Apply on Play] Manual Bake Avatar";
        private const int ManualBakeContextMenuPriority = 49;

        [MenuItem(ManualBakeMenuName, true)]
        [MenuItem(ManualBakeContextMenuName, true, ManualBakeContextMenuPriority)]
        private static bool CheckManualBake()
        {
            var avatar = Selection.activeGameObject;
            return avatar && avatar.GetComponent<VRCAvatarDescriptor>() != null;
        }

        [MenuItem(ManualBakeMenuName, false)]
        [MenuItem(ManualBakeContextMenuName, false, ManualBakeContextMenuPriority)]
        private static void ExecuteManualBake()
        {
            var avatar = Selection.activeGameObject;
            var avatarDescriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            if (avatar == null || avatarDescriptor == null) return;

            var original = avatar;
            avatar = Object.Instantiate(avatar);
            avatar.transform.position += Vector3.forward * 2;
            
            var callbacks = ApplyOnPlayCallbackRegistry.GetCallbacks();

            try
            {
                ApplyOnPlayCaller.ProcessAvatar(avatar, "Manual Bake", callbacks);
            }
            finally
            {
                ApplyOnPlayCaller.CallManualBakeFinalizer(ApplyOnPlayCallbackRegistry.GetFinalizers(), original, avatar);
                Selection.objects = new Object[] { avatar };
            }
        }
    }
}
