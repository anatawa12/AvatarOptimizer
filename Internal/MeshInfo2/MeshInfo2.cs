#if true
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using nadena.dev.ndmf;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Debug = System.Diagnostics.Debug;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    public class MeshInfo2
    {
        public readonly Renderer SourceRenderer;
        public Transform? RootBone;
        public Bounds Bounds;
        public readonly List<Vertex> Vertices = new List<Vertex>(0);

        private readonly Mesh? _originalMesh;

        // TexCoordStatus which is 3 bits x 8 = 24 bits
        private uint _texCoordStatus;

        public readonly List<SubMesh> SubMeshes = new List<SubMesh>(0);

        public readonly List<(string name, float weight)> BlendShapes = new List<(string name, float weight)>(0);

        public readonly List<Bone> Bones = new List<Bone>();

        public bool HasColor { get; set; }
        public bool HasNormals { get; set; }
        public bool HasTangent { get; set; }

        public MeshInfo2(SkinnedMeshRenderer renderer)
        {
            SourceRenderer = renderer;
            var mesh = _originalMesh = renderer.sharedMesh;
            if (mesh != null && mesh.vertexCount != 0 && mesh.vertices.Length == 0)
            {
                var originalMeshImporter = GetImporter(ObjectRegistry.GetReference(mesh).Object as Mesh);

                ModelImporter? GetImporter(Mesh? importingMesh)
                {
                    if (!importingMesh) return null;
                    var path = AssetDatabase.GetAssetPath(importingMesh);
                    if (string.IsNullOrEmpty(path)) return null;
                    return AssetImporter.GetAtPath(path) as ModelImporter;
                }

                if (originalMeshImporter == null)
                {
                    BuildLog.LogError("MeshInfo2:error:MeshNotReadable", mesh);
                }
                else
                {
                    void AutoFix()
                    {
                        originalMeshImporter.isReadable = true;
                        originalMeshImporter.SaveAndReimport();
                    }

                    BuildLog.LogErrorWithAutoFix("MeshInfo2:error:MeshNotReadable", AutoFix, mesh);
                }

                return;
            }

            using (ErrorReport.WithContextObject(renderer))
            {
                if (mesh != null)
                    ReadSkinnedMesh(mesh);

                var updateWhenOffscreen = renderer.updateWhenOffscreen;
                renderer.updateWhenOffscreen = false;
                Bounds = renderer.localBounds;
                // ReSharper disable once Unity.InefficientPropertyAccess
                // updateWhenOffscreen = false before accessing localBounds
                renderer.updateWhenOffscreen = updateWhenOffscreen;
                RootBone = renderer.rootBone ? renderer.rootBone : renderer.transform;

                if (mesh != null)
                {
                    for (var i = 0; i < mesh.blendShapeCount; i++)
                        BlendShapes[i] = (BlendShapes[i].name, renderer.GetBlendShapeWeight(i));
                }

                SetMaterials(renderer);

                var bones = renderer.bones;
                for (var i = 0; i < bones.Length && i < Bones.Count; i++) Bones[i].Transform = bones[i];

                RemoveUnusedBones();

                AssertInvariantContract("SkinnedMeshRenderer");
            }
        }

        public MeshInfo2(MeshRenderer renderer)
        {
            SourceRenderer = renderer;
            using (ErrorReport.WithContextObject(renderer))
            {
                var meshFilter = renderer.GetComponent<MeshFilter>();
                var mesh = _originalMesh = meshFilter != null ? meshFilter.sharedMesh : null;
                if (mesh != null && !mesh.isReadable && EditorApplication.isPlaying)
                {
                    BuildLog.LogError("MeshInfo2:error:MeshNotReadable", mesh);
                    return;
                }
                if (mesh != null)
                    ReadStaticMesh(mesh);

                if (mesh != null)
                    Bounds = mesh.bounds;
                RootBone = renderer.transform;

                SetMaterials(renderer);

                AssertInvariantContract("MeshRenderer");
            }
        }

        private void SetMaterials(Renderer renderer)
        {
            if (SubMeshes.Count == 0) return;

            var sourceMaterials = renderer.sharedMaterials;

            if (sourceMaterials.Length < SubMeshes.Count)
                SubMeshes.RemoveRange(sourceMaterials.Length, SubMeshes.Count - sourceMaterials.Length);

            if (SubMeshes.Count == sourceMaterials.Length)
            {
                for (var i = 0; i < SubMeshes.Count; i++)
                    SubMeshes[i].SharedMaterial = sourceMaterials[i];
            }
            else
            {
                // there are multi pass rendering
                for (var i = 0; i < SubMeshes.Count - 1; i++)
                    SubMeshes[i].SharedMaterial = sourceMaterials[i];

                var lastMeshMaterials = new Material[sourceMaterials.Length - SubMeshes.Count + 1];
                
                for (int i = SubMeshes.Count - 1, j = 0; i < sourceMaterials.Length; i++, j++)
                    lastMeshMaterials[j] = sourceMaterials[i];
                SubMeshes[^1].SharedMaterials = lastMeshMaterials;
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public void AssertInvariantContract(string context)
        {
            var vertices = new HashSet<Vertex>(Vertices);
            Debug.Assert(SubMeshes.SelectMany(x => x.Vertices).All(vertices.Contains),
                $"{context}: some SubMesh has invalid triangles");
            var bones = new HashSet<Bone>(Bones);
            Debug.Assert(Vertices.SelectMany(x => x.BoneWeights).Select(x => x.bone).All(bones.Contains),
                $"{context}: some SubMesh has invalid bone weights");
        }

        /// <summary>
        /// Makes all vertices in this MeshInfo2 boned.
        /// </summary>
        public void MakeBoned()
        {
            if (Bones.Count != 0) return;

            Bones.Add(new Bone(Matrix4x4.identity, RootBone));

            foreach (var vertex in Vertices)
                vertex.BoneWeights.Add((Bones[0], 1f));
        }

        public void ReadSkinnedMesh(Mesh mesh)
        {
            ReadStaticMesh(mesh);

            Profiler.BeginSample("Read Skinned Mesh Part");
            Profiler.BeginSample("Read Bones");
            ReadBones(mesh);
            Profiler.EndSample();
            Profiler.BeginSample("Read BlendShapes");
            ReadBlendShapes(mesh);
            Profiler.EndSample();
        }

        private void ReadBones(Mesh mesh)
        {
            Bones.Clear();
            Bones.Capacity = Math.Max(Bones.Capacity, mesh.bindposes.Length);
            Bones.AddRange(mesh.bindposes.Select(x => new Bone(x)));

            var bonesPerVertex = mesh.GetBonesPerVertex();
            var allBoneWeights = mesh.GetAllBoneWeights();
            var bonesBase = 0;
            for (var i = 0; i < bonesPerVertex.Length; i++)
            {
                int count = bonesPerVertex[i];
                Vertices[i].BoneWeights.Capacity = count;
                foreach (var boneWeight1 in allBoneWeights.AsReadOnlySpan().Slice(bonesBase, count))
                    Vertices[i].BoneWeights.Add((Bones[boneWeight1.boneIndex], boneWeight1.weight));
                bonesBase += count;
            }
        }

        private void ReadBlendShapes(Mesh mesh)
        {
            BlendShapes.Clear();
            Profiler.BeginSample("Prepare shared buffers");
            var maxFrames = 0;
            var frameCounts = new NativeArray<int>(mesh.blendShapeCount, Allocator.TempJob);
            var shapeNames = new string[mesh.blendShapeCount];
            for (var i = 0; i < mesh.blendShapeCount; i++)
            {
                var frames = mesh.GetBlendShapeFrameCount(i);
                shapeNames[i] = mesh.GetBlendShapeName(i);
                maxFrames = Math.Max(frames, maxFrames);
                frameCounts[i] = frames;
            }

            var deltaVertices = new Vector3[Vertices.Count];
            var deltaNormals = new Vector3[Vertices.Count];
            var deltaTangents = new Vector3[Vertices.Count];
            var allFramesBuffer = new NativeArray3<Vertex.BlendShapeFrame>(mesh.blendShapeCount, Vertices.Count,
                maxFrames, Allocator.TempJob);
            var meaningfuls = new NativeArray2<bool>(mesh.blendShapeCount, Vertices.Count, Allocator.TempJob);
            Profiler.EndSample();

            for (var blendShape = 0; blendShape < mesh.blendShapeCount; blendShape++)
            {
                BlendShapes.Add((shapeNames[blendShape], 0.0f));

                for (var frame = 0; frame < frameCounts[blendShape]; frame++)
                {
                    Profiler.BeginSample("GetFrameInfo");
                    mesh.GetBlendShapeFrameVertices(blendShape, frame, deltaVertices, deltaNormals, deltaTangents);
                    var weight = mesh.GetBlendShapeFrameWeight(blendShape, frame);
                    Profiler.EndSample();

                    Profiler.BeginSample("Copy to buffer");
                    for (var vertex = 0; vertex < deltaNormals.Length; vertex++)
                    {
                        var deltaVertex = deltaVertices[vertex];
                        var deltaNormal = deltaNormals[vertex];
                        var deltaTangent = deltaTangents[vertex];
                        allFramesBuffer[blendShape, vertex, frame] = new Vertex.BlendShapeFrame(weight, deltaVertex, deltaNormal, deltaTangent);
                    }
                    Profiler.EndSample();
                }
            }

            Profiler.BeginSample("Compute Meaningful with Job");
            new ComputeMeaningfulJob
            {
                vertexCount = Vertices.Count,
                allFramesBuffer = allFramesBuffer,
                frameCounts = frameCounts,
                meaningfuls = meaningfuls,
            }.Schedule(Vertices.Count * mesh.blendShapeCount, 1).Complete();
            Profiler.EndSample();

            for (var blendShape = 0; blendShape < mesh.blendShapeCount; blendShape++)
            {
                Profiler.BeginSample("Save to Vertices");
                for (var vertex = 0; vertex < Vertices.Count; vertex++)
                {
                    if (meaningfuls[blendShape, vertex])
                    {
                        Profiler.BeginSample("Clone BlendShapes");
                        var slice = allFramesBuffer[blendShape, vertex].Slice(0, frameCounts[blendShape]);
                        Vertices[vertex].BlendShapes[shapeNames[blendShape]] = slice.ToArray();
                        Profiler.EndSample();
                    }
                }
                Profiler.EndSample();
            }

            meaningfuls.Dispose();
            frameCounts.Dispose();
            allFramesBuffer.Dispose();
            Profiler.EndSample();
        }

        [BurstCompile]
        struct ComputeMeaningfulJob : IJobParallelFor
        {
            public int vertexCount;

            // allFramesBuffer[blendShape][vertex][frame]
            [ReadOnly]
            public NativeArray3<Vertex.BlendShapeFrame> allFramesBuffer;
            [ReadOnly]
            public NativeArray<int> frameCounts;
            // allFramesBuffer[blendShape][vertex]
            [WriteOnly]
            public NativeArray2<bool> meaningfuls;

            public void Execute(int index)
            {
                var blendShape = index / vertexCount;
                var vertex = index % vertexCount;
                var slice = allFramesBuffer[blendShape, vertex].Slice(0, frameCounts[blendShape]);
                meaningfuls[blendShape, vertex] = IsMeaningful(slice);
            }
            
            bool IsMeaningful(NativeSlice<Vertex.BlendShapeFrame> frames)
            {
                foreach (var (_, position, normal, tangent) in frames)
                {
                    if (position != Vector3.zero) return true;
                    if (normal != Vector3.zero) return true;
                    if (tangent != Vector3.zero) return true;
                }

                return false;
            }
        }

        public void ReadStaticMesh(Mesh mesh)
        {
            Profiler.BeginSample($"Read Static Mesh Part");
            Vertices.Capacity = Math.Max(Vertices.Capacity, mesh.vertexCount);
            Vertices.Clear();
            for (var i = 0; i < mesh.vertexCount; i++) Vertices.Add(new Vertex());

            var vertexBuffers = GetVertexBuffers(mesh);

            CopyVertexAttr(mesh, VertexAttribute.Position, vertexBuffers, DataParsers.Vector3Provider,
                setHasAttribute: null,
                assign: (x, v) => x.Position = v);

            CopyVertexAttr(mesh, VertexAttribute.Normal, vertexBuffers, DataParsers.Vector3Provider,
                setHasAttribute: _ => HasNormals = true,
                assign: (x, v) => x.Normal = v);

            CopyVertexAttr(mesh, VertexAttribute.Tangent, vertexBuffers, DataParsers.Vector4Provider,
                setHasAttribute: _ => HasTangent = true,
                assign: (x, v) => x.Tangent = v);

            // TODO: this may lost precision or HDR color
            CopyVertexAttr(mesh, VertexAttribute.Color, vertexBuffers, DataParsers.Color32Provider,
                setHasAttribute: _ => HasColor = true,
                assign: (x, v) => x.Color = v);

            for (var uvChannel = 0; uvChannel <= 7; uvChannel++)
            {
                // ReSharper disable AccessToModifiedClosure
                CopyVertexAttr(mesh, VertexAttribute.TexCoord0 + uvChannel, vertexBuffers, DataParsers.Vector4Provider,
                    setHasAttribute: dims => SetTexCoordStatus(uvChannel, TexCoordStatus.Vector2 + (dims - 2)),
                    assign: (x, v) => x.SetTexCoord(uvChannel, v));
                // ReSharper restore AccessToModifiedClosure
            }

            SubMeshes.Clear();
            SubMeshes.Capacity = Math.Max(SubMeshes.Capacity, mesh.subMeshCount);

            var triangles = new List<int>();
            for (var i = 0; i < mesh.subMeshCount; i++)
            {
                mesh.GetIndices(triangles, i);
                SubMeshes.Add(new SubMesh(Vertices, triangles, mesh.GetSubMesh(i)));
            }
            Profiler.EndSample();
        }

        private static (byte[] buffer, int stride)[] GetVertexBuffers(Mesh mesh)
        {
            var vertexBufferCount = mesh.vertexBufferCount;
            var vertexBuffers = new (byte[] buffer, int stride)[vertexBufferCount];
            for (var i = 0; i < vertexBufferCount; i++)
            {
                var vertexBuffer = mesh.GetVertexBuffer(i);
                var stride = vertexBuffer.stride;
                var data = new byte[vertexBuffer.count * vertexBuffer.stride];
                vertexBuffer.GetData(data);
                vertexBuffers[i] = (data, stride);
            }

            return vertexBuffers;
        }

        delegate T DataParser<T>(byte[] data, int offset);
        delegate DataParser<T> ReadDataProvider<T>(VertexAttributeFormat format, int dimension);

        void CopyVertexAttr<T>(
            Mesh mesh, 
            VertexAttribute attribute, 
            (byte[] buffer, int stride)[] vertexDataList,
            ReadDataProvider<T> readProvider,
            Action<int>? setHasAttribute,
            Action<Vertex, T> assign)
        {
            var dimension = mesh.GetVertexAttributeDimension(attribute);

            if (dimension == 0)
            {
                if (setHasAttribute == null)
                    throw new InvalidOperationException($"required attribute {attribute} does not exist");
                return;
            }

            setHasAttribute?.Invoke(dimension);

            var format = mesh.GetVertexAttributeFormat(attribute);
            var stream = mesh.GetVertexAttributeStream(attribute);
            var offset = mesh.GetVertexAttributeOffset(attribute);

            var reader = readProvider(format, dimension);

            var stride = vertexDataList[stream].stride;
            var buffer = vertexDataList[stream].buffer;
 
            for (var i = 0; i < Vertices.Count; i++)
            {
                var data = reader(buffer, stride * i + offset);
                assign(Vertices[i], data);
            }
        }

        static class DataParsers
        {
            public static DataParser<Vector3> Vector3Provider(VertexAttributeFormat format, int dimension)
            {
                switch (dimension)
                {
                    case 1:
                        var floatParser = FloatParser(format);
                        return (data, offset) => new Vector3(floatParser(data, offset), 0, 0);
                    case 2:
                        var vector2Parser = Vector2Parser(format);
                        return (data, offset) => vector2Parser(data, offset);
                    case 3:
                        return Vector3Parser(format);
                    case 4:
                        var vector4Parser = Vector4Parser(format);
                        return (data, offset) => vector4Parser(data, offset);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(dimension), dimension, null);
                }
            }

            public static DataParser<Vector4> Vector4Provider(VertexAttributeFormat format, int dimension)
            {
                switch (dimension)
                {
                    case 1:
                        var floatParser = FloatParser(format);
                        return (data, offset) => new Vector4(floatParser(data, offset), 0, 0);
                    case 2:
                        var vector2Parser = Vector2Parser(format);
                        return (data, offset) => vector2Parser(data, offset);
                    case 3:
                        var vector3Parser = Vector3Parser(format);
                        return (data, offset) => vector3Parser(data, offset);
                    case 4:
                        return Vector4Parser(format);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(dimension), dimension, null);
                }
            }

            public static DataParser<Color32> Color32Provider(VertexAttributeFormat format, int dimension)
            {
                var vector4Parser = Vector4Provider(format, dimension);
                return (data, offset) =>
                {
                    var color = vector4Parser(data, offset);
                    color *= byte.MaxValue;
                    return new Color32((byte)color.x, (byte)color.y, (byte)color.z, (byte)color.w);
                };
            }

            private static DataParser<Vector4> Vector4Parser(VertexAttributeFormat format)
            {
                var size = ValueSize(format);
                var floatParser = FloatParser(format);
                return (data, offset) => new Vector4(
                    floatParser(data, offset),
                    floatParser(data, offset + size),
                    floatParser(data, offset + size * 2),
                    floatParser(data, offset + size * 3));
            }

            private static DataParser<Vector3> Vector3Parser(VertexAttributeFormat format)
            {
                var size = ValueSize(format);
                var floatParser = FloatParser(format);
                return (data, offset) => new Vector3(
                    floatParser(data, offset),
                    floatParser(data, offset + size),
                    floatParser(data, offset + size * 2));
            }

            private static DataParser<Vector2> Vector2Parser(VertexAttributeFormat format)
            {
                var size = ValueSize(format);
                var floatParser = FloatParser(format);
                return (data, offset) => new Vector2(
                    floatParser(data, offset),
                    floatParser(data, offset + size));
            }

            private static int ValueSize(VertexAttributeFormat format) =>
                format switch
                {
                    VertexAttributeFormat.Float32 or VertexAttributeFormat.UInt32 or VertexAttributeFormat.SInt32 => 4,
                    VertexAttributeFormat.Float16 or VertexAttributeFormat.UNorm16 or VertexAttributeFormat.SNorm16
                        or VertexAttributeFormat.UInt16 or VertexAttributeFormat.SInt16 => 2,
                    VertexAttributeFormat.UNorm8 or VertexAttributeFormat.SNorm8 or VertexAttributeFormat.UInt8
                        or VertexAttributeFormat.SInt8 => 1,
                    _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
                };

            // those casts are checked with this gist
            // https://gist.github.com/anatawa12/e27775280a273f7f433c8b427aeb3108
            private static DataParser<float> FloatParser(VertexAttributeFormat format) =>
                format switch
                {
                    VertexAttributeFormat.Float32 => (data, offset) => BitConverter.ToSingle(data, offset),
                    VertexAttributeFormat.Float16 => (data, offset) =>
                        Mathf.HalfToFloat(BitConverter.ToUInt16(data, offset)),
                    VertexAttributeFormat.UNorm8 => (data, offset) => data[offset] / (float)byte.MaxValue,
                    // -128 become -1.007874015748 is correct behaior
                    VertexAttributeFormat.SNorm8 => (data, offset) => (sbyte)data[offset] / (float)sbyte.MaxValue,
                    VertexAttributeFormat.UNorm16 => (data, offset) => BitConverter.ToUInt16(data, offset) / (float)ushort.MaxValue,
                    VertexAttributeFormat.SNorm16 => (data, offset) => BitConverter.ToInt16(data, offset) / (float)short.MaxValue,
                    VertexAttributeFormat.UInt8 => (data, offset) => data[offset],
                    VertexAttributeFormat.SInt8 => (data, offset) => (sbyte)data[offset],
                    VertexAttributeFormat.UInt16 => (data, offset) => BitConverter.ToUInt16(data, offset),
                    VertexAttributeFormat.SInt16 => (data, offset) => BitConverter.ToInt16(data, offset),
                    // TODO: Those can be loose precision
                    VertexAttributeFormat.UInt32 => (data, offset) => BitConverter.ToUInt32(data, offset),
                    VertexAttributeFormat.SInt32 => (data, offset) => BitConverter.ToInt32(data, offset),
                    _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
                };
        }

        private const int BitsPerTexCoordStatus = 2;
        private const int TexCoordStatusMask = (1 << BitsPerTexCoordStatus) - 1;

        public TexCoordStatus GetTexCoordStatus(int index)
        {
            return (TexCoordStatus)((_texCoordStatus >> (index * BitsPerTexCoordStatus)) & TexCoordStatusMask);
        }

        public void SetTexCoordStatus(int index, TexCoordStatus value)
        {
            _texCoordStatus = (ushort)(
                _texCoordStatus & ~(TexCoordStatusMask << (BitsPerTexCoordStatus * index)) | 
                ((uint)value & TexCoordStatusMask) << (BitsPerTexCoordStatus * index));
        }

        public void ClearMeshData()
        {
            Vertices.Clear();
            _texCoordStatus = default;
            SubMeshes.Clear();
            BlendShapes.Clear();
            Bones.Clear();
            HasColor = false;
            HasNormals = false;
            HasTangent = false;
        }

        public bool IsEmpty() => Bounds == default && IsEmptyMesh();
        
        public bool IsEmptyMesh() =>
            Vertices.Count == 0 &&
            SubMeshes.Count == 0 &&
            BlendShapes.Count == 0 &&
            Bones.Count == 0;

        public void Optimize()
        {
            RemoveUnusedBones();
        }

        private void RemoveUnusedBones()
        {
            if (Bones.Count == 0) return;
            var hasValidBone = Bones.Any(x => x.Transform != null);

            // GC Bones
            var usedBones = new HashSet<Bone>();
            foreach (var meshInfo2Vertex in Vertices)
            foreach (var (bone, _) in meshInfo2Vertex.BoneWeights)
                usedBones.Add(bone);
            Bones.RemoveAll(x => !usedBones.Contains(x));

            if (hasValidBone && Bones.All(x => x.Transform == null))
            {
                // if all transform is null, the renderer will render nothing.
                // so we have to some bone to render.
                if (RootBone) Bones.Add(new Bone(Matrix4x4.identity, RootBone));
            }
        }

        /// <returns>true if we flattened multi pass rendering</returns>
        public void FlattenMultiPassRendering(string reasonComponent)
        {
            if (SubMeshes.All(x => x.SharedMaterials.Length == 1)) return;
            
            BuildLog.LogWarning("MeshInfo2:warning:multiPassRendering", reasonComponent, SourceRenderer);

            // flatten SubMeshes
            var subMeshes = SubMeshes.ToArray();
            SubMeshes.Clear();
            foreach (var subMesh in subMeshes)
            foreach (var material in subMesh.SharedMaterials)
                SubMeshes.Add(new SubMesh(subMesh, material));
        }

        public void WriteToMesh(Mesh destMesh)
        {
            Optimize();
            destMesh.Clear();

            // if mesh is empty, clearing mesh is enough!
            if (SubMeshes.Count == 0) return; 

            Profiler.BeginSample("Write to Mesh");

            Profiler.BeginSample("Vertices and Normals");
            // Basic Vertex Attributes: vertices, normals
            {
                var vertices = new Vector3[Vertices.Count];
                for (var i = 0; i < Vertices.Count; i++)
                    vertices[i] = Vertices[i].Position;
                destMesh.vertices = vertices;
            }

            // tangents
            if (HasNormals)
            {
                var normals = new Vector3[Vertices.Count];
                for (var i = 0; i < Vertices.Count; i++)
                    normals[i] = Vertices[i].Normal;
                destMesh.normals = normals;
            }
            Profiler.EndSample();

            // tangents
            if (HasTangent)
            {
                Profiler.BeginSample("Tangents");
                var tangents = new Vector4[Vertices.Count];
                for (var i = 0; i < Vertices.Count; i++)
                    tangents[i] = Vertices[i].Tangent;
                destMesh.tangents = tangents;
                Profiler.EndSample();
            }

            // UVs
            {
                var uv2 = new Vector2[Vertices.Count];
                var uv3 = new Vector3[Vertices.Count];
                var uv4 = new Vector4[Vertices.Count];
                for (var uvIndex = 0; uvIndex < 8; uvIndex++)
                {
                    Profiler.BeginSample($"UV#{uvIndex}");
                    switch (GetTexCoordStatus(uvIndex))
                    {
                        case TexCoordStatus.NotDefined:
                            // nothing to do
                            break;
                        case TexCoordStatus.Vector2:
                            for (var i = 0; i < Vertices.Count; i++)
                                uv2[i] = Vertices[i].GetTexCoord(uvIndex);
                            destMesh.SetUVs(uvIndex, uv2);
                            break;
                        case TexCoordStatus.Vector3:
                            for (var i = 0; i < Vertices.Count; i++)
                                uv3[i] = Vertices[i].GetTexCoord(uvIndex);
                            destMesh.SetUVs(uvIndex, uv3);
                            break;
                        case TexCoordStatus.Vector4:
                            for (var i = 0; i < Vertices.Count; i++)
                                uv4[i] = Vertices[i].GetTexCoord(uvIndex);
                            destMesh.SetUVs(uvIndex, uv4);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    Profiler.EndSample();
                }
            }

            // color
            if (HasColor)
            {
                Profiler.BeginSample($"Vertex Color");
                var colors = new Color32[Vertices.Count];
                for (var i = 0; i < Vertices.Count; i++)
                    colors[i] = Vertices[i].Color;
                destMesh.colors32 = colors;
                Profiler.EndSample();
            }

            // bones
            destMesh.bindposes = Bones.Select(x => x.Bindpose.ToUnity()).ToArray();

            // triangles and SubMeshes
            Profiler.BeginSample("Triangles");
            {
                var vertexIndices = new Dictionary<Vertex, int>();
                // first, set vertex indices
                for (var i = 0; i < Vertices.Count; i++)
                    vertexIndices.Add(Vertices[i], i);

                var submeshWithMoreThan65536Verts = false;
                var submeshIndexBuffersList = new List<(SubMesh subMesh, int baseVertex, int[] indices)>();

                for (var i = 0; i < SubMeshes.Count - 1; i++)
                {
                    var subMesh = SubMeshes[i];

                    // for non-last submesh, we have to duplicate submesh for multi pass rendering
                    AddSubmesh(subMesh, vertexIndices, submeshIndexBuffersList, ref submeshWithMoreThan65536Verts, subMesh.SharedMaterials.Length);
                }

                {
                    var subMesh = SubMeshes[^1];
                    // for last submesh, we can use single submesh for multi pass rendering
                    AddSubmesh(subMesh, vertexIndices, submeshIndexBuffersList, ref submeshWithMoreThan65536Verts, 1);
                }

                static void AddSubmesh(SubMesh subMesh, Dictionary<Vertex, int> vertexIndices, List<(SubMesh subMesh, int baseVertex, int[] indices)> submeshIndexBuffers, ref bool submeshWithMoreThan65536Verts, int count)
                {
                    var indices = new int[subMesh.Vertices.Count];
                    for (var index = 0; index < subMesh.Vertices.Count; index++)
                        indices[index] = vertexIndices[subMesh.Vertices[index]];

                    var min = indices.Min();
                    var max = indices.Max();
                    submeshWithMoreThan65536Verts |= max - min >= ushort.MaxValue;

                    for (var j = 0; j < count; j++)
                        submeshIndexBuffers.Add((subMesh, 0, indices));
                }

                var submeshIndexBuffers = submeshIndexBuffersList.ToArray();

                // determine index format
                // if all vertices has less than 65536 vertices, we can use UInt16
                // if all vertices has more than 65536 vertices but each submesh has less than 65536 vertices, we can use UInt16 with vaseVertex
                // otherwise, we have to use UInt32
                //
                // Please note currently there is no optimization for index buffer to apply this optimization perfectly.
                // You may need to reorder meshes in Merge Skinned Mesh. I will implement this optimization in future if I can.

                if (Vertices.Count <= ushort.MaxValue)
                {
                    destMesh.indexFormat = IndexFormat.UInt16;
                }
                else if (!submeshWithMoreThan65536Verts)
                {
                    destMesh.indexFormat = IndexFormat.UInt16;
                    foreach (ref var submeshIndexBuffer in submeshIndexBuffers.AsSpan())
                    {
                        submeshIndexBuffer.baseVertex = submeshIndexBuffer.indices.Min();
                        for (var i = 0; i < submeshIndexBuffer.indices.Length; i++)
                            submeshIndexBuffer.indices[i] -= submeshIndexBuffer.baseVertex;
                    }
                }
                else
                {
                    destMesh.indexFormat = IndexFormat.UInt32;
                }

                var submeshIndex = 0;

                destMesh.subMeshCount = submeshIndexBuffers.Length;

                for (var i = 0; i < submeshIndexBuffers.Length; i++)
                {
                    var (subMesh, baseVertex, indices) = submeshIndexBuffers[i];
                    destMesh.SetIndices(indices, 0, subMesh.Vertices.Count, subMesh.Topology, i,
                        baseVertex: baseVertex);
                }

                Debug.Assert(submeshIndexBuffers.Length == submeshIndex);
            }
            Profiler.EndSample();

            // BoneWeights
            if (Vertices.Any(x => x.BoneWeights.Count != 0)){
                Profiler.BeginSample("BoneWeights");
                var boneIndices = new Dictionary<Bone, int>();
                for (var i = 0; i < Bones.Count; i++)
                    boneIndices.Add(Bones[i], i);

                var bonesPerVertex = new NativeArray<byte>(Vertices.Count, Allocator.Temp);
                var allBoneWeights =
                    new NativeArray<BoneWeight1>(Vertices.Sum(x => x.BoneWeights.Count), Allocator.Temp);
                var boneWeightsIndex = 0;
                for (var i = 0; i < Vertices.Count; i++)
                {
                    bonesPerVertex[i] = (byte)Vertices[i].BoneWeights.Count;
                    Vertices[i].BoneWeights.Sort((x, y) => -x.weight.CompareTo(y.weight));
                    foreach (var (bone, weight) in Vertices[i].BoneWeights)
                        allBoneWeights[boneWeightsIndex++] = new BoneWeight1
                            { boneIndex = boneIndices[bone], weight = weight };
                }

                destMesh.SetBoneWeights(bonesPerVertex, allBoneWeights);
                Profiler.EndSample();
            }

            // BlendShapes
            if (BlendShapes.Count != 0)
            {
                Profiler.BeginSample("BlendShapes");
                for (var i = 0; i < BlendShapes.Count; i++)
                {
                    Debug.Assert(destMesh.blendShapeCount == i, "Unexpected state: BlendShape count");
                    var (shapeName, _) = BlendShapes[i];
                    var weightsSet = new HashSet<float>();

                    foreach (var vertex in Vertices)
                        if (vertex.BlendShapes.TryGetValue(shapeName, out var frames))
                            foreach (var frame in frames)
                                weightsSet.Add(frame.Weight);

                    // blendShape with no weights is not allowed.
                    if (weightsSet.Count == 0)
                        weightsSet.Add(100);

                    var weights = weightsSet.ToArray();
                    Array.Sort(weights);

                    var positions = new Vector3[Vertices.Count];
                    var normals = new Vector3[Vertices.Count];
                    var tangents = new Vector3[Vertices.Count];

                    foreach (var weight in weights)
                    {
                        for (var vertexI = 0; vertexI < Vertices.Count; vertexI++)
                        {
                            var vertex = Vertices[vertexI];

                            vertex.TryGetBlendShape(shapeName, weight, 
                                out var position, out var normal, out var tangent,
                                getDefined: true);
                            positions[vertexI] = position;
                            normals[vertexI] = normal;
                            tangents[vertexI] = tangent;
                        }

                        destMesh.AddBlendShapeFrame(shapeName, weight, positions, normals, tangents);
                    }
                }
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }

        public void WriteToSkinnedMeshRenderer(SkinnedMeshRenderer targetRenderer)
        {
            using (ErrorReport.WithContextObject(targetRenderer))
            {
                if (!IsEmptyMesh() || _originalMesh != null)
                {
                    var name = $"AAOGeneratedMesh{targetRenderer.name}";
                    var mesh = new Mesh { name = name };

                    WriteToMesh(mesh);
                    // I don't know why but Instantiating mesh will fix broken BlendShapes with
                    // https://github.com/anatawa12/AvatarOptimizer/issues/753
                    // https://booth.pm/ja/items/1054593.
                    mesh = Object.Instantiate(mesh);
                    mesh.name = name;
                    if (_originalMesh) ObjectRegistry.RegisterReplacedObject(_originalMesh, mesh);
                    targetRenderer.sharedMesh = mesh;
                }

                for (var i = 0; i < BlendShapes.Count; i++)
                    targetRenderer.SetBlendShapeWeight(i, BlendShapes[i].weight);
                targetRenderer.sharedMaterials = SubMeshes.SelectMany(x => x.SharedMaterials).ToArray();
                targetRenderer.bones = Bones.Select(x => x.Transform).ToArray();

                targetRenderer.rootBone = RootBone;
                var offscreen = targetRenderer.updateWhenOffscreen;
                targetRenderer.updateWhenOffscreen = false;
                if (Bounds != default)
                    targetRenderer.localBounds = Bounds;
                targetRenderer.updateWhenOffscreen = offscreen;
            }
        }

        public void WriteToMeshRenderer(MeshRenderer targetRenderer)
        {
            using (ErrorReport.WithContextObject(targetRenderer))
            {
                var mesh = new Mesh { name = $"AAOGeneratedMesh{targetRenderer.name}" };
                var meshFilter = targetRenderer.GetComponent<MeshFilter>();
                WriteToMesh(mesh);
                if (_originalMesh) ObjectRegistry.RegisterReplacedObject(_originalMesh, mesh);
                meshFilter.sharedMesh = mesh;
                targetRenderer.sharedMaterials = SubMeshes.SelectMany(x => x.SharedMaterials).ToArray();
            }
        }

        public override string ToString() =>
            SourceRenderer ? $"MeshInfo2({SourceRenderer})" : $"MeshInfo2(Not Belong to Renderer)";
    }

    public class SubMesh
    {
        public readonly MeshTopology Topology = MeshTopology.Triangles;

        // size of this must be 3 * n
        public List<Vertex> Triangles
        {
            get
            {
                Debug.Assert(Topology == MeshTopology.Triangles);
                return Vertices;
            }
        }

        public List<Vertex> Vertices { get; } = new List<Vertex>();

        public Material? SharedMaterial
        {
            get => SharedMaterials[0];
            set => SharedMaterials[0] = value;
        }

        public Material?[] SharedMaterials = { null };

        public SubMesh()
        {
        }

        public SubMesh(List<Vertex> vertices) => Vertices = vertices;
        public SubMesh(List<Vertex> vertices, Material sharedMaterial) => 
            (Vertices, SharedMaterial) = (vertices, sharedMaterial);
        public SubMesh(Material? sharedMaterial) => SharedMaterial = sharedMaterial;
        public SubMesh(Material? sharedMaterial, MeshTopology topology) =>
            (SharedMaterial, Topology) = (sharedMaterial, topology);

        public SubMesh(SubMesh subMesh, Material? material)
        {
            Topology = subMesh.Topology;
            Vertices = new List<Vertex>(subMesh.Vertices);
            SharedMaterial = material;
        }

        public SubMesh(List<Vertex> vertices, List<int> triangles, SubMeshDescriptor descriptor)
        {
            Topology = descriptor.topology;
            Vertices.Capacity = descriptor.indexCount;
            foreach (var i in triangles)
                Vertices.Add(vertices[i]);
        }

        public bool TryGetPrimitiveSize(string component, out int primitiveSize)
        {
            switch (Topology)
            {
                case MeshTopology.Triangles:
                    primitiveSize = 3;
                    return true;
                case MeshTopology.Quads:
                    primitiveSize = 4;
                    return true;
                case MeshTopology.Lines:
                    primitiveSize = 2;
                    return true;
                case MeshTopology.Points:
                    primitiveSize = 1;
                    return true;
                case MeshTopology.LineStrip:
                    BuildLog.LogWarning("MeshInfo2:warning:lineStrip", component);
                    primitiveSize = default;
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void RemovePrimitives(string component, Func<Vertex[], bool> condition)
        {
            if (!TryGetPrimitiveSize(component, out var primitiveSize))
                return;
            var primitiveBuffer = new Vertex[primitiveSize];
            int srcI = 0, dstI = 0;
            for (; srcI < Vertices.Count; srcI += primitiveSize)
            {
                for (var i = 0; i < primitiveSize; i++)
                    primitiveBuffer[i] = Vertices[srcI + i];

                if (condition(primitiveBuffer))
                    continue;

                // no vertex is in box: 
                for (var i = 0; i < primitiveSize; i++)
                    Vertices[dstI + i] = primitiveBuffer[i];
                dstI += primitiveSize;
            }
            Vertices.RemoveRange(dstI, Vertices.Count - dstI);
        }
    }

    public class Vertex
    {
        public Vector3 Position { get; set; }
        public Vector3 Normal { get; set; }
        public Vector4 Tangent { get; set; } = new Vector4(1, 0, 0, 1);
        public Vector4 TexCoord0 { get; set; }
        public Vector4 TexCoord1 { get; set; }
        public Vector4 TexCoord2 { get; set; }
        public Vector4 TexCoord3 { get; set; }
        public Vector4 TexCoord4 { get; set; }
        public Vector4 TexCoord5 { get; set; }
        public Vector4 TexCoord6 { get; set; }
        public Vector4 TexCoord7 { get; set; }

        public Color32 Color { get; set; } = new Color32(0xff, 0xff, 0xff, 0xff);

        // SkinnedMesh related
        public List<(Bone bone, float weight)> BoneWeights = new List<(Bone, float)>();

        // Each frame must sorted increasingly
        public readonly Dictionary<string, BlendShapeFrame[]> BlendShapes = 
            new Dictionary<string, BlendShapeFrame[]>();

        public readonly struct BlendShapeFrame
        {
            public readonly float Weight;
            public readonly Vector3 Position;
            public readonly Vector3 Normal;
            public readonly Vector3 Tangent;

            public BlendShapeFrame(float weight, Vector3 position, Vector3 normal, Vector3 tangent)
            {
                Position = position;
                Normal = normal;
                Tangent = tangent;
                Weight = weight;
            }

            public void Deconstruct(out float weight, out Vector3 position, out Vector3 normal, out Vector3 tangent)
            {
                weight = Weight;
                position = Position;
                normal = Normal;
                tangent = Tangent;
            }
        }

        public Vector4 GetTexCoord(int index)
        {
            switch (index)
            {
                // @formatter off
                case 0: return TexCoord0;
                case 1: return TexCoord1;
                case 2: return TexCoord2;
                case 3: return TexCoord3;
                case 4: return TexCoord4;
                case 5: return TexCoord5;
                case 6: return TexCoord6;
                case 7: return TexCoord7;
                default: throw new IndexOutOfRangeException("TexCoord index");
                // @formatter on
            }
        }

        public void SetTexCoord(int index, Vector4 value)
        {
            switch (index)
            {
                // @formatter off
                case 0: TexCoord0 = value; break;
                case 1: TexCoord1 = value; break;
                case 2: TexCoord2 = value; break;
                case 3: TexCoord3 = value; break;
                case 4: TexCoord4 = value; break;
                case 5: TexCoord5 = value; break;
                case 6: TexCoord6 = value; break;
                case 7: TexCoord7 = value; break;
                default: throw new IndexOutOfRangeException("TexCoord index");
                // @formatter on
            }
        }

        public bool TryGetBlendShape(string name, float weight, out Vector3 position, out Vector3 normal,
            out Vector3 tangent, bool getDefined = false)
        {
            if (!BlendShapes.TryGetValue(name, out var frames))
            {
                position = default;
                normal = default;
                tangent = default;
                return false;
            }

            if (frames.Length == 0)
            {
                position = default;
                normal = default;
                tangent = default;
                return false;
            }

            if (!getDefined && Mathf.Abs(weight) <= 0.0001f && ZeroForWeightZero())
            {
                position = Vector3.zero;
                normal = Vector3.zero;
                tangent = Vector3.zero;
                return true;
            }

            bool ZeroForWeightZero()
            {
                if (frames.Length == 1) return true;
                var first = frames.First();
                var end = frames.Last();

                // both weight are same sign, zero for 0 weight
                if (first.Weight <= 0 && end.Weight <= 0) return true;
                if (first.Weight >= 0 && end.Weight >= 0) return true;

                return false;
            }

            if (frames.Length == 1)
            {
                // simplest and likely
                var frame = frames[0];
                var ratio = weight / frame.Weight;
                position = frame.Position * ratio;
                normal = frame.Normal * ratio;
                tangent = frame.Tangent * ratio;
                return true;
            }
            else
            {
                // multi frame
                var (lessFrame, greaterFrame) = FindFrame();
                var ratio = InverseLerpUnclamped(lessFrame.Weight, greaterFrame.Weight, weight);

                position = Vector3.LerpUnclamped(lessFrame.Position, greaterFrame.Position, ratio);
                normal = Vector3.LerpUnclamped(lessFrame.Normal, greaterFrame.Normal, ratio);
                tangent = Vector3.LerpUnclamped(lessFrame.Tangent, greaterFrame.Tangent, ratio);
                return true;
            }

            (BlendShapeFrame, BlendShapeFrame) FindFrame()
            {
                var firstFrame = frames[0];
                var lastFrame = frames.Last();

                if (firstFrame.Weight > 0 && weight < firstFrame.Weight)
                {
                    // if all weights are positive and the weight is less than first weight: lerp 0..first
                    return (default, firstFrame);
                }

                if (lastFrame.Weight < 0 && weight > lastFrame.Weight)
                {
                    // if all weights are negative and the weight is more than last weight: lerp last..0
                    return (lastFrame, default);
                }

                // otherwise, lerp between two surrounding frames OR nearest two frames

                for (var i = 1; i < frames.Length; i++)
                {
                    if (weight <= frames[i].Weight)
                        return (frames[i - 1], frames[i]);
                }

                return (frames[frames.Length - 2], frames[frames.Length - 1]);
            }

            float InverseLerpUnclamped(float a, float b, float value) => (value - a) / (b - a);
        }

        public Vertex()
        {
        }

        private Vertex(Vertex vertex)
        {
            Position = vertex.Position;
            Normal = vertex.Normal;
            Tangent = vertex.Tangent;
            TexCoord0 = vertex.TexCoord0;
            TexCoord1 = vertex.TexCoord1;
            TexCoord2 = vertex.TexCoord2;
            TexCoord3 = vertex.TexCoord3;
            TexCoord4 = vertex.TexCoord4;
            TexCoord5 = vertex.TexCoord5;
            TexCoord6 = vertex.TexCoord6;
            TexCoord7 = vertex.TexCoord7;
            Color = vertex.Color;
            BoneWeights = vertex.BoneWeights.ToList();
            BlendShapes = new Dictionary<string, BlendShapeFrame[]>(vertex.BlendShapes);
        }

        public Vertex Clone() => new Vertex(this);

        public Vector3 ComputeActualPosition(MeshInfo2 meshInfo2, Func<Transform, Matrix4x4> getLocalToWorld, Matrix4x4 rendererWorldToLocalMatrix)
        {
            var position = Position;

            // first, apply BlendShapes
            foreach (var (name, weight) in meshInfo2.BlendShapes)
                if (TryGetBlendShape(name, weight, out var posDelta, out _, out _))
                    position += posDelta;

            // then, apply bones
            var matrix = Matrix4x4.zero;
            foreach (var (bone, weight) in BoneWeights)
            {
                var transformMat = bone.Transform != null
                    ? getLocalToWorld(bone.Transform)
                    : Matrix4x4.identity;
                var boneMat = transformMat * bone.Bindpose;
                matrix += boneMat * weight;
            }

            matrix = rendererWorldToLocalMatrix * matrix;
            return matrix * new Vector4(position.x, position.y, position.z, 1f);
        }
    }

    public class Bone
    {
        public Matrix4x4 Bindpose;
        public Transform? Transform;

        public Bone(Matrix4x4 bindPose) : this(bindPose, null) {}
        public Bone(Matrix4x4 bindPose, Transform? transform) => (Bindpose, Transform) = (bindPose, transform);
    }

    public enum TexCoordStatus
    {
        NotDefined = 0,
        Vector2 = 1,
        Vector3 = 2,
        Vector4 = 3,
    }
}
#endif
