using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.MaskTextureEditor
{
    internal class UvMapDrawer : ScriptableObject
    {
        [SerializeField]
        private SkinnedMeshRenderer _renderer = null;

        [SerializeField]
        private int _subMesh = 0;

        private Mesh _mesh = null;
        private int _meshDirtyCount = 0;
        private (Vector3 PointA, Vector3 PointB)[] _lines = null;
        private Vector3[] _points = null;
        private int[] _indices = null;

        private void Awake()
        {
            // Set HideFlags to keep the state when reloading a scene or domain
            hideFlags = HideFlags.HideAndDontSave;
        }

        public void Init(SkinnedMeshRenderer renderer, int subMesh)
        {
            _renderer = renderer;
            _subMesh = subMesh;
        }

        public void Draw(Rect rect)
        {
            if (_mesh != _renderer.sharedMesh ||
                _meshDirtyCount != EditorUtility.GetDirtyCount(_mesh) ||
                _lines == null ||
                _points == null ||
                _indices == null)
            {
                _mesh = _renderer.sharedMesh;
                _meshDirtyCount = EditorUtility.GetDirtyCount(_mesh);
                _lines = CollectLines();
                _points = new Vector3[_lines.Length * 2];
                _indices = new int[_lines.Length * 2];
            }

            // Draw the main texture if present
            if (_subMesh < _renderer.sharedMaterials.Length &&
                _renderer.sharedMaterials[_subMesh].mainTexture != null)
            {
                EditorGUI.DrawTextureTransparent(rect, _renderer.sharedMaterials[_subMesh].mainTexture);
            }

            // Draw background lines for visibility
            for (var i = 0; i < _lines.Length; i++)
            {
                var indexA = i * 2 + 0;
                var indexB = i * 2 + 1;
                var pointA = new Vector2(_lines[i].PointA.x, 1.0f - _lines[i].PointA.y);
                var pointB = new Vector2(_lines[i].PointB.x, 1.0f - _lines[i].PointB.y);
                _indices[indexA] = indexA;
                _indices[indexB] = indexB;
                _points[indexA] = Rect.NormalizedToPoint(rect, pointA);
                _points[indexB] = Rect.NormalizedToPoint(rect, pointB);
            }
            Handles.color = GUI.color * Color.gray;
            Handles.DrawLines(_points, _indices);

            // Draw foreground lines with offset
            for (var i = 0; i < _points.Length; i++)
            {
                _points[i] -= (Vector3)Vector2.one;
            }
            Handles.color = GUI.color * Color.white;
            Handles.DrawLines(_points, _indices);
        }

        private (Vector3 PointA, Vector3 PointB)[] CollectLines()
        {
            switch (_mesh.GetTopology(_subMesh))
            {
                case MeshTopology.Lines:
                    return CollectLines(2);
                case MeshTopology.Triangles:
                    return CollectLines(3);
                case MeshTopology.Quads:
                    return CollectLines(4);
                default:
                    return new (Vector3, Vector3)[0];
            };

            (Vector3 PointA, Vector3 PointB)[] CollectLines(int topology)
            {
                var lines = new HashSet<(Vector3, Vector3)>();
                var points = _mesh.uv;
                var indices = _mesh.GetIndices(_subMesh);
                for (var i = 0; i < indices.Length; i++)
                {
                    var pointA = points[indices[i / topology * topology + (i + 0) % topology]];
                    var pointB = points[indices[i / topology * topology + (i + 1) % topology]];
                    if (!lines.Contains((pointA, pointB)) && !lines.Contains((pointB, pointA)))
                    {
                        lines.Add((pointA, pointB));
                    }
                }
                return lines.ToArray();
            }
        }
    }
}
