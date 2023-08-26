using System.IO;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Anatawa12.AvatarOptimizer.Test
{
    internal static class TestUtils
    {
        public static GameObject NewAvatar(string name = null)
        {
            var root = new GameObject();
            root.name = name ?? "Test Avatar";
            var animator = root.AddComponent<Animator>();
            animator.avatar = AvatarBuilder.BuildGenericAvatar(root, "");
            var descriptor = root.AddComponent<VRCAvatarDescriptor>();
            return root;
        }

        public static string GetAssetPath(string testRelativePath)
        {
            var path = AssetDatabase.GUIDToAssetPath("fc50cab76afb46348d98df4ce8d84e8b");
            var baseDir = path.Substring(0, path.LastIndexOf('/'));
            return $"{baseDir}/{testRelativePath}";
        }

        public static T GetAssetAt<T>(string testRelativePath) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(GetAssetPath(testRelativePath));
    }
}
