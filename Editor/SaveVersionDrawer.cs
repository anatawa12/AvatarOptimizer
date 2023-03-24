using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer
{
    class SaveVersionDrawer
    {
        private int nestCount = -1;
        [CanBeNull] private SerializedProperty _versionArray;
        [CanBeNull] private SerializedProperty _currentVersion;
        [CanBeNull] private GUIContent _content;
        public void Draw(SerializedObject serializedObject)
        {
            EditorGUI.BeginDisabledGroup(true);
            try
            {
                if (serializedObject.isEditingMultipleObjects)
                {
                    EditorGUILayout.LabelField("SaveVersion", "multi editing preview not supported");
                    return;
                }

                if (nestCount == -1)
                    nestCount = NestCount(serializedObject.targetObject);

                if (_content == null)
                    _content = new GUIContent($"SaveVersion{nestCount}");

                if (_versionArray == null)
                    _versionArray = serializedObject.FindProperty(nameof(AvatarTagComponent.saveVersions));
                Debug.Assert(_versionArray != null, nameof(_versionArray) + " != null");

                if (_currentVersion != null)
                {
                    EditorGUILayout.PropertyField(_currentVersion, _content);
                }
                else if (nestCount < _versionArray.arraySize)
                {
                    _currentVersion = _versionArray.GetArrayElementAtIndex(nestCount);
                    EditorGUILayout.PropertyField(_currentVersion, _content);
                }
                else
                {
                    EditorGUILayout.LabelField(_content, new GUIContent("Undefined"));
                }
            }
            finally
            {
                EditorGUI.EndDisabledGroup();
            }
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
