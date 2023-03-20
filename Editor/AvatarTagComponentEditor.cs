using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [UsedImplicitly]
    public class AvatarTagComponentEditor
    {
        [UsedImplicitly]
        public static void SetCurrentSaveVersion(AvatarTagComponent component)
        {
            var nestCount = NestCount(component);
            if (nestCount < component.saveVersions.Length)
                return; // already defined: we don't have to update nestCount
            // resize
            var old = component.saveVersions;
            component.saveVersions = new int[nestCount + 1];
            System.Array.Copy(old, component.saveVersions, nestCount);
            component.saveVersions[nestCount] = Migration.PrereleaseStateDetector.GetCurrentVersion();
            EditorUtility.SetDirty(component);
        }
        
        private static int NestCount(Object instance)
        {
            var nestCount = 0;
            while ((bool)(instance = PrefabUtility.GetCorrespondingObjectFromSource(instance)))
                nestCount++;

            return nestCount;
        }
    }
}
