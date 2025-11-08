using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(RemoveMeshInBox))]
    internal class RemoveMeshInBoxEditor : AvatarTagComponentEditorBase
    {
        private SerializedProperty _boxes = null!; // Initialized in OnEnable
        private SerializedProperty _removeInBox = null!; // Initialized in OnEnable
        private string? _editingBoxPropPath;

        private readonly Dictionary<string, (Quaternion value, Vector3 euler)> _eulerAngles =
            new Dictionary<string, (Quaternion value, Vector3 euler)>();

        private void OnEnable()
        {
            _boxes = serializedObject.FindProperty(nameof(RemoveMeshInBox.boxes));
            _removeInBox = serializedObject.FindProperty(nameof(RemoveMeshInBox.removeInBox));
        }

        private void OnDisable()
        {
            RestoreToolState();
        }

        protected override void OnInspectorGUIInner()
        {
            GenericEditSkinnedMeshComponentsEditor.DrawUnexpectedRendererError(targets);

            // remove in box
            {
                var labelContent = new GUIContent(AAOL10N.Tr("RemoveMeshInBox:prop:removePolygonsInOrOut"));
                var inBoxContent = new GUIContent(AAOL10N.Tr("RemoveMeshInBox:prop:removePolygonsInOrOut:inBox"));
                var outOfBoxContent = new GUIContent(AAOL10N.Tr("RemoveMeshInBox:prop:removePolygonsInOrOut:outOfBox"));

                var removeInBoxRect = EditorGUILayout.GetControlRect();
                labelContent = EditorGUI.BeginProperty(removeInBoxRect, labelContent, _removeInBox);
                var popup = EditorGUI.Popup(removeInBoxRect, labelContent, _removeInBox.boolValue ? 0 : 1,
                    new[] { inBoxContent, outOfBoxContent });
                _removeInBox.boolValue = popup == 0;
                EditorGUI.EndProperty();
            }

            // size prop
            _boxes.isExpanded = true;
            using (new BoundingBoxEditor.EditorScope(this))
                EditorGUILayout.PropertyField(_boxes);

            serializedObject.ApplyModifiedProperties();
        }

        [CustomPropertyDrawer(typeof(RemoveMeshInBox.BoundingBox))]
        class BoundingBoxEditor : PropertyDrawer
        {
            private static RemoveMeshInBoxEditor? _upstreamEditor;

            public readonly struct EditorScope : IDisposable
            {
                private readonly RemoveMeshInBoxEditor? _oldEditor;

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
                var vector3Height =
                    EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, new GUIContent("center"));
                return EditorGUIUtility.singleLineHeight // header
                       + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight //edit this box
                       + EditorGUIUtility.standardVerticalSpacing + vector3Height // center
                       + EditorGUIUtility.standardVerticalSpacing + vector3Height // size
                       + EditorGUIUtility.standardVerticalSpacing + vector3Height // rotation
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
                    var vector3Height =
                        EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, new GUIContent("center"));

                    using (new GUILayout.HorizontalScope())
                    {
                        if (_upstreamEditor != null)
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
                    position.y += EditorGUIUtility.standardVerticalSpacing + vector3Height;
                    EditorGUI.PropertyField(position, sizeProp);
                    position.y += EditorGUIUtility.standardVerticalSpacing + vector3Height;

                    if (_upstreamEditor == null ||
                        !_upstreamEditor._eulerAngles.TryGetValue(property.propertyPath, out var eulerCache) ||
                        eulerCache.value != rotationProp.quaternionValue)
                    {
                        eulerCache = (value: rotationProp.quaternionValue,
                            euler: rotationProp.quaternionValue.eulerAngles);
                    }

                    // rotation in euler
                    var rotationLabel = new GUIContent(AAOL10N.Tr("RemoveMeshInBox:BoundingBox:prop:rotation"));
                    rotationLabel = EditorGUI.BeginProperty(position, rotationLabel, rotationProp);
                    EditorGUI.BeginChangeCheck();
                    var euler = EditorGUI.Vector3Field(position, rotationLabel, eulerCache.euler);
                    if (EditorGUI.EndChangeCheck())
                    {
                        var quot = Quaternion.Euler(euler);
                        rotationProp.quaternionValue = quot;
                        eulerCache = (quot, euler);
                    }

                    if (_upstreamEditor != null)
                        _upstreamEditor._eulerAngles[property.propertyPath] = eulerCache;

                    EditorGUI.EndProperty();
                    position.y += EditorGUIUtility.standardVerticalSpacing + vector3Height;
                }
            }
        }

        private readonly BoxHandle _boxBoundsHandle = new BoxHandle();
        private string? _editingBoxPropPathPrevFrame;

        private void RestoreToolState()
        {
            if (_editingBoxPropPathPrevFrame != null)
            {
                // tools are not generally hidden in unity so we simply revert to show state instead of saving and restoring
                // This prevents the tools from being hidden forever because of bug in the editor script.
                Tools.hidden = false;
            }
        }

        private void OnSceneGUI()
        {
            if (_editingBoxPropPathPrevFrame != _editingBoxPropPath)
            {
                if (_editingBoxPropPath != null)
                {
                    if (_editingBoxPropPathPrevFrame == null)
                    {
                        Tools.hidden = true;
                    }
                }
                else
                {
                    RestoreToolState();
                }
            }
            _editingBoxPropPathPrevFrame = _editingBoxPropPath;
            if (_editingBoxPropPath == null) return;
            var box = serializedObject.FindProperty(_editingBoxPropPath);
            if (box == null) return;

            var centerProp = box.FindPropertyRelative("center");
            var rotationProp = box.FindPropertyRelative("rotation");
            var sizeProp = box.FindPropertyRelative("size");

            var transform = ((Component)targets[0]).transform;
            var transformRotation = transform.rotation;
            var transformLossyScale = transform.lossyScale;

            BoxHandle();
            TransformHandle();

            serializedObject.ApplyModifiedProperties();

            return;

            void TransformHandle()
            {
                EditorGUI.BeginChangeCheck();

                var globalRotation = transformRotation * rotationProp.quaternionValue;
                var globalRotationNew = globalRotation;

                var globalPosition = transform.TransformPoint(centerProp.vector3Value);
                var globalPositionNew = globalPosition;

                Handles.TransformHandle(ref globalPositionNew, ref globalRotationNew);

                if (EditorGUI.EndChangeCheck())
                {
                    if (globalPosition != globalPositionNew)
                    {
                        // something like TransformManipulator.SetPositionDelta(Vector3,Vector3)
                        var localDelta = transform.InverseTransformPoint(globalPositionNew) - centerProp.vector3Value;
                        //var localDelta = transform.InverseTransformDirection(delta);
                        var expected = centerProp.vector3Value + localDelta;
                        SetPositionWithLocalDelta(localDelta, globalPosition);
                    }

                    var deltaRotation = Quaternion.Inverse(globalRotation) * globalRotationNew;
                    deltaRotation.ToAngleAxis(out var angle, out _);
                    if (!Mathf.Approximately(angle, 0))
                    {
                        rotationProp.quaternionValue =
                            Quaternion.Normalize(Quaternion.Inverse(transformRotation) * globalRotationNew);
                    }
                }
            }

            void BoxHandle()
            {
                var boxMatrix = transform.localToWorldMatrix;
                boxMatrix *= Matrix4x4.TRS(centerProp.vector3Value, rotationProp.quaternionValue, Vector3.one);

                using (new Handles.DrawingScope(Color.red, boxMatrix))
                {
                    _boxBoundsHandle.center = Vector3.zero;
                    _boxBoundsHandle.size = sizeProp.vector3Value;
                    EditorGUI.BeginChangeCheck();
                    _boxBoundsHandle.DrawHandle();
                    if (EditorGUI.EndChangeCheck())
                    {
                        sizeProp.vector3Value = _boxBoundsHandle.size;
                        // _boxBoundsHandle.center is delta in rotated space.
                        SetPositionWithLocalDelta(rotationProp.quaternionValue * _boxBoundsHandle.center, null);
                    }
                }
            }

            float RoundBasedOnMinimumDifference(float valueToRound, float minDifference)
            {
                return minDifference == 0f
                    ? DiscardLeastSignificantDecimal(valueToRound)
                    : (float)Math.Round(valueToRound, GetNumberOfDecimalsForMinimumDifference(minDifference),
                        MidpointRounding.AwayFromZero);
            }

            float DiscardLeastSignificantDecimal(float v)
            {
                int digits = Mathf.Clamp((int)(5.0 - Mathf.Log10(Mathf.Abs(v))), 0, 15);
                return (float)Math.Round(v, digits, MidpointRounding.AwayFromZero);
            }

            int GetNumberOfDecimalsForMinimumDifference(float minDifference)
            {
                return Mathf.Clamp(-Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(minDifference))), 0, 15);
            }

            void SetPositionWithLocalDelta(Vector3 localDelta, Vector3? globalPosition)
            {
                var oldLocalPosition = centerProp.vector3Value;
                var newLocalPosition = oldLocalPosition + localDelta;

                if (globalPosition is {} globalPositionValue)
                {
                    var minDragDifference = Vector3.one * (HandleUtility.GetHandleSize(globalPositionValue) / 80f);
                    minDragDifference.x /= transformLossyScale.x;
                    minDragDifference.y /= transformLossyScale.y;
                    minDragDifference.z /= transformLossyScale.z;

                    newLocalPosition.x = RoundBasedOnMinimumDifference(newLocalPosition.x, minDragDifference.x);
                    newLocalPosition.y = RoundBasedOnMinimumDifference(newLocalPosition.y, minDragDifference.y);
                    newLocalPosition.z = RoundBasedOnMinimumDifference(newLocalPosition.z, minDragDifference.z);
                }

                newLocalPosition.x = Mathf.Approximately(localDelta.x, 0) ? oldLocalPosition.x : newLocalPosition.x;
                newLocalPosition.y = Mathf.Approximately(localDelta.y, 0) ? oldLocalPosition.y : newLocalPosition.y;
                newLocalPosition.z = Mathf.Approximately(localDelta.z, 0) ? oldLocalPosition.z : newLocalPosition.z;

                centerProp.vector3Value = newLocalPosition;
            }
        }

        class BoxHandle : BoxBoundsHandle
        {
            protected override void DrawWireframe()
            {
                // no-op
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
