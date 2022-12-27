
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.Merger
{
    public class MoveInPrefabObjectTest : EditorWindow
    {
        [MenuItem("Tools/MoveInPrefabObject")]
        public static void OpenWindow()
        {
            CreateWindow<MoveInPrefabObjectTest>();
        }

        private Transform _parent;
        private Transform _child;

        private void OnGUI()
        {
            _parent = (Transform)EditorGUILayout.ObjectField("parent", _parent, typeof(Transform), true);
            _child = (Transform)EditorGUILayout.ObjectField("child", _child, typeof(Transform), true);
            using (new EditorGUI.DisabledScope())
            {
                if (GUILayout.Button("MakeParent"))
                {
                    var serializedChild = new SerializedObject(_child);
                    var oldParent = _child.parent;
                    var serializedOldParent = new SerializedObject(oldParent);
                    var serializedNewParent = new SerializedObject(_parent);
                    var childrenOfOldProp = serializedOldParent.FindProperty("m_Children");
                    var childrenOfNewProp = serializedNewParent.FindProperty("m_Children");
                    var fatherProp = serializedChild.FindProperty("m_Father");

                    var inParentIndex = Enumerable.Range(0, childrenOfOldProp.arraySize)
                        .FirstOrDefault(i => childrenOfOldProp.GetArrayElementAtIndex(i).objectReferenceValue == _child);
                    for (var i = inParentIndex + 1; i < childrenOfOldProp.arraySize; i++)
                        childrenOfOldProp.GetArrayElementAtIndex(i - 1).objectReferenceValue =
                            childrenOfOldProp.GetArrayElementAtIndex(i).objectReferenceValue;
                    childrenOfOldProp.arraySize -= 1;

                    fatherProp.objectReferenceValue = _parent;

                    childrenOfNewProp.arraySize += 1;
                    childrenOfNewProp.GetArrayElementAtIndex(childrenOfNewProp.arraySize - 1)
                        .objectReferenceValue = _child;

                    serializedChild.ApplyModifiedProperties();
                    serializedOldParent.ApplyModifiedProperties();
                    serializedNewParent.ApplyModifiedProperties();

                    EditorUtility.SetDirty(_child);
                    EditorUtility.SetDirty(oldParent);
                    EditorUtility.SetDirty(_parent);
                    // This makes "Disconnecting is no longer implemented" error and reopening scene breaks the movement
                }
            }
        }
    }
}
