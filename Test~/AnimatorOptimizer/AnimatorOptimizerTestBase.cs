using UnityEditor;
using UnityEditor.Animations;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    // Infrastucture for AnimatorOptimizer tests
    public class AnimatorOptimizerTestBase
    {
        public AnimatorOptimizerTestBase()
        {
            var name = GetType().Name;
            if (name.EndsWith("Test")) name = name.Substring(0, name.Length - 4);
            if (name.EndsWith("Tests")) name = name.Substring(0, name.Length - 5);
            TestName = name;
        }

        public AnimatorController LoadCloneAnimatorController(string name)
        {
            var path = TestUtils.GetAssetPath($"AnimatorOptimizer/{TestName}/{name}.controller");
            var cloned = TestUtils.GetAssetPath($"AnimatorOptimizer/{TestName}/{name}.copied.controller");
            AssetDatabase.CopyAsset(path, cloned);
            var loaded = AssetDatabase.LoadAssetAtPath<AnimatorController>(cloned);
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(cloned))
                AssetDatabase.RemoveObjectFromAsset(o);
            AssetDatabase.DeleteAsset(cloned);
            return loaded;
        }

        public AnimatorController LoadAnimatorController(string name)
        {
            return TestUtils.GetAssetAt<AnimatorController>($"AnimatorOptimizer/{TestName}/{name}.controller");
        }

        public string TestName { get; }
    }
}