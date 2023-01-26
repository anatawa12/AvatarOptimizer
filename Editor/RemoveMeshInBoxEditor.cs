using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(RemoveMeshInBox))]
    internal class RemoveMeshInBoxEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // TODO: implement custom editor
            base.OnInspectorGUI();
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
