using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes.Blendshape
{
    /// <summary>
    /// Holds information about where in the native storage arrays a particular blend shape maps to
    /// </summary>
    internal struct BlendshapeDescriptor
    {
        public int StartFrame, NumFrames;
        /// <summary>
        /// True if any delta value for any frame is nonzero
        /// </summary>
        public bool IsMeaningful;
    }
    
    /// <summary>
    /// Holds the native arrays used to store all blendshape data
    /// </summary>
    internal class BlendshapeNativeStorage : IDisposable
    {
        public delegate JobHandle LaunchJobDelegate();

        private bool IsValid = true;
        private List<JobHandle> ActiveJobs = new List<JobHandle>();
        
        public int NumVertices { get; }

        // These arrays are organized by shape, frame, then vertex. These are also dense; zero-delta vertices are
        // represented in these arrays.
        private NativeArray<Vector3> _positions, _normals, _tangents;
        private NativeArray<float> _frameWeights;
        private NativeArray<BlendshapeDescriptor> _shapeDescriptors;
        
        public NativeArray<Vector3> DeltaPositions {
            get
            {
                if (!IsValid) throw new ObjectDisposedException(nameof(BlendshapeNativeStorage));
                return _positions;
            }
        }
        
        public NativeArray<Vector3> DeltaNormals {
            get
            {
                if (!IsValid) throw new ObjectDisposedException(nameof(BlendshapeNativeStorage));
                return _normals;
            }
        }
        
        public NativeArray<Vector3> DeltaTangents {
            get
            {
                if (!IsValid) throw new ObjectDisposedException(nameof(BlendshapeNativeStorage));
                return _tangents;
            }
        }
        
        public NativeArray<float> FrameWeights {
            get
            {
                if (!IsValid) throw new ObjectDisposedException(nameof(BlendshapeNativeStorage));
                return _frameWeights;
            }
        }
        
        public NativeArray<BlendshapeDescriptor> ShapeDescriptors {
            get
            {
                if (!IsValid) throw new ObjectDisposedException(nameof(BlendshapeNativeStorage));
                return _shapeDescriptors;
            }
        }

        public BlendshapeNativeStorage(Mesh m)
        {
            NumVertices = m.vertexCount;
            
            int frames = 0;
            int shapes = m.blendShapeCount;
            for (int s = 0; s < shapes; s++)
            {
                frames += m.GetBlendShapeFrameCount(s);
            }
            
            _positions = new NativeArray<Vector3>(m.vertexCount * frames, Allocator.TempJob);
            _normals = new NativeArray<Vector3>(m.vertexCount * frames, Allocator.TempJob);
            _tangents = new NativeArray<Vector3>(m.vertexCount * frames, Allocator.TempJob);
            _frameWeights = new NativeArray<float>(frames, Allocator.TempJob);
            _shapeDescriptors = new NativeArray<BlendshapeDescriptor>(shapes, Allocator.TempJob);
            
            EditorApplication.delayCall += Dispose;
        }

        public BlendshapeNativeStorage(int verticesCount, BlendshapeDescriptor[] shapeDescriptors, float[] weights)
        {
            NumVertices = verticesCount;
            
            _shapeDescriptors = new NativeArray<BlendshapeDescriptor>(shapeDescriptors, Allocator.TempJob);
            _frameWeights = new NativeArray<float>(weights, Allocator.TempJob);
            
            _positions = new NativeArray<Vector3>(verticesCount * weights.Length, Allocator.TempJob);
            _normals = new NativeArray<Vector3>(verticesCount * weights.Length, Allocator.TempJob);
            _tangents = new NativeArray<Vector3>(verticesCount * weights.Length, Allocator.TempJob);
            
            EditorApplication.delayCall += Dispose;
        }

        /// <summary>
        /// Records that a job is in progress, so that disposal can wait for it
        /// </summary>
        /// <param name="h"></param>
        /// <returns></returns>
        public JobHandle LaunchJob(JobHandle h)
        {
            ActiveJobs.Add(h);
            return h;
        }
        
        public void WaitForAllJobs()
        {
            foreach (var job in ActiveJobs)
            {
                job.Complete();
            }
            ActiveJobs.Clear();
        }

        ~BlendshapeNativeStorage()
        {
            Dispose();
        }
        
        public void Dispose()
        {
            if (!IsValid) return;
            
            IsValid = false;
            WaitForAllJobs();
            
            _positions.Dispose();
            _normals.Dispose();
            _tangents.Dispose();
            _frameWeights.Dispose();
            _shapeDescriptors.Dispose();
            
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// This class tracks blendshape data from a mesh in a compact and burst-compatible format.
    /// </summary>
    internal class BlendshapeInfo
    {
        private BlendshapeNativeStorage Storage;
        
        private Dictionary<string, int> _shapeNameToIndex = new Dictionary<string, int>();
        private List<string> _shapeNames = new List<string>();

        public int InitialVertexCount => Storage.NumVertices;

        struct MergeRange
        {
            public BlendshapeInfo sourceInfo;
            public int startSrcVertex, startDstVertex, vertexCount;
        }
        
        private BlendshapeInfo(List<Vertex> vertices)
        {
            // Assign shape indexes
            AssignMergeShapeIndexes(vertices);
            
            // Determine vertex index mappings (and update vertex fields)
            Dictionary<BlendshapeInfo, List<MergeRange>> mappingRanges = AssignMergeIndices(vertices);
            
            // Now determine what frames exist
            // This is a mapping of dest shape index -> [weight => frame index]
            // We also generate shape descriptors in this step
            MergeFrames(mappingRanges, out var shapeDescriptors, out var weights);
            
            // Allocate sufficient space for the merged data
            Storage = new BlendshapeNativeStorage(vertices.Count, shapeDescriptors, weights);
            
            JobHandle.CombineDependencies(
                new ZeroFill(Storage.DeltaPositions).Schedule(Storage.DeltaPositions.Length, 1024),
                new ZeroFill(Storage.DeltaNormals).Schedule(Storage.DeltaNormals.Length, 1024),
                new ZeroFill(Storage.DeltaTangents).Schedule(Storage.DeltaTangents.Length, 1024)
            ).Complete();
            
            CopyFrames(mappingRanges);
        }

        private void CopyFrames(Dictionary<BlendshapeInfo, List<MergeRange>> mappingRanges)
        {
            for (int s = 0; s < _shapeNames.Count; s++)
            {
                var desc = Storage.ShapeDescriptors[s];
                for (int f = 0; f < desc.NumFrames; f++)
                {
                    CopySingleFrame(s, f, mappingRanges);
                }
            }
        }

        private void CopySingleFrame(int shapeIndex, int frame, Dictionary<BlendshapeInfo, List<MergeRange>> mappingRanges)
        {
            var shapeName = _shapeNames[shapeIndex];
            var desc = Storage.ShapeDescriptors[shapeIndex];

            int dstOffset = Storage.NumVertices * (desc.StartFrame + frame);

            foreach (var kvp in mappingRanges)
            {
                var srcShapes = kvp.Key;
                var ranges = kvp.Value;

                if (!srcShapes._shapeNameToIndex.TryGetValue(shapeName, out var srcIndex))
                {
                    continue;
                }

                var srcDesc = srcShapes.Storage.ShapeDescriptors[srcIndex];
                int srcOffset = srcShapes.Storage.NumVertices * (srcDesc.StartFrame + frame);

                foreach (var range in ranges)
                {
                    int srcStart = srcOffset + range.startSrcVertex;
                    int dstStart = dstOffset + range.startDstVertex;
                    int count = range.vertexCount;

                    Storage.DeltaPositions.Slice(dstStart, count).CopyFrom(srcShapes.Storage.DeltaPositions.Slice(srcStart, count));
                    Storage.DeltaNormals.Slice(dstStart, count).CopyFrom(srcShapes.Storage.DeltaNormals.Slice(srcStart, count));
                    Storage.DeltaTangents.Slice(dstStart, count).CopyFrom(srcShapes.Storage.DeltaTangents.Slice(srcStart, count));
                }
            }
        }

        private Dictionary<BlendshapeInfo, List<MergeRange>> AssignMergeIndices(List<Vertex> vertices)
        {
            MergeRange currentRange = new MergeRange();

            Dictionary<BlendshapeInfo, List<MergeRange>> rangeBuffer =
                new Dictionary<BlendshapeInfo, List<MergeRange>>();

            for (int v = 0; v < vertices.Count; v++)
            {
                var dstVertex = vertices[v];
                var srcIndex = dstVertex.Index;
                var srcInfo = dstVertex.BlendshapeInfo;

                if (currentRange.sourceInfo != srcInfo && currentRange.sourceInfo != null)
                {
                    if (!rangeBuffer.TryGetValue(currentRange.sourceInfo, out var ranges))
                    {
                        ranges = new List<MergeRange>();
                        rangeBuffer[currentRange.sourceInfo] = ranges;
                    }
                    ranges.Add(currentRange);
                    currentRange = new MergeRange();
                }
                
                if (currentRange.sourceInfo == null)
                {
                    currentRange.sourceInfo = srcInfo;
                    currentRange.startSrcVertex = srcIndex;
                    currentRange.startDstVertex = v;
                }

                currentRange.vertexCount++;

                dstVertex.BlendshapeInfo = this;
                dstVertex.Index = v;
            }
            
            if (currentRange.sourceInfo != null)
            {
                if (!rangeBuffer.TryGetValue(currentRange.sourceInfo, out var ranges))
                {
                    ranges = new List<MergeRange>();
                    rangeBuffer[currentRange.sourceInfo] = ranges;
                }
                ranges.Add(currentRange);
            }

            return rangeBuffer;
        }

        private void AssignMergeShapeIndexes(List<Vertex> vertices)
        {
            HashSet<BlendshapeInfo> visitedInfos = new HashSet<BlendshapeInfo>();

            foreach (var vertex in vertices)
            {
                if (!visitedInfos.Add(vertex.BlendshapeInfo))
                {
                    continue;
                }

                foreach (var shapeName in vertex.BlendshapeInfo._shapeNames)
                {
                    if (shapeName != null && !_shapeNameToIndex.ContainsKey(shapeName))
                    {
                        var index = _shapeNames.Count;
                        _shapeNames.Add(shapeName);
                        _shapeNameToIndex[shapeName] = index;
                    }
                }
            }
        }

        private void MergeFrames(
            Dictionary<BlendshapeInfo, List<MergeRange>> mappingRanges,
            out BlendshapeDescriptor[] shapeDescriptors,
            out float[] weights
        )
        {
            shapeDescriptors = new BlendshapeDescriptor[_shapeNames.Count];
            List<float> weightList = new List<float>();

            int nextFrameIndex = 0;
            for (int s = 0; s < _shapeNames.Count; s++)
            {
                BlendshapeDescriptor descriptor = new BlendshapeDescriptor()
                {
                    IsMeaningful = false,
                    StartFrame = nextFrameIndex,
                    NumFrames = 0
                };

                bool isFirst = true;
                foreach (var info in mappingRanges.Keys)
                {
                    if (!info._shapeNameToIndex.TryGetValue(_shapeNames[s], out var index))
                    {
                        continue;
                    }

                    var srcDesc = info.Storage.ShapeDescriptors[index];
                    
                    if (isFirst)
                    {
                        descriptor.NumFrames = srcDesc.NumFrames;
                        nextFrameIndex += srcDesc.NumFrames;
                    
                        shapeDescriptors[s] = descriptor;
                        
                        // Copy weights
                        for (int i = 0; i < srcDesc.NumFrames; i++)
                        {
                            weightList.Add(info.Storage.FrameWeights[srcDesc.StartFrame + i]);
                        }
                        
                        isFirst = false;
                    }
                    else
                    {
                        // Verify weights are consistent
                        if (descriptor.NumFrames != srcDesc.NumFrames)
                        {
                            BuildReport.LogWarning("MergeSkinnedMesh:warning:blendShapeWeightMismatch", _shapeNames[s]);
                            continue;
                        }

                        for (int f = 0; f < srcDesc.NumFrames; f++)
                        {
                            if (weightList[descriptor.StartFrame + f] != info.Storage.FrameWeights[srcDesc.StartFrame + f])
                            {
                                BuildReport.LogWarning("MergeSkinnedMesh:warning:blendShapeWeightMismatch", _shapeNames[s]);
                                break;
                            }
                        }
                    }
                }
            }

            weights = weightList.ToArray();
        }


        /// <summary>
        /// Constructs a BlendshapeInfo by merging blendshape data from vertices originally from multiple meshes.
        /// The input vertices will be updated to reference this new BlendshapeInfo. 
        /// </summary>
        /// <param name="mergedVertices"></param>
        public static BlendshapeInfo MergeVertices(List<Vertex> vertices)
        {
            return new BlendshapeInfo(vertices);
        }
        
        public BlendshapeInfo(Mesh mesh)
        {
            Storage = new BlendshapeNativeStorage(mesh);

            Vector3[] positions = new Vector3[mesh.vertexCount];
            Vector3[] normals = new Vector3[mesh.vertexCount];
            Vector3[] tangents = new Vector3[mesh.vertexCount];

            int nVerts = mesh.vertexCount;
            int nShapes = mesh.blendShapeCount;
            int offset = 0;
            for (int i = 0; i < nShapes; i++)
            {
                var name = mesh.GetBlendShapeName(i);
                _shapeNameToIndex[name] = i;
                _shapeNames.Add(name);

                BlendshapeDescriptor descriptor = new BlendshapeDescriptor();
                descriptor.StartFrame = offset;
                descriptor.NumFrames = mesh.GetBlendShapeFrameCount(i);
                descriptor.IsMeaningful = false;
                
                int nFrames = mesh.GetBlendShapeFrameCount(i);
                for (int f = 0; f < nFrames; f++)
                {
                    float weight = mesh.GetBlendShapeFrameWeight(i, f);
                    mesh.GetBlendShapeFrameVertices(i, f, positions, normals, tangents);
                    
                    Storage.DeltaPositions.Slice(offset * nVerts, nVerts).CopyFrom(positions);
                    Storage.DeltaNormals.Slice(offset * nVerts, nVerts).CopyFrom(normals);
                    Storage.DeltaTangents.Slice(offset * nVerts, nVerts).CopyFrom(tangents);

                    var frameWeights = Storage.FrameWeights;
                    frameWeights[offset] = weight;

                    offset++;
                }

                var descriptors = Storage.ShapeDescriptors;
                descriptors[i] = descriptor;
            }
        }

        /// <summary>
        /// Returns an enumerable of blendshape names which do not influence any bones.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetZeroInfluenceBlendshapes()
        {
            Storage.WaitForAllJobs();

            var descriptors = Storage.ShapeDescriptors;
            for (int i = 0; i < _shapeNames.Count; i++)
            {
                var desc = descriptors[i];
                desc.IsMeaningful = false;
                descriptors[i] = desc;
            }
            
            Storage.LaunchJob(new FindMeaningfulShapes()
            {
                nVertices = Storage.NumVertices,
                DeltaPositions = Storage.DeltaPositions,
                DeltaNormals = Storage.DeltaNormals,
                DeltaTangents = Storage.DeltaTangents,
                ShapeDescriptors = Storage.ShapeDescriptors
            }.Schedule(Storage.ShapeDescriptors.Length, 1)).Complete();
            
            for (int i = 0; i < _shapeNames.Count; i++)
            {
                var name = _shapeNames[i];
                var descriptor = Storage.ShapeDescriptors[i];

                if (name != null && !descriptor.IsMeaningful)
                {
                    yield return name;
                }
            }
        }

        /// <summary>
        /// Returns an enumerable of vertices affected by a specific blend shape.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sqrTolerance"></param>
        /// <returns></returns>
        public IEnumerable<int> VerticesAffectedByShape(string name, double sqrTolerance)
        {
            int shapeIndex = _shapeNameToIndex[name];
            var descriptor = Storage.ShapeDescriptors[shapeIndex];

            for (int v = 0; v < Storage.NumVertices; v++)
            {
                for (int f = descriptor.StartFrame; f < descriptor.StartFrame + descriptor.NumFrames; f++)
                {
                    int index = f * Storage.NumVertices + v;

                    if (Storage.DeltaPositions[index].sqrMagnitude > sqrTolerance)
                    {
                        yield return v;
                        break; // skip remaining frames
                    }
                }
            }
        } 

        /// <summary>
        /// Tries to find the interpolated blend shape delta for a given shape name, frame weight, and vertex.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="weight"></param>
        /// <param name="vertexIndex"></param>
        /// <param name="position"></param>
        /// <param name="normal"></param>
        /// <param name="tangent"></param>
        /// <returns></returns>
        public bool TryGetBlendshape(
            string name,
            float weight,
            int vertexIndex,
            out Vector3 position,
            out Vector3 normal,
            out Vector3 tangent
        )
        {
            if (!_shapeNameToIndex.TryGetValue(name, out var shapeIndex))
            {
                position = default;
                normal = default;
                tangent = default;
                
                return false;
            }
            
            var descriptor = Storage.ShapeDescriptors[shapeIndex];

            int nverts = Storage.NumVertices;
            int nframes = descriptor.NumFrames;
            int startFrame = descriptor.StartFrame;
            
            if (Mathf.Abs(weight) <= 0.0001f && ZeroForWeightZero())
            {
                position = Vector3.zero;
                normal = Vector3.zero;
                tangent = Vector3.zero;
                return true;
            }

            bool ZeroForWeightZero()
            {
                if (nframes == 1) return true;
                var first = Frame(0);
                var end = Frame(nframes - 1);

                // both weight are same sign, zero for 0 weight
                if (first.Weight <= 0 && end.Weight <= 0) return true;
                if (first.Weight >= 0 && end.Weight >= 0) return true;

                return false;
            }

            if (nframes == 1)
            {
                // simplest and likely
                var frame = Frame(0);
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

            (BlendshapeVertexFrame, BlendshapeVertexFrame) FindFrame()
            {
                var firstFrame = Frame(0);
                var lastFrame = Frame(nframes - 1);

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

                for (var i = 1; i < nframes; i++)
                {
                    if (weight <= Frame(i).Weight)
                        return (Frame(i-1), Frame(i));
                }

                return (Frame(nframes - 2), Frame(nframes - 1));
            }

            BlendshapeVertexFrame Frame(int f)
            {
                int index = (startFrame + f) * nverts + vertexIndex;
                
                return new BlendshapeVertexFrame(
                    Storage.DeltaPositions[index],
                    Storage.DeltaNormals[index],
                    Storage.DeltaTangents[index],
                    Storage.FrameWeights[startFrame + f]
                );
            }

            float InverseLerpUnclamped(float a, float b, float value) => (value - a) / (b - a);
        }

        /// <summary>
        /// Transforms all blendshape data based on an array of per-vertex affine transformations.
        /// </summary>
        /// <param name="perVertexTransforms">The array of affine transformations to apply. Ownership of this array is
        /// transferred to this function; this array will be deallocated asynchronously.</param>
        public void TransformVertexSpaces(NativeArray<Matrix4x4> perVertexTransforms)
        {
            if (perVertexTransforms.Length != InitialVertexCount)
            {
                throw new ArgumentException("Wrong vertex count for TransformVertexSpaces");
            }
            
            Storage.LaunchJob(new TransformVertices()
            {
                PerVertexTransforms = perVertexTransforms,
                DeltaPositions = Storage.DeltaPositions,
                DeltaNormals = Storage.DeltaNormals,
                DeltaTangents = Storage.DeltaTangents
            }.Schedule(Storage.DeltaPositions.Length, 32));
        }
        
        /// <summary>
        /// Marks a blendshape for deletion.
        /// </summary>
        /// <param name="name"></param>
        public void DeleteBlendshape(string name)
        {
            if (_shapeNameToIndex.TryGetValue(name, out var index))
            {
                _shapeNameToIndex.Remove(name);
                _shapeNames[index] = null;
            }
        }

        /// <summary>
        /// Saves all blendshape data to a mesh.
        /// </summary>
        /// <param name="destMesh"></param>
        /// <param name="vertices"></param>
        public void SaveToMesh(Mesh destMesh, List<Vertex> vertices)
        {
            Storage.WaitForAllJobs();
            destMesh.ClearBlendShapes();

            int nShapes = _shapeNameToIndex.Count;
            List<int> shapeIndices = _shapeNameToIndex.Values.OrderBy(i => i).ToList();
            
            // Vertices may have been deleted during processing, so create a lookup table to map between the different
            // indices
            int[] dstToSrcVertexIndex = new int[vertices.Count];
            int idx = 0;
            foreach (var v in vertices)
            {
                dstToSrcVertexIndex[idx++] = v.Index;
            }

            Vector3[] positions = new Vector3[dstToSrcVertexIndex.Length];
            Vector3[] normals = new Vector3[dstToSrcVertexIndex.Length];
            Vector3[] tangents = new Vector3[dstToSrcVertexIndex.Length];
            
            foreach (var nativeIndex in shapeIndices)
            {
                var descriptor = Storage.ShapeDescriptors[nativeIndex];
                for (int f = descriptor.StartFrame; f < descriptor.StartFrame + descriptor.NumFrames; f++)
                {
                    /*Storage.DeltaPositions.Slice(f * Mesh.vertexCount, Mesh.vertexCount).CopyTo(positions);
                    Storage.DeltaNormals.Slice(f * Mesh.vertexCount, Mesh.vertexCount).CopyTo(normals);
                    Storage.DeltaTangents.Slice(f * Mesh.vertexCount, Mesh.vertexCount).CopyTo(tangents);*/

                    // TODO: should this move to Burst? [might be best to wait until post-2021 when we can access the
                    // blendshape data directly from Burst]
                    for (int v = 0; v < dstToSrcVertexIndex.Length; v++)
                    {
                        var srcVertex = dstToSrcVertexIndex[v];
                        positions[v] = Storage.DeltaPositions[f * Storage.NumVertices + srcVertex];
                        normals[v] = Storage.DeltaNormals[f * Storage.NumVertices + srcVertex];
                        tangents[v] = Storage.DeltaTangents[f * Storage.NumVertices + srcVertex];
                    }
                    
                    destMesh.AddBlendShapeFrame(_shapeNames[nativeIndex], Storage.FrameWeights[f], 
                        positions, normals, tangents);
                }
            }
        }

        [BurstCompile]
        private struct TransformVertices : IJobParallelFor
        {
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Matrix4x4> PerVertexTransforms;
            
            public NativeArray<Vector3> DeltaPositions;
            public NativeArray<Vector3> DeltaNormals;
            public NativeArray<Vector3> DeltaTangents;
            
            public void Execute(int index)
            {
                // index here is the index into each of the Delta arrays
                int vertexIndex = index % PerVertexTransforms.Length;
                var transformation = PerVertexTransforms[vertexIndex];

                if (transformation.m33 < 0.1)
                {
                    // Uninitialized element, skip it
                    return;
                }
                
                var position = DeltaPositions[index];
                var normal = DeltaNormals[index];
                var tangent = DeltaTangents[index];
                
                DeltaPositions[index] = transformation.MultiplyPoint3x4(position);
                DeltaNormals[index] = transformation.MultiplyPoint3x3(normal);
                DeltaTangents[index] = transformation.MultiplyPoint3x3(tangent);
            }
        }

        [BurstCompile]
        private struct FindMeaningfulShapes : IJobParallelFor
        {
            public int nVertices;
            
            [ReadOnly] public NativeArray<Vector3> DeltaPositions;
            [ReadOnly] public NativeArray<Vector3> DeltaNormals;
            [ReadOnly] public NativeArray<Vector3> DeltaTangents;
            
            public NativeArray<BlendshapeDescriptor> ShapeDescriptors;
            
            public void Execute(int index)
            {
                var descriptor = ShapeDescriptors[index];

                int startIndex = descriptor.StartFrame * nVertices;
                int endIndex = startIndex + descriptor.NumFrames * nVertices;

                for (int i = startIndex; i < endIndex; i++)
                {
                    if (DeltaPositions[i] != Vector3.zero
                        || DeltaNormals[i] != Vector3.zero
                        || DeltaTangents[i] != Vector3.zero)
                    {
                        descriptor.IsMeaningful = true;
                        ShapeDescriptors[index] = descriptor;
                        break;
                    }
                }
            }
        }
        
        [BurstCompile]
        private struct ZeroFill : IJobParallelFor
        {
            [WriteOnly]
            public NativeArray<Vector3> Array;

            public ZeroFill(NativeArray<Vector3> array)
            {
                Array = array;
            }
            
            public void Execute(int index)
            {
                Array[index] = Vector3.zero;
            }
        }
    }
    
    internal struct BlendshapeVertexFrame
    {
        public Vector3 Position, Normal, Tangent;
        public float Weight; 
        
        public BlendshapeVertexFrame(Vector3 deltaPosition, Vector3 deltaNormal, Vector3 deltaTangent, float weight)
        {
            this.Position = deltaPosition;
            this.Normal = deltaNormal;
            this.Tangent = deltaTangent;
            this.Weight = weight;
        }
    }
}