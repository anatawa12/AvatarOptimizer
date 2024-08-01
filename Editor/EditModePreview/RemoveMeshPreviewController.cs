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
            typeof(RemoveMeshByMask),
        };

        public RemoveMeshPreviewController([NotNull] SkinnedMeshRenderer targetRenderer, Mesh originalMesh = null, Mesh previewMesh = null)
        {
            if (targetRenderer == null) throw new ArgumentNullException(nameof(targetRenderer));

            // Previewing object
            TargetGameObject = targetRenderer.gameObject;
            _targetRenderer = new ComponentHolder<SkinnedMeshRenderer>(targetRenderer);
            _rendererTransform = new ComponentHolder<Transform>(targetRenderer.transform);

            OriginalMesh = originalMesh ? originalMesh : _targetRenderer.Value.sharedMesh;
            _blendShapePreviewContext = new BlendShapePreviewContext(OriginalMesh);

            _boneTransforms = new ComponentHolder<Transform>[OriginalMesh.bindposes.Length];
            for (var i = 0; i < _boneTransforms.Length && i < targetRenderer.bones.Length; i++)
                _boneTransforms[i] = new ComponentHolder<Transform>(targetRenderer.bones[i]);

            _removeMeshInBox = default;
            _removeMeshByBlendShape = default;
            _removeMeshByMask = default;
            _maskTextureEditorWindowPreviewTextureInstanceId = default;

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

            _uv = new NativeArray<Vector2>(OriginalMesh.uv, Allocator.Persistent);

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
        private ComponentHolder<Transform> _rendererTransform;
        private ComponentHolder<Transform>[] _boneTransforms;
        private ComponentHolder<RemoveMeshInBox> _removeMeshInBox;
        private ComponentHolder<RemoveMeshByBlendShape> _removeMeshByBlendShape;
        private ComponentHolder<RemoveMeshByMask> _removeMeshByMask;
        private int _maskTextureEditorWindowPreviewTextureInstanceId = default;

        private readonly BlendShapePreviewContext _blendShapePreviewContext;
        private readonly int[] _subMeshTriangleEndIndices;
        private NativeArray<Triangle> _triangles;
        private NativeArray<Vector2> _uv;
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

            var transformUpdated = _rendererTransform.Update(null) != Changed.Nothing;
            for (var i = 0; i < _boneTransforms.Length; i++)
                if (_boneTransforms[i].Update(null) != Changed.Nothing)
                    transformUpdated = true;
            if (transformUpdated)
            {
                _removeMeshWithBoxPreviewContext?.OnUpdateBones(_targetRenderer.Value);
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
                    _removeMeshWithBoxPreviewContext?.OnUpdateSkinnedMeshRenderer(_targetRenderer.Value);
                    _removeMeshWithBoxPreviewContext?.OnUpdateBones(_targetRenderer.Value);
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

            switch (_removeMeshByMask.Update(TargetGameObject))
            {
                default:
                case Changed.Updated:
                    modified = true;
                    break;
                case Changed.Removed:
                    modified = true;
                    break;
                case Changed.Created:
                    // TODO
                    //if (_removeMeshByBlendShapePreviewContext == null)
                    //    _removeMeshByBlendShapePreviewContext =
                    //        new RemoveMeshByBlendShapePreviewContext(_blendShapePreviewContext, OriginalMesh);
                    modified = true;
                    break;
                case Changed.Nothing:
                    break;
            }

            if (_removeMeshByMask.Value != null && MaskTextureEditor.Window.IsOpen(_targetRenderer.Value))
            {
                var instanceId = MaskTextureEditor.Window.Instance.PreviewTexture.GetInstanceID();
                if (_maskTextureEditorWindowPreviewTextureInstanceId != instanceId)
                {
                    _maskTextureEditorWindowPreviewTextureInstanceId = instanceId;
                    modified = true;
                }
            }
            else if (_maskTextureEditorWindowPreviewTextureInstanceId != default)
            {
                _maskTextureEditorWindowPreviewTextureInstanceId = default;
                modified = true;
            }

            if (modified)
                UpdatePreviewMesh();

            // modifier component not found
            if (!(_removeMeshInBox.Value || _removeMeshByBlendShape.Value || _removeMeshByMask.Value)) return false;

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

                    PreviewMesh.subMeshCount = _subMeshTriangleEndIndices.Length;
                    var triIdx = 0;

                    for (var subMeshIdx = 0; subMeshIdx < _subMeshTriangleEndIndices.Length; subMeshIdx++)
                    {
                        _indexBuffer.Clear();

                        var maskData = Array.Empty<Color32>();
                        var maskMode = MaskMode.Disabled;
                        var maskWidth = 0;
                        var maskHeight = 0;
                        if (_removeMeshByMask.Value)
                        {
                            var materials = _removeMeshByMask.Value.materials;
                            if (subMeshIdx < materials.Length)
                            {
                                var submeshInfo = materials[subMeshIdx];
                                if (submeshInfo.enabled)
                                {
                                    var maskTexture = MaskTextureEditor.Window.IsOpen(_targetRenderer.Value, subMeshIdx)
                                        ? MaskTextureEditor.Window.Instance.PreviewTexture
                                        : submeshInfo.mask;
                                    var mode = submeshInfo.mode;
                                    if (maskTexture != null && maskTexture.isReadable)
                                    {
                                        maskData = maskTexture.GetPixels32();
                                        maskWidth = maskTexture.width;
                                        maskHeight = maskTexture.height;
                                        maskMode = (MaskMode)mode;
                                    }
                                }
                            }
                        }

                        var triStart = triIdx;
                        var triEnd = _subMeshTriangleEndIndices[subMeshIdx];
                        var triLen = triEnd - triStart;

                        using (var maskDataArray = new NativeArray<Color32>(maskData, Allocator.TempJob))
                        {
                            new FlagTrianglesJob
                            {
                                Triangles = _triangles.Slice(triStart, triLen),
                                RemoveFlags = flags.Slice(triStart, triLen),
                                VertexCount = OriginalMesh.vertexCount,

                                Boxes = boxes,
                                BlendShapeAppliedVertices = blendShapeAppliedVertices,

                                BlendShapeIndices = blendShapeIndices,
                                ToleranceSquared = tolerance * tolerance,
                                BlendShapeMovements = blendShapeMovements,

                                UV = _uv,
                                Mask = maskDataArray,
                                MaskWidth = maskWidth,
                                MaskHeight = maskHeight,
                                MaskMode = maskMode,
                            }.Schedule(triLen, 1).Complete();
                        }

                        for (; triIdx < _subMeshTriangleEndIndices[subMeshIdx]; triIdx++)
                        {
                            if (!flags[triIdx])
                            {
                                _indexBuffer.Add(_triangles[triIdx].First);
                                _indexBuffer.Add(_triangles[triIdx].Second);
                                _indexBuffer.Add(_triangles[triIdx].Third);
                            }
                        }

                        PreviewMesh.SetTriangles(_indexBuffer, subMeshIdx);
                    }

                    _indexBuffer.Clear();
                }
            }
        }

        [BurstCompile]
        struct FlagTrianglesJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeSlice<Triangle> Triangles;
            public int VertexCount;
            public NativeSlice<bool> RemoveFlags;

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

            // Remove Mesh by Mask
            [ReadOnly]
            public NativeArray<Vector2> UV;
            [ReadOnly]
            public NativeArray<Color32> Mask;
            public int MaskWidth;
            public int MaskHeight;
            public MaskMode MaskMode;

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
                            && boundingBox.ContainsVertex(BlendShapeAppliedVertices[triangle.Second])
                            && boundingBox.ContainsVertex(BlendShapeAppliedVertices[triangle.Third]))
                        {
                            return true;
                        }
                    }
                }

                switch (MaskMode)
                {
                    case MaskMode.RemoveWhite:
                        if (GetValue(UV[triangle.First]) > 127
                            && GetValue(UV[triangle.Second]) > 127
                            && GetValue(UV[triangle.Third]) > 127)
                        {
                            return true;
                        }
                        break;
                    case MaskMode.RemoveBlack:
                        if (GetValue(UV[triangle.First]) <= 127
                            && GetValue(UV[triangle.Second]) <= 127
                            && GetValue(UV[triangle.Third]) <= 127)
                        {
                            return true;
                        }
                        break;
                    case MaskMode.Disabled:
                    default:
                        break;
                }

                return false;
            }

            private bool TestBlendShape(int movementBase, int index) =>
                BlendShapeMovements[movementBase + index].sqrMagnitude > ToleranceSquared;

            private int GetValue(Vector2 uv)
            {
                var x = Mathf.FloorToInt(Utils.Modulo(uv.x, 1) * MaskWidth);
                var y = Mathf.FloorToInt(Utils.Modulo(uv.y, 1) * MaskHeight);
                var color = Mask[x + y * MaskWidth];
                return Mathf.Max(Mathf.Max(color.r, color.g), color.b);
            }
        }

        enum MaskMode
        {
            Disabled = -1,
            RemoveWhite = RemoveMeshByMask.RemoveMode.RemoveWhite,
            RemoveBlack = RemoveMeshByMask.RemoveMode.RemoveBlack,
        }

        public void Dispose()
        {
            _triangles.Dispose();
            _uv.Dispose();
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
