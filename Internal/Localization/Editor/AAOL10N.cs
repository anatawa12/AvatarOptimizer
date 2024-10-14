using System.Collections.Generic;
using nadena.dev.ndmf.localization;
using nadena.dev.ndmf.ui;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// Wrapper class of NDMF Localizer with static methods
    /// </summary>
    public static class AAOL10N
    {
        public static readonly Localizer Localizer = new Localizer("en-us", () =>
        {
            var localizationFolder = AssetDatabase.GUIDToAssetPath("8b53df9f2e18428bbba2165c4f2af9be") + "/";
            return new List<LocalizationAsset>
            {
                AssetDatabase.LoadAssetAtPath<LocalizationAsset>(localizationFolder + "en-us.po"),
                AssetDatabase.LoadAssetAtPath<LocalizationAsset>(localizationFolder + "ja-jp.po"),
                AssetDatabase.LoadAssetAtPath<LocalizationAsset>(localizationFolder + "zh-hans.po"),
                AssetDatabase.LoadAssetAtPath<LocalizationAsset>(localizationFolder + "zh-hant.po"),
            };
        });


        public static string Tr(string key) => Localizer.GetLocalizedString(key);

        public static string? TryTr(string descriptionKey)
        {
            if (Localizer.TryGetLocalizedString(descriptionKey, out var result))
                return result;
            return null;
        }

        public static void DrawLanguagePicker() => LanguageSwitcher.DrawImmediate();
    }
}
