using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.PrefabSafeList;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(RemoveMeshInBox))]
    internal class RemoveMeshInBoxEditor : Editor
    {
        private readonly SaveVersionDrawer _saveVersion = new SaveVersionDrawer();
        private ListEditor _boxList;
        private int _editingBox = -1;

        private void OnEnable()
        {
            var nestCount = PrefabSafeListUtil.PrefabNestCount(serializedObject.targetObject);
            _boxList = new ListEditor(serializedObject.FindProperty("boxList"), nestCount);
        }

        public override void OnInspectorGUI()
        {
            if (targets.Length != 1)
            {
                base.OnInspectorGUI();
                return;
            }
           
            _saveVersion.Draw(serializedObject);

            var rect = EditorGUILayout.GetControlRect(true, _boxList.GetPropertyHeight());
            _boxList.OnGUI(rect);

            serializedObject.ApplyModifiedProperties();
        }

        private class ListEditor : EditorBase
        {
            public string EditingBoxPropPath;

            private readonly Dictionary<string, (Quaternion value, Vector3 euler)> _eulerAngle =
                new Dictionary<string, (Quaternion value, Vector3 euler)>();

            public ListEditor(SerializedProperty property, int nestCount) : base(property, nestCount)
            {
            }

            protected override float FieldHeight(SerializedProperty serializedProperty)
            {
                return EditorGUIUtility.singleLineHeight // header
                       + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight // center
                       + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight // size
                       + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight // rotation
                       + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight //edit this box
                    ;
            }

            protected override bool Field(Rect position, GUIContent label, SerializedProperty boxProp)
            {
                var removeElement = false;

                position.height = EditorGUIUtility.singleLineHeight;
                var prevWidth = position.width;
                var prevX = position.x;
                position.width -= EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(position, label);
                position.x += position.width + EditorGUIUtility.standardVerticalSpacing;
                position.width = EditorGUIUtility.singleLineHeight;

                if (GUI.Button(position, EditorStatics.RemoveButton))
                    removeElement = true;

                position.width = prevWidth;
                position.x = prevX;

                position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;

                var centerProp = boxProp.FindPropertyRelative("center");
                var sizeProp = boxProp.FindPropertyRelative("size");
                var rotationProp = boxProp.FindPropertyRelative("rotation");

                using (new EditorGUI.IndentLevelScope())
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        var editingCurrent = EditingBoxPropPath == boxProp.propertyPath;
                        if (GUI.Button(position, editingCurrent ? "Finish Editing Box" : "Edit This Box"))
                        {
                            EditingBoxPropPath = editingCurrent ? null : boxProp.propertyPath;
                            SceneView.RepaintAll(); // to show/hide gizmo
                        }
                        position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                    }

                    EditorGUI.PropertyField(position, centerProp);
                    position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(position, sizeProp);
                    position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                    if (!_eulerAngle.TryGetValue(boxProp.propertyPath, out var eulerCache)
                        || eulerCache.value != rotationProp.quaternionValue)
                    {
                        _eulerAngle[boxProp.propertyPath] = eulerCache = (rotationProp.quaternionValue,
                            rotationProp.quaternionValue.eulerAngles);
                    }

                    // rotation in euler
                    var rotationLabel = new GUIContent("Rotation");
                    rotationLabel = EditorGUI.BeginProperty(position, rotationLabel, rotationProp);
                    EditorGUI.BeginChangeCheck();
                    var euler = EditorGUI.Vector3Field(position, rotationLabel, eulerCache.euler);
                    if (EditorGUI.EndChangeCheck())
                    {
                        var quot = Quaternion.Euler(euler);
                        rotationProp.quaternionValue = quot;
                        _eulerAngle[boxProp.propertyPath] = (quot, euler);
                    }
                    EditorGUI.EndProperty();
                    position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                }

                return removeElement;
            }

            protected override void InitializeNewElement(IElement element)
            {
                var box = element.ValueProperty;
                box.FindPropertyRelative("center").vector3Value = Vector3.zero;
                box.FindPropertyRelative("size").vector3Value = Vector3.one;
                box.FindPropertyRelative("rotation").quaternionValue = Quaternion.identity;
                EditingBoxPropPath = element.ValueProperty.propertyPath;
            }

            static class EditorStatics
            {
                public static readonly GUIContent RemoveButton = new GUIContent("x")
                {
                    tooltip = "Remove Element from the list."
                };
            }
        }

        private void OnSceneGUI()
        {
            if (_boxList.EditingBoxPropPath == null) return;
            var box = serializedObject.FindProperty(_boxList.EditingBoxPropPath);
            if (box == null) return;

            var centerProp = box.FindPropertyRelative("center");
            var rotationProp = box.FindPropertyRelative("rotation");
            var sizeProp = box.FindPropertyRelative("size");

            Handles.matrix = ((Component)targets[0]).transform.localToWorldMatrix;

            centerProp.vector3Value = Handles.PositionHandle(centerProp.vector3Value, Quaternion.identity);
            rotationProp.quaternionValue =
                Handles.RotationHandle(rotationProp.quaternionValue, centerProp.vector3Value);

            var size = sizeProp.vector3Value;
            var center = centerProp.vector3Value;
            var halfSize = size / 2;
            var x = rotationProp.quaternionValue * new Vector3(halfSize.x, 0, 0);
            var y = rotationProp.quaternionValue * new Vector3(0, halfSize.y, 0);
            var z = rotationProp.quaternionValue * new Vector3(0, 0, halfSize.z);

            BoxFaceSlider(ref center, ref size.x, x);
            BoxFaceSlider(ref center, ref size.x, -x);
            BoxFaceSlider(ref center, ref size.y, y);
            BoxFaceSlider(ref center, ref size.y, -y);
            BoxFaceSlider(ref center, ref size.z, z);
            BoxFaceSlider(ref center, ref size.z, -z);

            sizeProp.vector3Value = size;
            centerProp.vector3Value = center;

            serializedObject.ApplyModifiedProperties();
        }

        private void BoxFaceSlider(ref Vector3 center, ref float size, Vector3 directionInWorld)
        {
            var prev = center + directionInWorld;
            var newer = Handles.Slider(prev, directionInWorld,
                HandleUtility.GetHandleSize(prev) / 3, Handles.CubeHandleCap, -1f);

            if (prev != newer)
            {
                var opposite = center - directionInWorld;
                size = (opposite - newer).magnitude;
                center = (opposite + newer) / 2;
            }
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

                foreach (var boundingBox in script.boxList.GetAsList())
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
