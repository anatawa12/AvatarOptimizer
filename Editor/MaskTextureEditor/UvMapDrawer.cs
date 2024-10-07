using Anatawa12.AvatarOptimizer.EditModePreview;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.MaskTextureEditor
{
    internal class UvMapDrawer : ScriptableObject
    {
        [SerializeField]
        private SkinnedMeshRenderer _renderer = null!; // Initialized by Init

        [SerializeField]
        private int _subMesh = 0;

        private Mesh? _mesh = null;
        private int _meshDirtyCount = 0;
        private Vector2[]? _points = null;
        private Vector3[]? _buffer = null;
        private int[]? _lineIndices = null;
        private int[]? _removedLineIndices = null;

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
                _points == null ||
                _buffer == null ||
                _lineIndices == null ||
                _removedLineIndices == null)
            {
                _mesh = _renderer.sharedMesh;
                _meshDirtyCount = EditorUtility.GetDirtyCount(_mesh);

                CollectPointsAndLineIndices();
                _ = _buffer![0]; // initialized by CollectPointsAndLineIndices
                _ = _points![0]; // initialized by CollectPointsAndLineIndices
            }

            // Draw the main texture if present
            if (_subMesh < _renderer.sharedMaterials.Length &&
                _renderer.sharedMaterials[_subMesh].mainTexture != null)
            {
                EditorGUI.DrawTextureTransparent(rect, _renderer.sharedMaterials[_subMesh].mainTexture);
            }

            // Draw line shadows for visibility
            for (var i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] = Rect.NormalizedToPoint(rect, new Vector2(_points[i].x, 1.0f - _points[i].y));
            }
            Handles.color = GUI.color * Color.gray;
            Handles.DrawLines(_buffer, _lineIndices);
            Handles.color = GUI.color * Color.gray;
            Handles.DrawLines(_buffer, _removedLineIndices);

            // Draw lines with offset
            for (var i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] -= Vector3.one;
            }
            Handles.color = GUI.color * Color.white;
            Handles.DrawLines(_buffer, _lineIndices);
            Handles.color = GUI.color * Color.black;
            Handles.DrawLines(_buffer, _removedLineIndices);
        }

        private void CollectPointsAndLineIndices()
        {
            {
                var lines = CollectLines(_mesh!, _subMesh);
                _points = _mesh!.uv;
                _buffer = new Vector3[_points.Length];
                _lineIndices = FlattenLineIndices(lines);
                _removedLineIndices = new int[0];
            }

            HashSet<(int, int)> CollectLines(Mesh mesh, int subMesh)
            {
                var topology = 0;
                switch (mesh.GetTopology(subMesh))
                {
                    case MeshTopology.Lines:
                        topology = 2;
                        break;
                    case MeshTopology.Triangles:
                        topology = 3;
                        break;
                    case MeshTopology.Quads:
                        topology = 4;
                        break;
                    default:
                        return new HashSet<(int, int)>();
                };
                var lines = new HashSet<(int, int)>();
                var indices = mesh.GetIndices(subMesh);
                for (var i = 0; i < indices.Length; i++)
                {
                    var indexA = indices[i / topology * topology + (i + 0) % topology];
                    var indexB = indices[i / topology * topology + (i + 1) % topology];
                    lines.Add(indexA < indexB ? (indexA, indexB) : (indexB, indexA));
                }
                return lines;
            }

            int[] FlattenLineIndices(HashSet<(int, int)> lines)
            {
                var indices = new List<int>();
                foreach (var (a, b) in lines)
                {
                    indices.Add(a);
                    indices.Add(b);
                }
                return indices.ToArray();
            }
        }
    }
}
