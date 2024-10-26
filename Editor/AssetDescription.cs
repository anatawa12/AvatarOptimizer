using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf.localization;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer
{
    [CreateAssetMenu(fileName = "New Avatar Optimizer Asset Description", menuName = "Avatar Optimizer/Asset Description")]
    internal class AssetDescription : ScriptableObject
    {
        [SerializeField]
        [TextArea]
        // only for serialization so ignore warning
#pragma warning disable CS0414
        private string comment = "";
#pragma warning restore CS0414
        [SerializeField]
        private ClassReference[] meaninglessComponents = Array.Empty<ClassReference>();

        const int MonoScriptIdentifierType = 1;

        private static AssetDescriptionData? _data;

        private static AssetDescriptionData Data => _data ??= LoadData();

        class AssetDescriptionData
        {
            public HashSet<Type> meaninglessComponents = new();
        }

        static AssetDescriptionData LoadData()
        {
            var data = new AssetDescriptionData();
            foreach (var description in GetAllAssetDescriptions())
            {
                foreach (var component in description.meaninglessComponents)
                    if (GetMonoScriptFromGuid(component.guid, component.fileID) is MonoScript monoScript)
                        data.meaninglessComponents.Add(monoScript.GetClass());
            }

            return data;
        }

        private static IEnumerable<AssetDescription> GetAllAssetDescriptions()
        {
            foreach (var findAsset in AssetDatabase.FindAssets("t:AssetDescription"))
            {
                var path = AssetDatabase.GUIDToAssetPath(findAsset);
                var asset = AssetDatabase.LoadAssetAtPath<AssetDescription>(path);
                if (asset != null) yield return asset;
            }
        }

        public static void Reload() => _data = LoadData();
        public static HashSet<Type> GetMeaninglessComponents() => Data.meaninglessComponents;

        private static Object GetMonoScriptFromGuid(string guid, ulong fileid)
        {
            var idString = $"GlobalObjectId_V1-{MonoScriptIdentifierType}-{guid}-{fileid}-0";
            Debug.Assert(GlobalObjectId.TryParse(idString, out var id));
            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
        }

        [CustomEditor(typeof(AssetDescription))]
        internal class AssetDescriptionEditor : Editor
        {
            private SerializedProperty _comment = null!; // Initialized by OnEnable
            private SerializedProperty _meaninglessComponents = null!; // Initialized by OnEnable

            private void OnEnable()
            {
                _comment = serializedObject.FindProperty("comment");
                _meaninglessComponents = serializedObject.FindProperty("meaninglessComponents");
            }

            public override void OnInspectorGUI()
            {
                AAOL10N.DrawLanguagePicker();
                EditorGUILayout.LabelField(AAOL10N.Tr("AssetDescription:Description"), EditorStyles.wordWrappedLabel);
                if (GUILayout.Button(AAOL10N.Tr("AssetDescription:OpenDocs")))
                {
                    var baseUrl = CheckForUpdate.Checker.IsBeta ? "https://vpm.anatawa12.com/avatar-optimizer/beta/" : "https://vpm.anatawa12.com/avatar-optimizer/";
                    var isJapanese = LanguagePrefs.Language == "ja";
                    baseUrl += isJapanese ? "ja/" : "en/";
                    baseUrl += "docs/developers/asset-description/";
                    Application.OpenURL(baseUrl);
                }

                EditorGUILayout.Space(20f);
                EditorGUILayout.PropertyField(_comment);
                EditorGUILayout.PropertyField(_meaninglessComponents);

                serializedObject.ApplyModifiedProperties();
            }
        }

        // this struct holds guid + fileid reference to the ScriptAsset with fully qualified name and memo
        [Serializable]
        struct ClassReference
        {
            [SerializeField]
            public string className;
            [SerializeField]
            public string guid;
            [SerializeField]
            public ulong fileID;
        }
        
        [CustomPropertyDrawer(typeof(ClassReference))]
        internal class ClassReferenceEditor : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                // class
                var guidProperty = property.FindPropertyRelative("guid");
                var fileIDProperty = property.FindPropertyRelative("fileID");
                var classNameProperty = property.FindPropertyRelative("className");

                var mixed = guidProperty.hasMultipleDifferentValues || fileIDProperty.hasMultipleDifferentValues;
                EditorGUI.showMixedValue = mixed;

                var guid = guidProperty.stringValue;
                var fileID = fileIDProperty.longValue;

                if (guid == "" && fileID == 0)
                {
                    // it's none
                    var asset = EditorGUI.ObjectField(position, label, null, typeof(MonoScript), false);
                    if (asset != null) SetScript(asset);
                }
                else
                {
                    var obj = GetMonoScriptFromGuid(guid, (ulong)fileID);
                    if (obj == null)
                    {
                        // missing
                        EditorGUI.LabelField(position, label, MissingScriptContent(classNameProperty.stringValue));
                    }
                    else
                    {
                        // found
                        EditorGUI.BeginChangeCheck();
                        var asset = EditorGUI.ObjectField(position, label, obj, typeof(MonoScript), false);
                        if (EditorGUI.EndChangeCheck())
                            SetScript(asset);
                    }
                }

                void SetScript(Object asset)
                {
                    var type = (asset as MonoScript)?.GetClass();
                    if (asset != null && type != null)
                    {
                        var id = GlobalObjectId.GetGlobalObjectIdSlow(asset);
                        Debug.Assert(id.identifierType == MonoScriptIdentifierType);
                        Debug.Assert(id.targetPrefabId == 0);
                        guidProperty.stringValue = id.assetGUID.ToString();
                        fileIDProperty.longValue = (long)id.targetObjectId;
                        classNameProperty.stringValue = type.Name;
                    }
                    else if (asset == null)
                    {
                        guidProperty.stringValue = "";
                        fileIDProperty.longValue = 0;
                        classNameProperty.stringValue = "";
                    }
                }

                EditorGUI.showMixedValue = false;
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
