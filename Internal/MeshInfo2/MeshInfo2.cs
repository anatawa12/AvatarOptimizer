#if true
using System;
using System.Collections;
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
using Random = UnityEngine.Random;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    public class MeshInfo2 : IDisposable
    {
        public static bool MeshValidationEnabled = true;

        public readonly Renderer SourceRenderer;
        public Transform? RootBone;
        public Bounds Bounds;
        // owns Vertices
        public readonly List<Vertex> VerticesMutable = new List<Vertex>(0);
        public IReadOnlyList<Vertex> Vertices => VerticesMutable;

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

            using (ErrorReport.WithContextObject(renderer))
            {
                if (mesh != null)
                    ReadSkinnedMesh(mesh);

                Profiler.BeginSample("Bounds");
                var updateWhenOffscreen = renderer.updateWhenOffscreen;
                renderer.updateWhenOffscreen = false;
                Bounds = renderer.localBounds;
                // ReSharper disable once Unity.InefficientPropertyAccess
                // updateWhenOffscreen = false before accessing localBounds
                renderer.updateWhenOffscreen = updateWhenOffscreen;
                Profiler.EndSample();
                RootBone = renderer.rootBone ? renderer.rootBone : renderer.transform;

                Profiler.BeginSample("GetBlendShapeWeight");
                if (mesh != null)
                {
                    for (var i = 0; i < mesh.blendShapeCount; i++)
                        BlendShapes[i] = (BlendShapes[i].name, renderer.GetBlendShapeWeight(i));
                }
                Profiler.EndSample();

                SetMaterials(renderer);

                Profiler.BeginSample("Bone Transforms");
                var bones = renderer.bones;
                for (var i = 0; i < bones.Length && i < Bones.Count; i++) Bones[i].Transform = bones[i];
                Profiler.EndSample();

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

            Profiler.BeginSample("SetMaterials");

            var sourceMaterials = renderer.sharedMaterials;

            // In previous version of MeshInfo2, we removed SubMeshes if there are no materials
            // since we thought no materials means no rendering, so it won't be used.
            // However, acutally, Particle Systems uses SubMeshes without materials.
            // Therefore, we keep SubMeshes here and have empty materisls in SubMeshes,
            // and remove them in RemoveUnusedObjects or FlattenMultiPassRendering.
            //if (sourceMaterials.Length < SubMeshes.Count)
            //    SubMeshes.RemoveRange(sourceMaterials.Length, SubMeshes.Count - sourceMaterials.Length);

            if (sourceMaterials.Length <= SubMeshes.Count)
            {
                for (var i = 0; i < sourceMaterials.Length; i++)
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

            Profiler.EndSample();
        }

        [Conditional("UNITY_ASSERTIONS")]
        public void AssertInvariantContract(string context)
        {
            if (!MeshValidationEnabled) return;

            Profiler.BeginSample("AssertInvariantContract");
            var vertices = new HashSet<Vertex>(Vertices);
            Utils.Assert(SubMeshes.SelectMany(x => x.Vertices).All(vertices.Contains),
                $"{context}: some SubMesh has invalid triangles");
            var bones = new HashSet<Bone>(Bones);
            Utils.Assert(Vertices.SelectMany(x => x.BoneWeights).Select(x => x.bone).All(bones.Contains),
                $"{context}: some SubMesh has invalid bone weights");
            Profiler.EndSample();
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
            Profiler.BeginSample("Read Skinned Mesh");
            ReadStaticMesh(mesh);

            Profiler.BeginSample("Read Skinned Mesh Part");
            Profiler.BeginSample("Read Bones");
            ReadBones(mesh);
            Profiler.EndSample();
            Profiler.BeginSample("Read BlendShapes");
            ReadBlendShapes(mesh);
            Profiler.EndSample();
            Profiler.EndSample();
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
            Profiler.BeginSample("New Reading Method");
            BlendShapes.Clear();
            var buffer = new BlendShapeBuffer(mesh, BlendShapes);
            for (var vertex = 0; vertex < Vertices.Count; vertex++)
            {
                Vertices[vertex].BlendShapeBuffer = buffer;
                Vertices[vertex].BlendShapeBufferVertexIndex = vertex;
            }
            Profiler.EndSample();
        }

        public void ReadStaticMesh(Mesh mesh)
        {
            Profiler.BeginSample($"Read Static Mesh Part");
            VerticesMutable.Capacity = Math.Max(VerticesMutable.Capacity, mesh.vertexCount);
            Utils.DisposeAll(VerticesMutable);
            VerticesMutable.Clear();
            for (var i = 0; i < mesh.vertexCount; i++) VerticesMutable.Add(new Vertex());

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
                var hasUV = CopyVertexAttr(mesh, VertexAttribute.TexCoord0 + uvChannel, vertexBuffers, DataParsers.Vector4Provider,
                    setHasAttribute: dims => SetTexCoordStatus(uvChannel, TexCoordStatus.Vector2 + (dims - 2)),
                    assign: (x, v) => x.SetTexCoord(uvChannel, v));

                // if uvN is absent, copy from uvN-1
                if (!hasUV && uvChannel != 0)
                {
                    var prevChannel = uvChannel - 1;
                    Vertices.AsParallel().ForAll(v => v.SetTexCoord(uvChannel, v.GetTexCoord(prevChannel)));
                }
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

            var usedVertices = SubMeshes.SelectMany(x => x.Vertices).ToHashSet();
            foreach (var vertex in Vertices)
                vertex.IsOrphanVertex = !usedVertices.Contains(vertex);
            Profiler.EndSample();
        }

        private static (byte[] buffer, int stride)[] GetVertexBuffers(Mesh mesh)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("MeshInfo2 does not support -nographics environment");

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

        bool CopyVertexAttr<T>(
            Mesh mesh, 
            VertexAttribute attribute, 
            (byte[] buffer, int stride)[] vertexDataList,
            ReadDataProvider<T> readProvider,
            Action<int>? setHasAttribute,
            Action<Vertex, T> assign)
        {
            if (Vertices.Count == 0) return true;

            var dimension = mesh.GetVertexAttributeDimension(attribute);

            if (dimension == 0)
            {
                if (setHasAttribute == null)
                    throw new InvalidOperationException($"required attribute {attribute} does not exist");
                return false;
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

            return true;
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
            Utils.DisposeAll(VerticesMutable);
            VerticesMutable.Clear();
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
            Profiler.BeginSample("Remove Unused Bones");
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
            Profiler.EndSample();
        }

        /// <summary>
        /// Flattens multi pass rendereing, and removes unused submeshes.
        /// After this function is applied, all submeshes will have exactly one material.
        /// </summary>
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

        public void WriteToMesh(Mesh destMesh, bool isSkinnedMesh = false)
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

                // Until last non-empty submesh, we have to skip generating submesh if no material is assigned,
                // because later materials will be assigned to another material slot.
                // After last non-empty submesh, we can generate submesh even if no material is assigned.
                // The reason to generate submesh without material is to support meshes referenced by particle system.
                //
                // If the materials and submeshes in MeshInfo2 are like this:
                // Materials: | 0 | 1 | ∅ | 2 | 3 |
                // SubMeshes: |  0    | 1 | 2 | 3 | 4 |
                // Then, the generated submeshes will be like this:
                // Materials: | 0 | 1 | 2 | 3 |
                // SubMehses: | 0 | 0 | 2 | 3 | 4 |
                // We've removed SubMesh 1 because it has no material and there is successor submesh with material.
                // We keep SubMesh 4 because there is no successor submesh.
                int lastNonEmptySubMeshIndex = -1;
                for (var i = 0; i < SubMeshes.Count; i++)
                {
                    var subMesh = SubMeshes[i];
                    if (subMesh.SharedMaterials.Length != 0)
                        lastNonEmptySubMeshIndex = i;
                }

                for (var i = 0; i < SubMeshes.Count - 1; i++)
                {
                    var subMesh = SubMeshes[i];

                    var canKeepEmptySubMesh = lastNonEmptySubMeshIndex < i;

                    // for non-last submesh, we have to duplicate submesh for multi pass rendering
                    AddSubmesh(subMesh, vertexIndices, submeshIndexBuffersList, ref submeshWithMoreThan65536Verts, Math.Max(subMesh.SharedMaterials.Length, canKeepEmptySubMesh ? 1 : 0));
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

                    var min = indices.Aggregate(0, Math.Min);
                    var max = indices.Aggregate(0, Math.Max);
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

                destMesh.subMeshCount = submeshIndexBuffers.Length;

                for (var i = 0; i < submeshIndexBuffers.Length; i++)
                {
                    var (subMesh, baseVertex, indices) = submeshIndexBuffers[i];
                    destMesh.SetIndices(indices, 0, subMesh.Vertices.Count, subMesh.Topology, i,
                        baseVertex: baseVertex);
                }
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

                Profiler.BeginSample("Collect by buffer");
                var buffers = new List<BlendShapeBuffer>();
                var verticesByBuffer = new List<List<(Vertex, int)>>();

                for (var i = 0; i < Vertices.Count; i++)
                {
                    var vertex = Vertices[i];
                    var buffer = vertex.BlendShapeBuffer;
                    var bufferIndex = buffers.IndexOf(buffer);
                    if (bufferIndex == -1)
                    {
                        bufferIndex = buffers.Count;
                        buffers.Add(buffer);
                        verticesByBuffer.Add(new List<(Vertex, int)>());
                    }

                    verticesByBuffer[bufferIndex].Add((vertex, i));
                }

                Profiler.EndSample();

                // Process normal blend shapes
                for (var i = 0; i < BlendShapes.Count; i++)
                {
                    Profiler.BeginSample("Process Shape");
                    Utils.Assert(destMesh.blendShapeCount == i, "Unexpected state: BlendShape count");
                    var (shapeName, _) = BlendShapes[i];

                    Profiler.BeginSample("Collect Weights");
                    var weightsSet = new HashSet<float>();

                    foreach (var blendShapeBuffer in buffers)
                        if (blendShapeBuffer.Shapes.TryGetValue(shapeName, out var shapeShape))
                            weightsSet.UnionWith(shapeShape.Frames.Select(x => x.Weight));

                    // blendShape with no weights is not allowed.
                    if (weightsSet.Count == 0)
                        weightsSet.Add(100);

                    var weights = weightsSet.ToArray();
                    Array.Sort(weights);

                    Profiler.EndSample();

                    Profiler.BeginSample("Make Frames Recipies");
                    var weightRecipes = buffers.Select(buffer =>
                    {
                        var frames = new ApplyFrame2Array[weights.Length];

                        for (var weightI = 0; weightI < weights.Length; weightI++)
                        {
                            var weight = weights[weightI];
                            frames[weightI] = buffer.GetApplyFramesInfo(shapeName, weight);
                        }

                        return frames;
                    }).ToArray();
                    Profiler.EndSample();

                    Profiler.BeginSample("Copy and Apply Frames");
                    var positions = new Vector3[Vertices.Count];
                    var normals = new Vector3[Vertices.Count];
                    var tangents = new Vector3[Vertices.Count];

                    for (var weightI = 0; weightI < weights.Length; weightI++)
                    {
                        var weight = weights[weightI];

                        for (var bufferIndex = 0; bufferIndex < verticesByBuffer.Count; bufferIndex++)
                        {
                            var vertices = verticesByBuffer[bufferIndex];
                            var recipe = weightRecipes[bufferIndex][weightI];
                            var buffer = buffers[bufferIndex];

                            if (recipe.FrameCount == 1 && recipe.FirstFrameApplyWeight == 1)
                            {
                                // likely fast path: just copy
                                Profiler.BeginSample("FastCopyPath");
                                var deltaVertices = buffer.DeltaVertices[recipe.FirstFrameIndex];
                                var deltaNormals = buffer.DeltaNormals[recipe.FirstFrameIndex];
                                var deltaTangents = buffer.DeltaTangents[recipe.FirstFrameIndex];

                                foreach (var (vertex, vertexI) in vertices)
                                {
                                    positions[vertexI] = deltaVertices[vertex.BlendShapeBufferVertexIndex];
                                    normals[vertexI] = deltaNormals[vertex.BlendShapeBufferVertexIndex];
                                    tangents[vertexI] = deltaTangents[vertex.BlendShapeBufferVertexIndex];
                                }
                                Profiler.EndSample();
                            }
                            else
                            {
                                Profiler.BeginSample("ApplyRecipe");
                                foreach (var (vertex, vertexI) in vertices)
                                {
                                    positions[vertexI] = recipe.Apply(buffer.DeltaVertices,
                                        vertex.BlendShapeBufferVertexIndex);
                                    normals[vertexI] = recipe.Apply(buffer.DeltaNormals,
                                        vertex.BlendShapeBufferVertexIndex);
                                    tangents[vertexI] = recipe.Apply(buffer.DeltaTangents,
                                        vertex.BlendShapeBufferVertexIndex);
                                }
                                Profiler.EndSample();
                            }
                        }

                        destMesh.AddBlendShapeFrame(shapeName, weight, positions, normals, tangents);
                    }
                    Profiler.EndSample();
                    Profiler.EndSample();
                }
                Profiler.EndSample();
            }

            // Add dummy blend shape for SkinnedMesh without blend shapes and without bone weights
            if (isSkinnedMesh && BlendShapes.Count == 0 && !Vertices.Any(x => x.BoneWeights.Count != 0))
            {
                Profiler.BeginSample("DummyBlendShape");
                var positions = new Vector3[Vertices.Count];
                var normals = new Vector3[Vertices.Count];
                var tangents = new Vector3[Vertices.Count];
                // All arrays are initialized to zero by default, which is what we want for a dummy blend shape
                destMesh.AddBlendShapeFrame("AAO_DummyBlendShape", 100, positions, normals, tangents);
                Profiler.EndSample();
            }

            if (Vertices.Count != 0) {
                var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                foreach (var v in Vertices)
                {
                    if (float.IsFinite(v.Position.x)) min.x = Math.Min(min.x, v.Position.x);
                    if (float.IsFinite(v.Position.y)) min.y = Math.Min(min.y, v.Position.y);
                    if (float.IsFinite(v.Position.z)) min.z = Math.Min(min.z, v.Position.z);
                    if (float.IsFinite(v.Position.x)) max.x = Math.Max(max.x, v.Position.x);
                    if (float.IsFinite(v.Position.y)) max.y = Math.Max(max.y, v.Position.y);
                    if (float.IsFinite(v.Position.z)) max.z = Math.Max(max.z, v.Position.z);
                }

                var bounds = new Bounds();
                bounds.SetMinMax(min, max);
                destMesh.bounds = bounds;
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

                    WriteToMesh(mesh, isSkinnedMesh: true);
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
                WriteToMesh(mesh, isSkinnedMesh: false);
                if (_originalMesh) ObjectRegistry.RegisterReplacedObject(_originalMesh, mesh);
                meshFilter.sharedMesh = mesh;
                targetRenderer.sharedMaterials = SubMeshes.SelectMany(x => x.SharedMaterials).ToArray();
            }
        }

        public override string ToString() =>
            SourceRenderer ? $"MeshInfo2({SourceRenderer})" : $"MeshInfo2(Not Belong to Renderer)";

        /// <summary>
        /// Removes vertices that are not used in any submesh.
        /// </summary>
        public void RemoveUnusedVertices()
        {
            Profiler.BeginSample("Purge Unused Vertices");
            var usedVertices = new HashSet<Vertex>();
            foreach (var subMesh in SubMeshes)
            foreach (var vertex in subMesh.Vertices)
                usedVertices.Add(vertex);

            var removed = new List<Vertex>();
            VerticesMutable.RemoveAll(x =>
            {
                // orphan vertex are likely used for bounds calculation.
                // Therefore, we keep orphan vertex even if it is not used in any submesh.
                var remove = !usedVertices.Contains(x) && !x.IsOrphanVertex;
                if (remove) removed.Add(x);
                return remove;
            });
            Utils.DisposeAll(removed);
            Profiler.EndSample();
        }

        public void Dispose()
        {
            Utils.DisposeAll(VerticesMutable);
            VerticesMutable.Clear();
        }
    }

    public class SubMesh
    {
        public readonly MeshTopology Topology = MeshTopology.Triangles;

        // size of this must be 3 * n
        public List<Vertex> Triangles
        {
            get
            {
                Utils.Assert(Topology == MeshTopology.Triangles);
                return Vertices;
            }
        }

        // borrowed from MeshInfo2
        public List<Vertex> Vertices { get; } = new List<Vertex>();

        public Material? SharedMaterial
        {
            get => SharedMaterials[0];
            set
            {
                if (SharedMaterials.Length == 0)
                    SharedMaterials = new Material?[1];
                SharedMaterials[0] = value;
            }
        }

        public Material?[] SharedMaterials = Array.Empty<Material?>();

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

        public SubMesh(IReadOnlyList<Vertex> vertices, List<int> triangles, SubMeshDescriptor descriptor)
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

    public class Vertex : IDisposable
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

        private RefCountField<BlendShapeBuffer> _blendShapeBuffer = BlendShapeBuffer.Empty;

        public BlendShapeBuffer BlendShapeBuffer
        {
            get => _blendShapeBuffer.Value;
            set => _blendShapeBuffer.Value = value;
        }

        public int BlendShapeBufferVertexIndex;

        public bool IsOrphanVertex = false;

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
            var frames = BlendShapeBuffer.GetApplyFramesInfo(name, weight, getDefined);

            var bufferIndex = BlendShapeBufferVertexIndex;

            position = frames.Apply(BlendShapeBuffer.DeltaVertices, bufferIndex);
            normal = frames.Apply(BlendShapeBuffer.DeltaNormals, bufferIndex);
            tangent = frames.Apply(BlendShapeBuffer.DeltaTangents, bufferIndex);

            return frames.FrameCount != 0;
        }

        internal Vertex()
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
            BlendShapeBuffer = vertex.BlendShapeBuffer;
            BlendShapeBufferVertexIndex = vertex.BlendShapeBufferVertexIndex;
            IsOrphanVertex = vertex.IsOrphanVertex;
        }

        public Vertex Clone() => new Vertex(this);

        public Vector3 ComputeActualPosition(MeshInfo2 meshInfo2, Func<Transform, Matrix4x4> getLocalToWorld, Matrix4x4 rendererWorldToLocalMatrix)
        {
            var position = Position;

            // first, apply BlendShapes
            foreach (var (name, weight) in meshInfo2.BlendShapes)
                if (TryGetBlendShape(name, weight, out var posDelta, out _, out _))
                    position += posDelta;

            if (BoneWeights.Count == 0) return position;

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

        public void Dispose()
        {
            _blendShapeBuffer.Dispose();
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
    
    /// <summary>
    /// The class to make blendshape manipulation faster
    ///
    /// In general, MeshInfo2 will store data with reference / name form instead of index form for easy manipulation.
    ///
    /// However, for blendshape-heavy meash, this is too slow so this class will store data with index form.
    ///
    /// This class is generally immutable except for removing blendShapes because adding data would require creating new array.
    /// </summary>
    public class BlendShapeBuffer : IReferenceCount
    {
        public Dictionary<string, BlendShapeShape> Shapes { get; } = new();
        public readonly NativeArray<Vector3>[] DeltaVertices;
        public readonly NativeArray<Vector3>[] DeltaNormals;
        public readonly NativeArray<Vector3>[] DeltaTangents;
        public readonly int VertexCount;

        public BlendShapeBuffer(Mesh sourceMesh, List<(string name, float weight)> blendShapes)
        {
            Profiler.BeginSample("BlendShapeBuffer:Create");
            var totalFrames = 0;
            for (var i = 0; i < sourceMesh.blendShapeCount; i++)
                totalFrames += sourceMesh.GetBlendShapeFrameCount(i);

            VertexCount = sourceMesh.vertexCount;
            var vertexCount = sourceMesh.vertexCount;

            DeltaVertices = new NativeArray<Vector3>[totalFrames];
            DeltaNormals = new NativeArray<Vector3>[totalFrames];
            DeltaTangents = new NativeArray<Vector3>[totalFrames];

            var readVertices = new Vector3[vertexCount];
            var readNormals = new Vector3[vertexCount];
            var readTangents = new Vector3[vertexCount];

            var frameIndex = 0;
            for (var blendShapeIndex = 0; blendShapeIndex < sourceMesh.blendShapeCount; blendShapeIndex++)
            {
                Profiler.BeginSample("Process Shape");
                var name = sourceMesh.GetBlendShapeName(blendShapeIndex);
                var frameCount = sourceMesh.GetBlendShapeFrameCount(blendShapeIndex);

                var frameInfos = new BlendShapeFrameInfo[frameCount];

                for (var blendShapeFrameIndex = 0; blendShapeFrameIndex < frameCount; blendShapeFrameIndex++)
                {
                    Profiler.BeginSample("Process Frame");

                    Profiler.BeginSample("GetBlendShapeFrameVertices");
                    sourceMesh.GetBlendShapeFrameVertices(blendShapeIndex, blendShapeFrameIndex, 
                        readVertices, readNormals, readTangents);
                    Profiler.EndSample();

                    Profiler.BeginSample("SaveToBuffer");
                    DeltaVertices[frameIndex] = new NativeArray<Vector3>(readVertices, Allocator.TempJob);
                    DeltaNormals[frameIndex] = new NativeArray<Vector3>(readNormals, Allocator.TempJob);
                    DeltaTangents[frameIndex] = new NativeArray<Vector3>(readTangents, Allocator.TempJob);
                    Profiler.EndSample();

                    Profiler.BeginSample("GetBlendShapeFrameWeight");
                    var weight = sourceMesh.GetBlendShapeFrameWeight(blendShapeIndex, blendShapeFrameIndex);
                    frameInfos[blendShapeFrameIndex] = new BlendShapeFrameInfo(weight, frameIndex);
                    Profiler.EndSample();

                    frameIndex++;
                    Profiler.EndSample();
                }

                if (!Shapes.TryAdd(name, new BlendShapeShape(frameInfos)))
                {
                    // duplicated blendShape name detected.
                    // This can be generated with 3ds Max or other tools.
                    // Rename blendShape a little to avoid conflict.
                    name = $"{name}-nameConflict-{GetShortRandom()}";
                    Shapes.Add(name, new BlendShapeShape(frameInfos));
                }
                blendShapes.Add((name, 0.0f));
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }

        private static string GetShortRandom()
        {
            // generate 4-char base64 string
            // with 4 of 64 characters, it has 64^4 = 16777216 possibilities.
            // When we create 100 times, the possibility of collision is one in about 3390
            var chars = new char[4];

            for (var i = 0; i < chars.Length; i++)
                chars[i] = Base64Char(Random.Range(0, 64));
            return new string(chars);

            static char Base64Char(int value) => value switch
            {
                < 0 => throw new ArgumentOutOfRangeException(nameof(value), $"{value}"),
                < 10 => (char)('0' + value),
                < 36 => (char)('A' + value - 10),
                < 62 => (char)('a' + value - 36),
                62 => '-',
                63 => '_',
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"{value}"),
            };
        }

        // create empty
        private BlendShapeBuffer()
        {
            DeltaVertices = Array.Empty<NativeArray<Vector3>>();
            DeltaNormals = Array.Empty<NativeArray<Vector3>>();
            DeltaTangents = Array.Empty<NativeArray<Vector3>>();
        }

        static BlendShapeBuffer()
        {
            Empty = new BlendShapeBuffer();
            RefCount.Increment(Empty);
        }

        public ApplyFrame2Array GetApplyFramesInfo(string shapeName, float weight, bool getDefined = false) =>
            Shapes.TryGetValue(shapeName, out var shape) ? shape.GetApplyFramesInfo(weight, getDefined) : default;

        public static BlendShapeBuffer Empty { get; }

        public void RemoveBlendShape(string name) => Shapes.Remove(name);

        ReferenceCount IReferenceCount.ReferenceCount { get; } = new();

        public void Dispose()
        {
            Utils.DisposeAll(DeltaVertices.Concat(DeltaNormals).Concat(DeltaTangents));
        }
    }

    public class BlendShapeShape
    {
        public readonly BlendShapeFrameInfo[] Frames;

        public BlendShapeShape(BlendShapeFrameInfo[] frames)
        {
            if (frames.Length == 0) throw new ArgumentException("frames must not be empty", nameof(frames));
            // Frames must be sorted by weight
            Frames = frames;
        }

        public int FrameCount => Frames.Length;
        public IEnumerable<int> FramesBufferIndices => Frames.Select(x => x.BufferIndex);
        
        public ApplyFrame2Array GetApplyFramesInfo(
            float weight,
            bool getDefined = false)
        {
            if (!getDefined && Mathf.Abs(weight) <= 0.0001f && DoNotApplyIfWeightIsZero())
            {
                return new ApplyFrame2Array();
            }

            if (Frames.Length == 1)
            {
                // likely: single frame blendshape
                var frame = Frames[0];
                var ratio = weight / frame.Weight;

                return new ApplyFrame2Array(frame.BufferIndex, ratio);
            }
            else
            {
                // multi frame blendshape

                var firstFrame = Frames[0];
                var lastFrame = Frames[^1];

                // if all weights are positive and the weight is less than first weight: lerp 0..first (similar to single frame)
                if (firstFrame.Weight > 0 && weight < firstFrame.Weight)
                {
                    return new ApplyFrame2Array(firstFrame.BufferIndex, weight / firstFrame.Weight);
                }

                // if all weights are negative and the weight is more than last weight: lerp last..0 (similar to single frame)
                if (lastFrame.Weight < 0 && weight > lastFrame.Weight)
                {
                    return new ApplyFrame2Array(lastFrame.BufferIndex, weight / lastFrame.Weight);
                }

                

                // otherwise, lerp between two surrounding frames OR nearest two frames

                for (var i = 1; i < Frames.Length; i++)
                {
                    if (weight <= Frames[i].Weight)
                        return BuildInfoWithTwoFrames(Frames[i - 1], Frames[i], weight);
                }

                return BuildInfoWithTwoFrames(Frames[^2], Frames[^1], weight);

            }
        }

        ApplyFrame2Array BuildInfoWithTwoFrames(BlendShapeFrameInfo first, BlendShapeFrameInfo second, float weight)
        {
            var ratio = InverseLerpUnclamped(first.Weight, second.Weight, weight);

            return new ApplyFrame2Array(
                first.BufferIndex, 1 - ratio,
                second.BufferIndex, ratio);

            static float InverseLerpUnclamped(float a, float b, float value) => (value - a) / (b - a);
        }

        bool DoNotApplyIfWeightIsZero()
        {
            if (Frames.Length == 1) return true;

            var first = Frames[0];
            var end = Frames[^1];

            // both weight are same sign, zero for 0 weight
            if (first.Weight <= 0 && end.Weight <= 0) return true;
            if (first.Weight >= 0 && end.Weight >= 0) return true;

            return false;
        }
    }

    public readonly struct BlendShapeFrameInfo
    {
        public readonly float Weight;
        public readonly int BufferIndex;

        public BlendShapeFrameInfo(float weight, int bufferIndex)
        {
            Weight = weight;
            BufferIndex = bufferIndex;
        }
    }

    public readonly struct ApplyFrame2Array : IEnumerable<ApplyFrameInfo>
    {
        // To make default(ApplyFramesInfo) as nothing to apply, those indices are bit-inverted.
        private readonly int _firstFrameIndexInverted;
        private readonly float _firstFrameApplyWeight;
        private readonly int _secondFrameIndexInverted;
        private readonly float _secondFrameApplyWeight;

        // -1 means first frame is not to be applied
        public int FirstFrameIndex => ~_firstFrameIndexInverted;
        public float FirstFrameApplyWeight => _firstFrameApplyWeight;
        // -1 means second frame is not to be applied
        public int SecondFrameIndex => ~_secondFrameIndexInverted;
        public float SecondFrameApplyWeight => _secondFrameApplyWeight;

        public int FrameCount => FirstFrameIndex == -1 ? 0 : SecondFrameIndex == -1 ? 1 : 2;


        public ApplyFrameInfo this[int index]
        {
            get
            {
                if (index < 0 || index >= FrameCount) throw new IndexOutOfRangeException();
                return index switch
                {
                    0 => new ApplyFrameInfo(FirstFrameIndex, FirstFrameApplyWeight),
                    1 => new ApplyFrameInfo(SecondFrameIndex, SecondFrameApplyWeight),
                    _ => throw new IndexOutOfRangeException()
                };
            }
        }

        public ApplyFrame2Array(int firstFrameIndex, float firstFrameApplyWeight)
        {
            // optimization: if first frame is not to be applied, we can skip first frame
            if (firstFrameApplyWeight == 0)
            {
                this = default;
            }
            else
            {
                _firstFrameIndexInverted = ~firstFrameIndex;
                _firstFrameApplyWeight = firstFrameApplyWeight;
                _secondFrameIndexInverted = ~-1;
                _secondFrameApplyWeight = 0;
            }
        }

        public ApplyFrame2Array(int firstFrameIndex, float firstFrameApplyWeight, int secondFrameIndex,
            float secondFrameApplyWeight)
        {
            // optimization: if either frame is not to be applied, we can skip that frame
            if (firstFrameApplyWeight == 0)
            {
                this = new ApplyFrame2Array(secondFrameIndex, secondFrameApplyWeight);
            }
            else if (secondFrameApplyWeight == 0)
            {
                this = new ApplyFrame2Array(firstFrameIndex, firstFrameApplyWeight);
            }
            else
            {

                _firstFrameIndexInverted = ~firstFrameIndex;
                _firstFrameApplyWeight = firstFrameApplyWeight;
                _secondFrameIndexInverted = ~secondFrameIndex;
                _secondFrameApplyWeight = secondFrameApplyWeight;
            }
        }

        public Enumerator GetEnumerator() => new(this);
        IEnumerator<ApplyFrameInfo> IEnumerable<ApplyFrameInfo>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<ApplyFrameInfo>
        {
            private ApplyFrame2Array _frames;
            private int _index;

            public Enumerator(ApplyFrame2Array frames)
            {
                _frames = frames;
                _index = -1;
            }

            public ApplyFrameInfo Current => _frames[_index];
            object IEnumerator.Current => Current;

            public bool MoveNext() => ++_index < _frames.FrameCount;
            public void Reset() => _index = -1;

            void IDisposable.Dispose()
            {
            }
        }

        public Vector3 Apply(NativeArray<Vector3>[] deltas, int bufferIndex)
        {
            var delta = Vector3.zero;
            if (FirstFrameIndex == -1) return delta;
            delta += deltas[FirstFrameIndex][bufferIndex] * FirstFrameApplyWeight;
            if (SecondFrameIndex == -1) return delta;
            delta += deltas[SecondFrameIndex][bufferIndex] * SecondFrameApplyWeight;
            return delta;
        }
    }
    
    public readonly struct ApplyFrameInfo
    {
        public readonly int FrameIndex;
        public readonly float ApplyWeight;

        public ApplyFrameInfo(int frameIndex, float applyWeight) =>
            (FrameIndex, ApplyWeight) = (frameIndex, applyWeight);

        public void Deconstruct(out int frameIndex, out float applyWeight) =>
            (frameIndex, applyWeight) = (FrameIndex, ApplyWeight);
    }
}
#endif
