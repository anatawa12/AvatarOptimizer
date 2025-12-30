using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Anatawa12.AvatarOptimizer
{
    internal class AvatarOptimizerSettingsProvider : SettingsProvider
    {
        private SerializedObject _serializedObject;
        private SerializedProperty _ignoredComponentsProperty;

        private AvatarOptimizerSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _serializedObject = new SerializedObject(AvatarOptimizerSettings.instance);
            _ignoredComponentsProperty = _serializedObject.FindProperty("ignoredComponents");
        }

        public override void OnGUI(string searchContext)
        {
            AAOL10N.DrawLanguagePicker();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(AAOL10N.Tr("AvatarOptimizerSettings:Header"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(AAOL10N.Tr("AvatarOptimizerSettings:Description"), EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button(AAOL10N.Tr("AvatarOptimizerSettings:OpenDocs")))
            {
                var url = AAOL10N.Tr("AvatarOptimizerSettings:DocsUrl");
                System.Diagnostics.Process.Start(url);
            }

            EditorGUILayout.Space();

            _serializedObject.Update();
            EditorGUILayout.PropertyField(_ignoredComponentsProperty, new GUIContent(AAOL10N.Tr("AvatarOptimizerSettings:IgnoredComponents")), true);
            
            if (_serializedObject.ApplyModifiedProperties())
            {
                AvatarOptimizerSettings.instance.Save(true);
            }

            EditorGUILayout.Space();
            
            if (GUILayout.Button(AAOL10N.Tr("AvatarOptimizerSettings:ClearAll")))
            {
                if (EditorUtility.DisplayDialog(
                    AAOL10N.Tr("AvatarOptimizerSettings:ClearAll:Confirm:Title"),
                    AAOL10N.Tr("AvatarOptimizerSettings:ClearAll:Confirm:Message"),
                    AAOL10N.Tr("AvatarOptimizerSettings:ClearAll:Confirm:OK"),
                    AAOL10N.Tr("AvatarOptimizerSettings:ClearAll:Confirm:Cancel")))
                {
                    AvatarOptimizerSettings.instance.ClearIgnoredComponents();
                    _serializedObject.Update();
                }
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new AvatarOptimizerSettingsProvider("Project/Avatar Optimizer", SettingsScope.Project);
            provider.keywords = new HashSet<string>(new[] { "Avatar", "Optimizer", "Ignored", "Components", "Unknown", "IEditorOnly" });
            return provider;
        }
    }
}
