using Anatawa12.AvatarOptimizer.ErrorReporting;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Anatawa12.AvatarOptimizer
{
    internal static class ManualBake
    {
        private const string ManualBakeMenuName = "Tools/Avatar Optimizer/Manual Bake Avatar";
        private const string ManualBakeContextMenuName = "GameObject/[AvatarOptimizer] Manual Bake Avatar";
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
            var originalBasePath = RuntimeUtil.RelativePath(null, avatar);
            avatar = Object.Instantiate(avatar);
            avatar.transform.position += Vector3.forward * 2;
            var clonedBasePath = RuntimeUtil.RelativePath(null, avatar);

            BuildReport.Clear();
            try
            {
                var session = new OptimizerSession(avatar, Utils.CreateOutputAssetFile(original.name));
                EarlyOptimizerProcessor.ProcessObject(session);
                OptimizerProcessor.ProcessObject(session);
            }
            finally
            {
                BuildReport.RemapPaths(originalBasePath, clonedBasePath);
                Selection.objects = new Object[] { avatar };
            }
        }
    }
}
