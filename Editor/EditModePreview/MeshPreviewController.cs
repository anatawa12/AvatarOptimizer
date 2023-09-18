using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = System.Diagnostics.Debug;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    class MeshPreviewController : ScriptableSingleton<MeshPreviewController>
    {
        public GameObject targetGameObject;
        public Mesh originalMesh;
        public Mesh previewMesh;

        public SkinnedMeshRenderer targetRenderer;
        public VersionCounter targetRendererVersion;
        
        public RemoveMeshInBox removeMeshInBox;
        public VersionCounter removeMeshInBoxVersion;

        public RemoveMeshByBlendShape removeMeshByBlendShape;
        public VersionCounter removeMeshByBlendShapeVersion;

        public bool previewing;

        // non serialized properties
        private BlendShapePreviewContext _blendShapePreviewContext;
        private int[] _subMeshTriangleEndIndices;
        private NativeArray<Triangle> _triangles;
        [CanBeNull] private RemoveMeshWithBoxPreviewContext _removeMeshWithBoxPreviewContext;
        [CanBeNull] private RemoveMeshByBlendShapePreviewContext _removeMeshByBlendShapePreviewContext;
        private string[] _blendShapeNames;
        private List<int> _indexBuffer;

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

        public static bool Previewing => instance.previewing;

        private void UpdatePreviewing()
        {
            if (!previewing) return;

            bool ShouldStopPreview()
            {
                // target GameObject disappears
                if (targetGameObject == null || targetRenderer == null) return true;
                // animation mode externally exited
                if (!AnimationMode.InAnimationMode()) return true;
                // Showing Inspector changed
                if (ActiveEditorTracker.sharedTracker.activeEditors[0].target != targetGameObject) return true;

                return false;
            }

            if (ShouldStopPreview())
            {
                StopPreview();
                return;
            }

            var modified = false;

            if (targetRendererVersion.Update(targetRenderer, () => null) != Changed.Nothing)
            {
                _removeMeshWithBoxPreviewContext?.OnUpdateSkinnedMeshRenderer(targetRenderer);
                modified = true;
            }

            switch (removeMeshInBoxVersion.Update(removeMeshInBox,
                        () => removeMeshInBox = targetGameObject.GetComponent<RemoveMeshInBox>()))
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
                        new RemoveMeshWithBoxPreviewContext(_blendShapePreviewContext, originalMesh);
                    modified = true;
                    break;
                case Changed.Nothing:
                    break;
            }

            switch (removeMeshByBlendShapeVersion.Update(removeMeshByBlendShape,
                        () => removeMeshByBlendShape = targetGameObject.GetComponent<RemoveMeshByBlendShape>()))
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
                            new RemoveMeshByBlendShapePreviewContext(_blendShapePreviewContext, originalMesh);
                    modified = true;
                    break;
                case Changed.Nothing:
                    break;
            }

            // modifier component not found
            if (!(removeMeshInBox || removeMeshByBlendShape))
                StopPreview();

            if (modified)
                UpdatePreviewMesh();
        }

        private void InitPreviewMesh()
        {
            var subMeshes = new SubMeshDescriptor[originalMesh.subMeshCount];
            _subMeshTriangleEndIndices = new int[originalMesh.subMeshCount];
            var totalTriangles = 0;
            for (var i = 0; i < subMeshes.Length; i++)
            {
                subMeshes[i] = originalMesh.GetSubMesh(i);
                totalTriangles += subMeshes[i].indexCount / 3;
                _subMeshTriangleEndIndices[i] = totalTriangles;
            }

            _blendShapeNames = new string[originalMesh.blendShapeCount];
            for (var i = 0; i < originalMesh.blendShapeCount; i++)
                _blendShapeNames[i] = originalMesh.GetBlendShapeName(i);

            var originalTriangles = originalMesh.triangles;

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

            previewMesh = Instantiate(originalMesh);
            previewMesh.name = originalMesh.name + " (AAO Preview)";
            previewMesh.indexFormat = IndexFormat.UInt32;
        }

        private void SetPreviewMesh()
        {
            try
            {
                AnimationMode.BeginSampling();

                AnimationMode.AddPropertyModification(
                    EditorCurveBinding.PPtrCurve("", typeof(SkinnedMeshRenderer), "m_Mesh"),
                    new PropertyModification
                    {
                        target = targetRenderer,
                        propertyPath = "m_Mesh",
                        objectReference = originalMesh,
                    }, 
                    true);

                targetRenderer.sharedMesh = previewMesh;
            }
            finally
            {
                AnimationMode.EndSampling();   
            }
        }

        private void UpdatePreviewMesh()
        {
            var removeBlendShapeIndicesList = new List<int>();
            if (removeMeshByBlendShape)
            {
                var blendShapes = removeMeshByBlendShape.RemovingShapeKeys;

                for (var i = 0; i < _blendShapeNames.Length; i++)
                    if (blendShapes.Contains(_blendShapeNames[i]))
                        removeBlendShapeIndicesList.Add(i);
            }

            using (var flags = new NativeArray<bool>(_triangles.Length, Allocator.TempJob))
            {
                using (var boxes = new NativeArray<RemoveMeshInBox.BoundingBox>(
                           removeMeshInBox.boxes ?? Array.Empty<RemoveMeshInBox.BoundingBox>(), Allocator.TempJob))
                using (var blendShapeIndices =
                       new NativeArray<int>(removeBlendShapeIndicesList.ToArray(), Allocator.TempJob))
                {
                    var blendShapeAppliedVertices = _removeMeshWithBoxPreviewContext?.Vertices ?? default;
                    var blendShapeMovements = _removeMeshByBlendShapePreviewContext?.BlendShapeMovements ?? default;

                    new FlagTrianglesJob
                    {
                        Triangles = _triangles,
                        RemoveFlags = flags,
                        VertexCount = originalMesh.vertexCount,

                        Boxes = boxes,
                        BlendShapeAppliedVertices = blendShapeAppliedVertices,

                        BlendShapeIndices = blendShapeIndices,
                        ToleranceSquared = (float)(removeMeshByBlendShape ? removeMeshByBlendShape.tolerance : 0),
                        BlendShapeMovements = blendShapeMovements,
                    }.Schedule(_triangles.Length, 1).Complete();
                }

                var subMeshes = new SubMeshDescriptor[_subMeshTriangleEndIndices.Length];
                var subMeshIdx = 0;
                var subMeshIndexStart = 0;

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
                           _subMeshTriangleEndIndices[subMeshIdx] >= triIdx)
                    {
                        subMeshes[subMeshIdx] =
                            new SubMeshDescriptor(subMeshIndexStart, _indexBuffer.Count - subMeshIndexStart);
                        subMeshIndexStart = _indexBuffer.Count;
                        subMeshIdx++;
                    }
                }

                previewMesh.SetTriangles(_indexBuffer, subMeshes.Length);

                for (var i = 0; i < subMeshes.Length; i++)
                    previewMesh.SetSubMesh(i, subMeshes[i]);
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

        public bool StartPreview(GameObject expectedGameObject = null)
        {
            // Already in AnimationMode of other object
            if (AnimationMode.InAnimationMode()) return false;
            // Previewing object
            if (previewing) return false;

            targetGameObject = ActiveEditorTracker.sharedTracker.activeEditors[0].target as GameObject;
            if (targetGameObject == null) return false;
            if (expectedGameObject != null && expectedGameObject != targetGameObject) return false;
            targetRenderer = targetGameObject.GetComponent<SkinnedMeshRenderer>();
            if (targetRenderer == null) return false;

            originalMesh = targetRenderer.sharedMesh;
            _blendShapePreviewContext = new BlendShapePreviewContext(originalMesh);
            targetRendererVersion.Init(targetRenderer);

            // reset variables
            removeMeshInBox = null;
            _removeMeshWithBoxPreviewContext = null;
            removeMeshByBlendShape = null;
            _removeMeshByBlendShapePreviewContext = null;
            removeMeshInBoxVersion = default;
            removeMeshByBlendShapeVersion = default;

            removeMeshInBox = targetGameObject.GetComponent<RemoveMeshInBox>();
            if (removeMeshInBox)
            {
                removeMeshInBoxVersion.Init(removeMeshInBox);
                _removeMeshWithBoxPreviewContext =
                    new RemoveMeshWithBoxPreviewContext(_blendShapePreviewContext, originalMesh);
                _removeMeshWithBoxPreviewContext.OnUpdateSkinnedMeshRenderer(targetRenderer);
            }

            removeMeshByBlendShape = targetGameObject.GetComponent<RemoveMeshByBlendShape>();
            if (removeMeshByBlendShape)
            {
                removeMeshByBlendShapeVersion.Init(removeMeshByBlendShape);
                _removeMeshByBlendShapePreviewContext =
                    new RemoveMeshByBlendShapePreviewContext(_blendShapePreviewContext, originalMesh);
            }

            // modifier component not found
            if (!(removeMeshInBox || removeMeshByBlendShape))
            {
                StopPreview(); // for dispose invocation
                return false;
            }

            InitPreviewMesh();

            AnimationMode.StartAnimationMode();
            previewing = true;
            EditorApplication.update -= UpdatePreviewing;
            EditorApplication.update += UpdatePreviewing;
            SetPreviewMesh();
            return true;
        }

        public void StopPreview()
        {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            previewing = false;
            EditorApplication.update -= UpdatePreviewing;
            targetGameObject = null;
            targetRenderer = null;
            _triangles.Dispose();
            _removeMeshWithBoxPreviewContext?.Dispose();
            _removeMeshWithBoxPreviewContext = null;
            _removeMeshByBlendShapePreviewContext?.Dispose();
            _removeMeshByBlendShapePreviewContext = null;
        }

        [Serializable]
        public struct VersionCounter
        {
            // preview version + 1 to make default value != EditorUtility.GetDirtyCount(new object)
            public int previewVersion;

            public Changed Update(Object current, Func<Object> getObject)
            {
                if (!current)
                {
                    current = getObject();
                    if (current)
                    {
                        // newly created
                        previewVersion = EditorUtility.GetDirtyCount(current) + 1;
                        return Changed.Created;
                    }
                    else
                    {
                        if (previewVersion == 0) return Changed.Nothing;

                        // it seem component is removed
                        previewVersion = 0;
                        return Changed.Removed;
                    }
                }
                else
                {
                    var currentVersion = EditorUtility.GetDirtyCount(current) + 1;
                    if (previewVersion == currentVersion)
                        return Changed.Nothing;

                    previewVersion = currentVersion;
                    return Changed.Updated;
                }

            }

            public void Init(Object value)
            {
                if (value) previewVersion = EditorUtility.GetDirtyCount(value) + 1;
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
