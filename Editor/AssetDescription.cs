using System;
using CustomLocalization4EditorExtension;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer
{
    [CreateAssetMenu(fileName = "New Avatar Optimizer Asset Description", menuName = "Avatar Optimizer/Asset Descriotion")]
    internal class AssetDescription : ScriptableObject
    {
        [SerializeField]
        private ClassReference[] meaninglessComponents = Array.Empty<ClassReference>();

        [CustomEditor(typeof(AssetDescription))]
        internal class AssetDescriptionEditor : Editor
        {
            private SerializedProperty _meaninglessComponents;

            private void OnEnable()
            {
                _meaninglessComponents = serializedObject.FindProperty("meaninglessComponents");
            }

            public override void OnInspectorGUI()
            {
                CL4EE.DrawLanguagePicker();
                GUILayout.Label(CL4EE.Tr("AssetDescription:Header"), EditorStyles.boldLabel);
                EditorGUILayout.LabelField(CL4EE.Tr("AssetDescription:Description"), EditorStyles.wordWrappedLabel);
                if (GUILayout.Button(CL4EE.Tr("AssetDescription:OpenDocs")))
                {
                    var baseUrl = CheckForUpdate.IsBeta ? "https://vpm.anatawa12.com/avatar-optimizer/beta/" : "https://vpm.anatawa12.com/avatar-optimizer/";
                    var isJapanese = CL4EE.GetLocalization()?.CurrentLocaleCode == "ja";
                    baseUrl += isJapanese ? "ja/" : "en/";
                    baseUrl += "developers/asset-description/";
                    Application.OpenURL(baseUrl);
                }

                EditorGUILayout.PropertyField(_meaninglessComponents);
            }
        }

        // this struct holds guid + fileid reference to the ScriptAsset with fully qualified name and memo
        [Serializable]
        struct ClassReference
        {
            [SerializeField]
            private string className;
            [SerializeField]
            private string guid;
            [SerializeField]
            private ulong fileid;
            [SerializeField]
            private string comment;
        }
        
        [CustomPropertyDrawer(typeof(ClassReference))]
        internal class ClassReferenceEditor : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                var line = EditorGUIUtility.singleLineHeight;
                var space = EditorGUIUtility.standardVerticalSpacing;
                if (!property.isExpanded)
                {
                    return line;
                }
                else
                {
                    return line // header
                           + space + line // class
                           + space + line // comment
                        ;
                }
            }

            const int IdentifierType = 1;

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                position.height = EditorGUIUtility.singleLineHeight;
                property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
                if (!property.isExpanded) return;

                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // class
                var guidProperty = property.FindPropertyRelative("guid");
                var fileidProperty = property.FindPropertyRelative("fileid");
                var classNameProperty = property.FindPropertyRelative("className");

                var mixed = guidProperty.hasMultipleDifferentValues || fileidProperty.hasMultipleDifferentValues;
                EditorGUI.showMixedValue = mixed;

                var guid = guidProperty.stringValue;
                var fileid = fileidProperty.longValue;

                var scriptLabel = new GUIContent("Script");

                if (guid == "" && fileid == 0)
                {
                    // it's none
                    var asset = EditorGUI.ObjectField(position, scriptLabel, null, typeof(MonoScript), false);
                    if (asset != null) SetScript(asset);
                }
                else
                {
                    if (!GlobalObjectId.TryParse($"GlobalObjectId_V1-{IdentifierType}-{guid}-{fileid}-0", out var id))
                    {
                        // internal error
                        EditorGUI.LabelField(position, scriptLabel,
                            new GUIContent("Internal Error: paese ObjectId failed"));
                    }
                    else
                    {
                        var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
                        if (obj == null)
                        {
                            // missing
                            EditorGUI.LabelField(position, scriptLabel, MissingScriptContent(classNameProperty.stringValue));
                        }
                        else
                        {
                            // found
                            EditorGUI.BeginChangeCheck();
                            var asset = EditorGUI.ObjectField(position, scriptLabel, obj, typeof(MonoScript), false);
                            if (EditorGUI.EndChangeCheck())
                                SetScript(asset);
                        }
                    }
                }

                void SetScript(Object asset)
                {
                    var type = (asset as MonoScript)?.GetClass();
                    if (asset != null && type != null)
                    {
                        var id = GlobalObjectId.GetGlobalObjectIdSlow(asset);
                        Debug.Assert(id.identifierType == IdentifierType);
                        Debug.Assert(id.targetPrefabId == 0);
                        guidProperty.stringValue = id.assetGUID.ToString();
                        fileidProperty.longValue = (long)id.targetObjectId;
                        classNameProperty.stringValue = type.Name;
                    }
                    else if (asset == null)
                    {
                        guidProperty.stringValue = "";
                        fileidProperty.longValue = 0;
                        classNameProperty.stringValue = "";
                    }
                }

                EditorGUI.showMixedValue = false;

                // comment
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                var commentProperty = property.FindPropertyRelative("comment");
                EditorGUI.PropertyField(position, commentProperty, new GUIContent("Comment"));
            }

            static class Constants
            {
                public static readonly GUIContent MixedValueContent = EditorGUIUtility.TrTextContent("—", "Mixed Values");
            }

            GUIContent MissingScriptContent(string className) => EditorGUI.showMixedValue
                ? Constants.MixedValueContent
                : new GUIContent($"Missing: {className}");
        }
    }
}
