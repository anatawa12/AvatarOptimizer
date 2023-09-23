using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    class RemoveMeshPreviewController : IDisposable
    {
        public static Type[] EditorTypes =
        {
            typeof(RemoveMeshByBlendShape),
            typeof(RemoveMeshInBox),
        };

        public RemoveMeshPreviewController([NotNull] SkinnedMeshRenderer targetRenderer, Mesh originalMesh = null, Mesh previewMesh = null)
        {
            if (targetRenderer == null) throw new ArgumentNullException(nameof(targetRenderer));

            // Previewing object
            TargetGameObject = targetRenderer.gameObject;
            _targetRenderer = new ComponentHolder<SkinnedMeshRenderer>(targetRenderer);

            OriginalMesh = originalMesh ? originalMesh : _targetRenderer.Value.sharedMesh;
            _blendShapePreviewContext = new BlendShapePreviewContext(OriginalMesh);

            _removeMeshInBox = default;
            _removeMeshByBlendShape = default;

            var subMeshes = new SubMeshDescriptor[OriginalMesh.subMeshCount];
            _subMeshTriangleEndIndices = new int[OriginalMesh.subMeshCount];
            var totalTriangles = 0;
            for (var i = 0; i < subMeshes.Length; i++)
            {
                subMeshes[i] = OriginalMesh.GetSubMesh(i);
                totalTriangles += subMeshes[i].indexCount / 3;
                _subMeshTriangleEndIndices[i] = totalTriangles;
            }

            _blendShapeNames = new string[OriginalMesh.blendShapeCount];
            for (var i = 0; i < OriginalMesh.blendShapeCount; i++)
                _blendShapeNames[i] = OriginalMesh.GetBlendShapeName(i);

            var originalTriangles = OriginalMesh.triangles;


            _triangles = new NativeArray<Triangle>(totalTriangles, Allocator.Persistent);
            _indexBuffer = new List<int>();

            var trianglesIndex = 0;
            foreach (var subMeshDescriptor in subMeshes)
            {
                var indexStart = subMeshDescriptor.indexStart;
                for (var i = 0; i < subMeshDescriptor.indexCount / 3; i++)
                {
                    _triangles[trianglesIndex++] = new Triangle(
                        originalTriangles[indexStart + i * 3 + 0],
                        originalTriangles[indexStart + i * 3 + 1],
                        originalTriangles[indexStart + i * 3 + 2],
                        subMeshDescriptor.baseVertex);
                }
            }

            if (previewMesh)
            {
                PreviewMesh = previewMesh;
            }
            else
            {
                PreviewMesh = Object.Instantiate(OriginalMesh);
                PreviewMesh.name = OriginalMesh.name + " (AAO Preview)";
                PreviewMesh.indexFormat = IndexFormat.UInt32;
            }
        }

        public readonly GameObject TargetGameObject;
        public readonly Mesh OriginalMesh;
        public readonly Mesh PreviewMesh;
        public SkinnedMeshRenderer TargetRenderer => _targetRenderer.Value;

        private ComponentHolder<SkinnedMeshRenderer> _targetRenderer;
        private ComponentHolder<RemoveMeshInBox> _removeMeshInBox;
        private ComponentHolder<RemoveMeshByBlendShape> _removeMeshByBlendShape;

        private readonly BlendShapePreviewContext _blendShapePreviewContext;
        private readonly int[] _subMeshTriangleEndIndices;
        private NativeArray<Triangle> _triangles;
        [CanBeNull] private RemoveMeshWithBoxPreviewContext _removeMeshWithBoxPreviewContext;
        [CanBeNull] private RemoveMeshByBlendShapePreviewContext _removeMeshByBlendShapePreviewContext;
        private readonly string[] _blendShapeNames;
        private readonly List<int> _indexBuffer;

        struct Triangle
        {
            public int First;
            public int Second;
            public int Third;

            public Triangle(int first, int second, int third)
            {
                First = first;
                Second = second;
                Third = third;
            }

            public Triangle(int first, int second, int third, int baseIndex) : this(first + baseIndex,
                second + baseIndex, third + baseIndex)
            {
            }
        }

        /// <returns>True if this is no longer valid</returns>
        public bool UpdatePreviewing()
        {
            bool ShouldStopPreview()
            {
                // target GameObject disappears
                if (TargetGameObject == null || _targetRenderer.Value == null) return true;
                // animation mode externally exited
                if (!AnimationMode.InAnimationMode()) return true;
                // Showing Inspector changed
                if (ActiveEditorTracker.sharedTracker.activeEditors[0].target != TargetGameObject) return true;

                return false;
            }

            if (ShouldStopPreview()) return true;

            var modified = false;

            if (_targetRenderer.Update(null) != Changed.Nothing)
            {
                _removeMeshWithBoxPreviewContext?.OnUpdateSkinnedMeshRenderer(_targetRenderer.Value);
                modified = true;
            }

            switch (_removeMeshInBox.Update(TargetGameObject))
            {
                default:
                case Changed.Updated:
                    modified = true;
                    break;
                case Changed.Removed:
                    Debug.Assert(_removeMeshWithBoxPreviewContext != null,
                        nameof(_removeMeshWithBoxPreviewContext) + " != null");
                    _removeMeshWithBoxPreviewContext.Dispose();
                    _removeMeshWithBoxPreviewContext = null;
                    modified = true;
                    break;
                case Changed.Created:
                    Debug.Assert(_removeMeshWithBoxPreviewContext == null,
                        nameof(_removeMeshWithBoxPreviewContext) + " == null");
                    _removeMeshWithBoxPreviewContext =
                        new RemoveMeshWithBoxPreviewContext(_blendShapePreviewContext, OriginalMesh);
                    modified = true;
                    break;
                case Changed.Nothing:
                    break;
            }

            switch (_removeMeshByBlendShape.Update(TargetGameObject))
            {
                default:
                case Changed.Updated:
                    modified = true;
                    break;
                case Changed.Removed:
                    modified = true;
                    break;
                case Changed.Created:
                    if (_removeMeshByBlendShapePreviewContext == null)
                        _removeMeshByBlendShapePreviewContext =
                            new RemoveMeshByBlendShapePreviewContext(_blendShapePreviewContext, OriginalMesh);
                    modified = true;
                    break;
                case Changed.Nothing:
                    break;
            }

            if (modified)
                UpdatePreviewMesh();

            // modifier component not found
            if (!(_removeMeshInBox.Value || _removeMeshByBlendShape.Value)) return false;

            return false;
        }

        private void UpdatePreviewMesh()
        {
            var removeBlendShapeIndicesList = new List<int>();
            if (_removeMeshByBlendShape.Value)
            {
                var blendShapes = _removeMeshByBlendShape.Value.RemovingShapeKeys;

                for (var i = 0; i < _blendShapeNames.Length; i++)
                    if (blendShapes.Contains(_blendShapeNames[i]))
                        removeBlendShapeIndicesList.Add(i);
            }

            using (var flags = new NativeArray<bool>(_triangles.Length, Allocator.TempJob))
            {
                using (var boxes = new NativeArray<RemoveMeshInBox.BoundingBox>(
                           _removeMeshInBox.Value != null
                               ? _removeMeshInBox.Value.boxes
                               : Array.Empty<RemoveMeshInBox.BoundingBox>(), Allocator.TempJob))
                using (var blendShapeIndices =
                       new NativeArray<int>(removeBlendShapeIndicesList.ToArray(), Allocator.TempJob))
                using (var empty = new NativeArray<Vector3>(0, Allocator.TempJob))
                {
                    var blendShapeAppliedVertices = _removeMeshWithBoxPreviewContext?.Vertices ?? empty;
                    var blendShapeMovements = _removeMeshByBlendShapePreviewContext?.BlendShapeMovements ?? empty;
                    var tolerance =
                        (float)(_removeMeshByBlendShape.Value ? _removeMeshByBlendShape.Value.tolerance : 0);

                    new FlagTrianglesJob
                    {
                        Triangles = _triangles,
                        RemoveFlags = flags,
                        VertexCount = OriginalMesh.vertexCount,

                        Boxes = boxes,
                        BlendShapeAppliedVertices = blendShapeAppliedVertices,

                        BlendShapeIndices = blendShapeIndices,
                        ToleranceSquared = tolerance * tolerance,
                        BlendShapeMovements = blendShapeMovements,
                    }.Schedule(_triangles.Length, 1).Complete();
                }

                var subMeshIdx = 0;

                _indexBuffer.Clear();

                for (var triIdx = 0; triIdx < _triangles.Length; triIdx++)
                {
                    if (!flags[triIdx])
                    {
                        _indexBuffer.Add(_triangles[triIdx].First);
                        _indexBuffer.Add(_triangles[triIdx].Second);
                        _indexBuffer.Add(_triangles[triIdx].Third);
                    }

                    while (subMeshIdx < _subMeshTriangleEndIndices.Length &&
                           triIdx + 1 == _subMeshTriangleEndIndices[subMeshIdx])
                    {
                        PreviewMesh.SetTriangles(_indexBuffer, subMeshIdx);
                        _indexBuffer.Clear();
                        subMeshIdx++;
                    }
                }
            }
        }

        [BurstCompile]
        struct FlagTrianglesJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Triangle> Triangles;
            public int VertexCount;
            public NativeArray<bool> RemoveFlags;

            // Remove Mesh in Box
            [ReadOnly]
            public NativeArray<RemoveMeshInBox.BoundingBox> Boxes;
            [ReadOnly]
            public NativeArray<Vector3> BlendShapeAppliedVertices;

            // Remove Mesh by BlendShape
            [ReadOnly]
            public NativeArray<int> BlendShapeIndices;
            [ReadOnly]
            public NativeArray<Vector3> BlendShapeMovements;

            public float ToleranceSquared { get; set; }

            public void Execute(int index) => RemoveFlags[index] = TestTriangle(Triangles[index]);

            private bool TestTriangle(Triangle triangle)
            {
                // return true if remove
                if (BlendShapeIndices.Length != 0)
                {
                    // for RemoveMesh by BlendShape, *any* of vertex is moved, remove the triangle
                    foreach (var blendShapeIndex in BlendShapeIndices)
                    {
                        var movementBase = blendShapeIndex * VertexCount;

                        if (TestBlendShape(movementBase, triangle.First)) return true;
                        if (TestBlendShape(movementBase, triangle.Second)) return true;
                        if (TestBlendShape(movementBase, triangle.Third)) return true;
                    }
                }

                if (Boxes.Length != 0)
                {
                    foreach (var boundingBox in Boxes)
                    {
                        if (boundingBox.ContainsVertex(BlendShapeAppliedVertices[triangle.First])
                            && boundingBox.ContainsVertex(BlendShapeAppliedVertices[triangle.First])
                            && boundingBox.ContainsVertex(BlendShapeAppliedVertices[triangle.First]))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private bool TestBlendShape(int movementBase, int index) =>
                BlendShapeMovements[movementBase + index].sqrMagnitude > ToleranceSquared;
        }

        public void Dispose()
        {
            _triangles.Dispose();
            _removeMeshWithBoxPreviewContext?.Dispose();
            _removeMeshByBlendShapePreviewContext?.Dispose();
            _blendShapePreviewContext?.Dispose();
        }

        public struct ComponentHolder<T> where T : Component
        {
            public T Value => _value;
            // preview version + 1 to make default value != EditorUtility.GetDirtyCount(new object)
            private int _previousVersion;
            private T _value;

            public ComponentHolder(T value)
            {
                _value = value;
                if (value) _previousVersion = EditorUtility.GetDirtyCount(value) + 1;
                else _previousVersion = 0;
            }

            public Changed Update(GameObject gameObject)
            {
                if (!_value)
                {
                    _value = gameObject ? gameObject.GetComponent<T>() : null;
                    if (_value)
                    {
                        // newly created
                        _previousVersion = EditorUtility.GetDirtyCount(_value) + 1;
                        return Changed.Created;
                    }
                    else
                    {
                        if (_previousVersion == 0) return Changed.Nothing;

                        // it seem component is removed
                        _previousVersion = 0;
                        return Changed.Removed;
                    }
                }
                else
                {
                    var currentVersion = EditorUtility.GetDirtyCount(_value) + 1;
                    if (_previousVersion == currentVersion)
                        return Changed.Nothing;

                    _previousVersion = currentVersion;
                    return Changed.Updated;
                }

            }
        }

        internal enum Changed
        {
            Nothing,
            Updated,
            Removed,
            Created,
        }
    }
}