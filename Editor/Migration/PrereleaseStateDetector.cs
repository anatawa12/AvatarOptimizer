using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Migration
{
    /// <summary>
    /// This class will detect version migration.
    /// </summary>
    [InitializeOnLoad]
    internal static class PrereleaseStateDetector
    {
        private const string DataPath = "ProjectSettings/com.anatawa12.avatar-optimizer.v0.json";

        static PrereleaseStateDetector()
        {
            if (!File.Exists(DataPath))
            {
                // This pass can be one of
                // - installed 0.1.2 / older
                // - installed 1.0.0 / newer
                // - not installed yet
                // We ignore 0.1.2 / older. If it's 1.0.0, no migration is required so nothing to do here.
                return;
            }
            var data = JsonUtility.FromJson<JsonData>(File.ReadAllText(DataPath));
            Debug.Log($"[AvatarOptimizer.Migration] We found AvatarOptimizer v0.{data.currentSerializedVersion}.x save format.");
            if (data.currentSerializedVersion == 4)
            {
                Debug.Log($"[AvatarOptimizer.Migration] It's compatible with v1.x.x. removing v0.x version data file.");
                File.Delete(DataPath);
                return;
            }
            Debug.Log($"[AvatarOptimizer.Migration] We will show warning dialog");
            EditorApplication.delayCall += ShowMigrationRequiredWarning;
        }

        private static void ShowMigrationRequiredWarning()
        {
            var isJapanese = true;

            while (true)
            {
                string title, message, ok, ignore, alt;
                string checkAgain, back;
                if (isJapanese)
                {
                    title = "MIGRATION REQUIRED";
                    message = "AvatarOptimizer v0.3.xまたはそれ以前のバージョンがインストールされていたようです。\n" +
                              "v1.x.xにアップグレードする前に、v0.4.xをインストールし、設定フォーマットを移行する必要があります。\n" +
                              "移行しないと、AvatarOptimizerの設定が失われます。\n" +
                              "\n" +
                              "以下の手順でプロジェクトを移行してください。\n" +
                              "1. アセットを保存せずにUnityを終了する\n" +
                              "*v1.x.xをインストールしてから保存したAvatarOptimizerを含むアセットがある場合には設定が失われます*\n" +
                              "2. AvatarOptimizerをv0.4.xにダウングレードする\n" +
                              "3. Unityを起動し、v0.4.xへのマイグレーションを実行する\n" +
                              "4. AvatarOptimizerを再度アップグレードする";
                    ok = "保存せずにUnityを閉じる";
                    ignore = "無視する";
                    checkAgain = "AvatarOptimizerの設定が失われる可能性があります。\n" +
                                           "本当によろしいですか?";
                    back = "警告を読み直す";
                    alt = "Read in English";
                }
                else
                {
                    title = "MIGRATION REQUIRED";
                    message = "We found previously AvatarOptimizer v0.3.x or older is installed!\n" +
                              "Before upgrading to v1.x.x, you have to install v0.4.x and migrate your configuration format.\n" +
                              "Without migration, you'll lost the configurations of AvatarOptimizer.\n" +
                              "\n" +
                              "Please follow the following steps to migrate your project.\n" +
                              "1. Close Unity WITHOUT saving assets. \n" +
                              "*If you saved some assets with AvatarOptimizer after upgrading to v1.0.0, You will lost configuration.*\n" +
                              "2. Downgrade AvatarOptimizer to v0.4.x.\n" +
                              "3. Open Unity and run migration\n" +
                              "4. Upgrade AvatarOptimizer again.";
                    ok = "Exit Unity without saving anything";
                    ignore = "Ignore";
                    checkAgain = "Do you REALLY want to ignore this warning?\n" +
                                 "You'll lost the configurations of AvatarOptimizer";
                    back = "Back to Warning";
                    alt = "日本語で読む";
                }
            
                switch (EditorUtility.DisplayDialogComplex(title, message, ok, ignore, alt))
                {
                    case 0: // OK: Exit
                        EditorApplication.Exit(0);
                        return;
                    case 1: // Cancel: Ignore
                        if (EditorUtility.DisplayDialog(title, checkAgain, ignore, back))
                        {
                            File.Delete(DataPath);
                            return;
                        }
                        break;
                    case 2: // Show in another language
                        isJapanese = !isJapanese;
                        break;
                }
            }
        }

        [Serializable]
        private class JsonData
        {
            // serialize version is 0.x. if 0.0, it means nothing is reloaded
            public int currentSerializedVersion;
        }
    }
}
