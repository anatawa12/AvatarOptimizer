using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(RemoveMeshInBox))]
    internal class RemoveMeshInBoxEditor : Editor
    {
        private SerializedProperty _boxes;
        private int _editingBox = -1;
        private (Quaternion value, Vector3 euler)[] _eulerAngle = Array.Empty<(Quaternion, Vector3)>();

        private void OnEnable()
        {
            _boxes = serializedObject.FindProperty("boxes");
        }

        public override void OnInspectorGUI()
        {
            if (targets.Length != 1)
            {
                base.OnInspectorGUI();
                return;
            }

            if (_eulerAngle.Length < _boxes.arraySize)
            {
                _eulerAngle = _eulerAngle.Concat(Enumerable.Repeat(default((Quaternion, Vector3)),
                        _boxes.arraySize - _eulerAngle.Length))
                    .ToArray();
            }

            for (var i = 0; i < _boxes.arraySize; i++)
            {
                var box = _boxes.GetArrayElementAtIndex(i);
                var centerProp = box.FindPropertyRelative("center");
                var sizeProp = box.FindPropertyRelative("size");
                var rotationProp = box.FindPropertyRelative("rotation");

                EditorGUILayout.LabelField($"Box #{i}");
                using (new EditorGUI.IndentLevelScope())
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Remove This Box"))
                        {
                            _boxes.DeleteArrayElementAtIndex(i);
                            i--;
                        }

                        if (GUILayout.Button(_editingBox == i ? "Finish Editing Box" : "Edit This Box"))
                        {
                            _editingBox = _editingBox == i ? -1 : i;
                            SceneView.RepaintAll(); // to show/hide gizmo
                        }
                    }

                    EditorGUILayout.PropertyField(centerProp);
                    EditorGUILayout.PropertyField(sizeProp);
                    if (_eulerAngle[i].value != rotationProp.quaternionValue)
                    {
                        _eulerAngle[i] = (rotationProp.quaternionValue, rotationProp.quaternionValue.eulerAngles);
                    }

                    // rotation in euler
                    var label = new GUIContent("Rotation");
                    var rect = EditorGUILayout.GetControlRect(true, 18f, EditorStyles.numberField);
                    label = EditorGUI.BeginProperty(rect, label, rotationProp);
                    EditorGUI.BeginChangeCheck();
                    var euler = EditorGUI.Vector3Field(rect, label, _eulerAngle[i].euler);
                    if (EditorGUI.EndChangeCheck())
                    {
                        var quot = Quaternion.Euler(euler);
                        rotationProp.quaternionValue = quot;
                        _eulerAngle[i] = (quot, euler);
                    }
                    EditorGUI.EndProperty();
                }
            }

            if (GUILayout.Button($"Add Box"))
            {
                _boxes.arraySize += 1;
                
                var box = _boxes.GetArrayElementAtIndex(_boxes.arraySize - 1);
                box.FindPropertyRelative("center").vector3Value = Vector3.zero;
                box.FindPropertyRelative("size").vector3Value = Vector3.one;
                box.FindPropertyRelative("rotation").quaternionValue = Quaternion.identity;
                _editingBox = _boxes.arraySize - 1;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            if (_editingBox < 0 || _boxes.arraySize <= _editingBox || targets.Length != 1) return;

            var box = _boxes.GetArrayElementAtIndex(_editingBox);
            var centerProp = box.FindPropertyRelative("center");
            var rotationProp = box.FindPropertyRelative("rotation");
            var sizeProp = box.FindPropertyRelative("size");

            Handles.matrix = ((Component)targets[0]).transform.localToWorldMatrix;

            centerProp.vector3Value = Handles.PositionHandle(centerProp.vector3Value, Quaternion.identity);
            rotationProp.quaternionValue = Handles.RotationHandle(rotationProp.quaternionValue, centerProp.vector3Value);
            sizeProp.vector3Value = Handles.ScaleHandle(sizeProp.vector3Value, centerProp.vector3Value, 
                rotationProp.quaternionValue, HandleUtility.GetHandleSize(centerProp.vector3Value) * 1.5f);

            serializedObject.ApplyModifiedProperties();
        }

        // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        // ReSharper restore BitwiseOperatorOnEnumWithoutFlags
        public static void DrawGizmoActive(RemoveMeshInBox script, GizmoType gizmoType)
        {
            // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
            var selecting = (gizmoType & (GizmoType.InSelectionHierarchy | GizmoType.Selected)) != 0;
            // ReSharper restore BitwiseOperatorOnEnumWithoutFlags

            var matrixPrev = Handles.matrix;
            var colorPrev = Handles.color;
            try
            {
                Handles.matrix = script.transform.localToWorldMatrix;
                Handles.color = Color.red;

                foreach (var boundingBox in script.boxes)
                {
                    var halfSize = boundingBox.size / 2;
                    var x = boundingBox.rotation * new Vector3(halfSize.x, 0, 0);
                    var y = boundingBox.rotation * new Vector3(0, halfSize.y, 0);
                    var z = boundingBox.rotation * new Vector3(0, 0, halfSize.z);
                    var center = boundingBox.center;

                    var points = new Vector3[8]
                    {
                        center + x + y + z,
                        center + x + y - z,
                        center + x - y + z,
                        center + x - y - z,
                        center - x + y + z,
                        center - x + y - z,
                        center - x - y + z,
                        center - x - y - z,
                    };

                    var indices = new int[12 * 2]
                    {
                        0, 1, 0, 2, 0, 4,
                        3, 1, 3, 2, 3, 7,
                        5, 1, 5, 4, 5, 7,
                        6, 2, 6, 4, 6, 7,
                    };

                    Handles.DrawLines(points, indices);
                }
            }
            finally
            {
                Handles.matrix = matrixPrev;
                Handles.color = colorPrev;
            }
        }
    }
}
