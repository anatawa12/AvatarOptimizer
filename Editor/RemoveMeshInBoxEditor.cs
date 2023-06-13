using System;
using System.Collections.Generic;
using CustomLocalization4EditorExtension;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(RemoveMeshInBox))]
    internal class RemoveMeshInBoxEditor : AvatarTagComponentEditorBase
    {
        private SerializedProperty _boxes;
        private string _editingBoxPropPath;
        private readonly Dictionary<string, (Quaternion value, Vector3 euler)> _eulerAngles =
            new Dictionary<string, (Quaternion value, Vector3 euler)>();

        private void OnEnable()
        {
            _boxes = serializedObject.FindProperty(nameof(RemoveMeshInBox.boxes));
        }

        protected override void OnInspectorGUIInner()
        {
            // size prop
            _boxes.isExpanded = true;
            using (new BoundingBoxEditor.EditorScope(this))
                EditorGUILayout.PropertyField(_boxes);

            serializedObject.ApplyModifiedProperties();
        }

        [CustomPropertyDrawer(typeof(RemoveMeshInBox.BoundingBox))]
        class BoundingBoxEditor : PropertyDrawer
        {
            [CanBeNull] private static RemoveMeshInBoxEditor _upstreamEditor;

            public readonly struct EditorScope : IDisposable
            {
                private readonly RemoveMeshInBoxEditor _oldEditor;

                public EditorScope(RemoveMeshInBoxEditor editor)
                {
                    _oldEditor = _upstreamEditor;
                    _upstreamEditor = editor;
                }

                public void Dispose()
                {
                    _upstreamEditor = _oldEditor;
                }
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight // header
                       + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight // center
                       + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight // size
                       + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight // rotation
                       + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight //edit this box
                    ;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                position.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(position, label);

                position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;

                var centerProp = property.FindPropertyRelative("center");
                var sizeProp = property.FindPropertyRelative("size");
                var rotationProp = property.FindPropertyRelative("rotation");

                using (new EditorGUI.IndentLevelScope())
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        if (_upstreamEditor)
                        {
                            var editingCurrent = _upstreamEditor._editingBoxPropPath == property.propertyPath;
                            if (GUI.Button(position, editingCurrent ? "Finish Editing Box" : "Edit This Box"))
                            {
                                _upstreamEditor._editingBoxPropPath = editingCurrent ? null : property.propertyPath;
                                SceneView.RepaintAll(); // to show/hide gizmo
                            }
                        }
                        else
                        {
                            using (new EditorGUI.DisabledScope(true))
                                GUI.Button(position, "Cannot edit in this context");
                        }

                        position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                    }

                    EditorGUI.PropertyField(position, centerProp);
                    position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(position, sizeProp);
                    position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;

                    if (!_upstreamEditor ||
                        !_upstreamEditor._eulerAngles.TryGetValue(property.propertyPath, out var eulerCache) ||
                        eulerCache.value != rotationProp.quaternionValue)
                    {
                        eulerCache = (value: rotationProp.quaternionValue,
                            euler: rotationProp.quaternionValue.eulerAngles);
                    }

                    // rotation in euler
                    var rotationLabel = new GUIContent(CL4EE.Tr("RemoveMeshInBox:BoundingBox:prop:rotation"));
                    rotationLabel = EditorGUI.BeginProperty(position, rotationLabel, rotationProp);
                    EditorGUI.BeginChangeCheck();
                    var euler = EditorGUI.Vector3Field(position, rotationLabel, eulerCache.euler);
                    if (EditorGUI.EndChangeCheck())
                    {
                        var quot = Quaternion.Euler(euler);
                        rotationProp.quaternionValue = quot;
                        eulerCache = (quot, euler);
                    }

                    if (_upstreamEditor)
                        _upstreamEditor._eulerAngles[property.propertyPath] = eulerCache;

                    EditorGUI.EndProperty();
                    position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                }
            }
        }

        private void OnSceneGUI()
        {
            if (_editingBoxPropPath == null) return;
            var box = serializedObject.FindProperty(_editingBoxPropPath);
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
