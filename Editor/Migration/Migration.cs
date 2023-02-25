using System;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Migration
{
    [InitializeOnLoad]
    public static class Migration
    {
        static Migration()
        {
            // migration
            foreach (var mergeSkinnedMesh in Resources.FindObjectsOfTypeAll<MergeSkinnedMesh>())
            {
                var id = GlobalObjectId.GetGlobalObjectIdSlow(mergeSkinnedMesh);
                Debug.Log($"found merge skinned mesh: {mergeSkinnedMesh} {id} at" +
                          $" {AssetDatabase.GUIDToAssetPath(id.assetGUID.ToString())}");
            }

            EditorApplication.update += Update;
        }

        private static void Update()
        {
            try
            {
                if (PrereleaseStateDetector.MigrationRequired())
                {
                    if (DoMigrate())
                        PrereleaseStateDetector.MigrationFinished();
                }
                else
                {
                    PrereleaseStateDetector.MigrationFinished();
                }

            }
            catch
            {
                EditorUtility.DisplayDialog("Error!", "Error in migration process!", "OK");
                throw;
            }
            finally
            {
                EditorApplication.update -= Update;
            }
        }

        private static bool DoMigrate()
        {
            var result = EditorUtility.DisplayDialogComplex("Migration Required!",
                @"Upgrading AvatarOptimizer between major versions detected!
BACK UP YOUR PROJECT!
You have to migrate all scenes and prefabs to make AvatarOptimizer works correctly!
This may took long time and may break your project!
If you don't have backup, please migrate after creating backup!
Until migration is proceed, this tool will be ask you for migration for each time relaunching Unity.

Do you want to migrate project now?",
                "Migrate everything",
                "Cancel",
                "Migrate Prefabs Only");
            switch (result)
            {
                case 0: // migrate all
                    MigratePrefabs();
                    MigrateAllScenes();
                    return true;
                case 1: // do not migrate
                    return false;
                case 2: // migrate prefabs
                    MigratePrefabs();
                    return true;
                default: // unknown
                    return false; 
            }
        }

        private static void MigratePrefabs()
        {
        }

        private static void MigrateAllScenes()
        {
        }
    }
}
