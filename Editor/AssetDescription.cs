using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using nadena.dev.ndmf.localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

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
        /// <summary>
        /// <para>
        /// The animator parameters that External Tools reads.
        /// </para>
        /// <para>
        /// Avatar Optimizer will treat changing those parameters as side effects.
        ///
        /// As a part of optimization, Avatar Optimizer may remove PhysBones or Contact Receivers that are not used by the animator.
        /// However, if those parameters are used by some External Tools like OSC Tools in VRChat, it may break the behavior of the avatar.
        /// Registering parameters to this will prevent Avatar Optimizer from removing PhysBones or Contact Receivers that are used by OSC Tools.
        /// </para>
        /// </summary>
        [SerializeField]
        private OscParameter[] parametersReadByExternalTools = Array.Empty<OscParameter>();
        /// <summary>
        /// <para>
        /// The animator parameters that OSC Tools changes.
        /// </para>
        /// <para>
        /// Avatar Optimizer will assume those parameters might be changed by some external tools.
        ///
        /// Currently, this configuration is unused.
        ///
        /// Avatar Optimizer will implement optimizing Animator Controller by fixing non-animated parameters.
        /// However, if those parameters are changed by some external tools like OSC Tools in VRChat, it may break the behavior of the avatar.
        /// Therefore, registering parameters to this will prevent Avatar Optimizer from fixing non-animated parameters.
        /// </para>
        /// </summary>
        [SerializeField]
        private OscParameter[] parametersChangedByExternalTools = Array.Empty<OscParameter>();

        const int MonoScriptIdentifierType = 1;

        private static AssetDescriptionData? _data;

        private static AssetDescriptionData Data => _data ??= LoadData();

        class AssetDescriptionData
        {
            public HashSet<Type> meaninglessComponents = new();
            public OscParameterInfo parametersReadByExternalTools = OscParameterInfo.New();
            public OscParameterInfo parametersChangedByExternalTools = OscParameterInfo.New();
        }

        internal struct OscParameterInfo
        {
            public HashSet<string> ExactMatch;
            public List<Regex> RegexMatch;

            public static OscParameterInfo New()
            {
                return new OscParameterInfo
                {
                    ExactMatch = new HashSet<string>(),
                    RegexMatch = new List<Regex>(),
                };
            }

            public void Add(OscParameter parameter, AssetDescription desc)
            {
                if (parameter.name == "") return;
                switch (parameter.matchMode)
                {
                    case OscParameter.MatchMode.Exact:
                        ExactMatch.Add(parameter.name);
                        break;
                    case OscParameter.MatchMode.Regex:
                        try
                        {
                            _ = new Regex($"{parameter.name}", RegexOptions.CultureInvariant | RegexOptions.Compiled);
                            var regex = new Regex($"^(?:{parameter.name})$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
                            RegexMatch.Add(regex);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(
                                new ArgumentException(
                                    $"Invalid regex: {parameter.name} in asset description {desc.name}", e), desc);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        static AssetDescriptionData LoadData()
        {
            var data = new AssetDescriptionData();
            foreach (var description in GetAllAssetDescriptions())
            {
                foreach (var component in description.meaninglessComponents)
                    if (GetMonoScriptFromGuid(component.guid, component.fileID) is MonoScript monoScript)
                        data.meaninglessComponents.Add(monoScript.GetClass());

                foreach (var parameter in description.parametersReadByExternalTools)
                    data.parametersReadByExternalTools.Add(parameter, description);
                foreach (var parameter in description.parametersChangedByExternalTools)
                    data.parametersChangedByExternalTools.Add(parameter, description);
            }

            return data;
        }

        internal void AddMeaninglessComponents(IEnumerable<MonoScript> types)
        {
            var meaninglessComponentsList = meaninglessComponents.ToList();
            foreach (var type in types)
            {
                var id = GlobalObjectId.GetGlobalObjectIdSlow(type);
                Utils.Assert(id.identifierType == MonoScriptIdentifierType);
                Utils.Assert(id.targetPrefabId == 0);
                meaninglessComponentsList.Add(new ClassReference
                {
                    className = type.GetClass()?.Name ?? "",
                    guid = id.assetGUID.ToString(),
                    fileID = id.targetObjectId,
                });
            }
            meaninglessComponents = meaninglessComponentsList.ToArray();
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
        public static OscParameterInfo GetParametersReadByExternalTools() => Data.parametersReadByExternalTools;
        public static OscParameterInfo GetParametersChangedByExternalTools() => Data.parametersChangedByExternalTools;

        private static Object GetMonoScriptFromGuid(string guid, ulong fileid)
        {
            var idString = $"GlobalObjectId_V1-{MonoScriptIdentifierType}-{guid}-{fileid}-0";
            Utils.Assert(GlobalObjectId.TryParse(idString, out var id));
            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
        }

        [CustomEditor(typeof(AssetDescription))]
        internal class AssetDescriptionEditor : Editor
        {
            private SerializedProperty _comment = null!; // Initialized by OnEnable
            private SerializedProperty _meaninglessComponents = null!; // Initialized by OnEnable
            private SerializedProperty _parametersExternalToolsReads = null!; // Initialized by OnEnable
            private SerializedProperty _parametersExternalsToolsChanges = null!; // Initialized by OnEnable

            private void OnEnable()
            {
                _comment = serializedObject.FindProperty("comment");
                _meaninglessComponents = serializedObject.FindProperty("meaninglessComponents");
                _parametersExternalToolsReads = serializedObject.FindProperty(nameof(parametersReadByExternalTools));
                _parametersExternalsToolsChanges = serializedObject.FindProperty(nameof(parametersChangedByExternalTools));
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
                EditorGUILayout.PropertyField(_parametersExternalToolsReads, true);
                EditorGUILayout.PropertyField(_parametersExternalsToolsChanges, true);

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
                        Utils.Assert(id.identifierType == MonoScriptIdentifierType);
                        Utils.Assert(id.targetPrefabId == 0);
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
        
        // This class describes the OSC parameter
        [Serializable]
        public struct OscParameter : IEquatable<OscParameter>
        {
            [SerializeField]
            public string name;
            [SerializeField]
            public MatchMode matchMode;

            public enum MatchMode
            {
                Exact,
                Regex,
            }

            public bool Equals(OscParameter other) => name == other.name && matchMode == other.matchMode;
            public override bool Equals(object? obj) => obj is OscParameter other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(name, (int)matchMode);
            public static bool operator ==(OscParameter left, OscParameter right) => left.Equals(right);
            public static bool operator !=(OscParameter left, OscParameter right) => !left.Equals(right);
        }

        [CustomPropertyDrawer(typeof(OscParameter))]
        internal class OscParameterEditor : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 
                EditorGUIUtility.singleLineHeight;

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var nameProperty = property.FindPropertyRelative("name");
                var matchModeProperty = property.FindPropertyRelative("matchMode");

                var rect = EditorGUI.PrefixLabel(position, label);
                Rect text, popup;

                float width;
                if (rect.width > 200)
                    width = 99.5f;
                else
                    width = rect.width / 2 - 0.5f;

                var color = GUI.color;
                if (matchModeProperty.enumValueIndex == (int)OscParameter.MatchMode.Regex)
                {
                    try
                    {
                        _ = new Regex($"{nameProperty.stringValue}", RegexOptions.CultureInvariant | RegexOptions.Compiled);
                        _ = new Regex($"^(?:{nameProperty.stringValue})$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
                    }
                    catch
                    {
                        GUI.color = Color.red;
                    }
                }

                text = rect with { width = rect.width - width - 1 };
                popup = rect with { x = rect.xMax - width, width = width };

                EditorGUI.BeginChangeCheck();
                var newName = EditorGUI.TextField(text, nameProperty.stringValue);
                if (EditorGUI.EndChangeCheck())
                    nameProperty.stringValue = newName;

                GUI.color = color;
                
                EditorGUI.BeginChangeCheck();
                var newMatchMode = (OscParameter.MatchMode)EditorGUI.EnumPopup(popup, (OscParameter.MatchMode)matchModeProperty.enumValueIndex);
                if (EditorGUI.EndChangeCheck())
                    matchModeProperty.enumValueIndex = (int)newMatchMode;
            }
        }
    }
}
