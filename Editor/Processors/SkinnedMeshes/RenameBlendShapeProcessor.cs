using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class RenameBlendShapeProcessor : EditSkinnedMeshProcessor<RenameBlendShape>
    {
        public RenameBlendShapeProcessor(RenameBlendShape component) : base(component)
        {
        }

        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.AfterFreezeBlendShape;

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            var mapping = CollectBlendShapeSources(target.BlendShapes, Component.nameMap.GetAsMap());
            var weightMap = target.BlendShapes.ToDictionary(x => x.name, x => x.weight);
            WarnErrorConflicts(mapping, weightMap, context);
            SetTemporalNameForEmpty(mapping);
            DoRenameBlendShapes(target, mapping);
        }

        private void SetTemporalNameForEmpty(List<(string name, float weight, List<string> sources)> mapping)
        {
            var usingNames = new HashSet<string>(mapping.Select(x => x.name));

            var index = 0;

            string NewName()
            {
                while (true)
                {
                    var name = $"AAO-EmptyName-Placeholder-{index}";
                    index++;
                    if (!usingNames.Contains(name))
                        return name;
                }
            }

            for (var i = 0; i < mapping.Count; i++)
            {
                var tuple = mapping[i];
                if (tuple.name == "")
                    mapping[i] = tuple with { name = NewName() };
            }
        }

        public static void DoRenameBlendShapes(MeshInfo2 target, List<(string name, float weight, List<string> sources)> mapping)
        {
            foreach (var vertexPerBuffer in target.Vertices.GroupBy(x => x.BlendShapeBuffer))
            {
                var buffer = vertexPerBuffer.Key;
                var newShapes = new Dictionary<string, BlendShapeShape>();

                foreach (var (newName, _, sources) in mapping)
                {
                    if (sources.Count == 1)
                    {
                        // just rename
                        if (buffer.Shapes.Remove(sources[0], out var shapeShape))
                            newShapes.Add(newName, shapeShape);
                    }
                    else
                    {
                        // merge multiple blendshapes
                        var frames = MergeBlendShape(buffer, sources);
                        newShapes.Add(newName, new BlendShapeShape(frames));
                    }
                }

                // reinitialize buffer
                buffer.Shapes.Clear();
                foreach (var (name, shape) in newShapes)
                    buffer.Shapes.Add(name, shape);
            }

            target.BlendShapes.Clear();
            target.BlendShapes.AddRange(mapping.Select(x => (x.name, x.weight)));
        }

        private static BlendShapeFrameInfo[] MergeBlendShape(BlendShapeBuffer buffer, List<string> sources)
        {
            // merging
            var originalShapes = new List<BlendShapeShape>();

            foreach (var source in sources)
                if (buffer.Shapes.Remove(source, out var shapeShape))
                    originalShapes.Add(shapeShape);

            var weights = originalShapes
                .SelectMany(x => x.Frames)
                .Select(x => x.Weight)
                .Distinct()
                .ToArray();

            Array.Sort(weights);

            if (originalShapes.All(x => x.Frames.Length == weights.Length))
            {
                // single frame optimization
                var bufferSize = buffer.DeltaVertices[0].Length;

                var rootShapeShape = originalShapes[0];

                for (var frameIndex = 0; frameIndex < rootShapeShape.Frames.Length; frameIndex++)
                {
                    var targetBufferIndex = rootShapeShape.Frames[frameIndex].BufferIndex;

                    foreach (var blendShapeShape in originalShapes.Skip(1))
                    {
                        var index = blendShapeShape.Frames[frameIndex].BufferIndex;
                        for (var j = 0; j < bufferSize; j++)
                        {
                            buffer.DeltaVertices[targetBufferIndex][j] += buffer.DeltaVertices[index][j];
                            buffer.DeltaNormals[targetBufferIndex][j] += buffer.DeltaNormals[index][j];
                            buffer.DeltaTangents[targetBufferIndex][j] += buffer.DeltaTangents[index][j];
                        }
                    }
                }

                return rootShapeShape.Frames;
            }
            else
            {
                // multiple weights, we need to calculate intermediate frames
                var applyFrameInfos = new List<ApplyFrameInfo>[weights.Length];

                for (var i = 0; i < weights.Length; i++)
                {
                    var weight = weights[i];
                    var list = applyFrameInfos[i] = new List<ApplyFrameInfo>();

                    foreach (var shapeShape in originalShapes)
                        list.AddRange(shapeShape.GetApplyFramesInfo(weight, true));
                }

                // we create new buffer for each frame
                var bufferSize = buffer.DeltaNormals[0].Length;
                var positionBuffers = new Vector3[weights.Length][];
                var normalBuffers = new Vector3[weights.Length][];
                var tangentBuffers = new Vector3[weights.Length][];

                for (var i = 0; i < weights.Length; i++)
                {
                    var frameInfo = applyFrameInfos[i];

                    // copy with apply
                    var positionBuffer = positionBuffers[i] = new Vector3[bufferSize];
                    var normalBuffer = normalBuffers[i] = new Vector3[bufferSize];
                    var tangentBuffer = tangentBuffers[i] = new Vector3[bufferSize];

                    for (var j = 0; j < bufferSize; j++)
                    {
                        var position = Vector3.zero;
                        var normal = Vector3.zero;
                        var tangent = Vector3.zero;

                        foreach (var info in frameInfo)
                        {
                            var index = info.FrameIndex;
                            var weight = info.ApplyWeight;

                            position += buffer.DeltaVertices[index][j] * weight;
                            normal += buffer.DeltaNormals[index][j] * weight;
                            tangent += buffer.DeltaTangents[index][j] * weight;
                        }

                        positionBuffer[j] = position;
                        normalBuffer[j] = normal;
                        tangentBuffer[j] = tangent;
                    }
                }

                // save to buffer
                var bufferIndices = originalShapes.SelectMany(x => x.Frames).Take(weights.Length).ToArray();
                var frames = new BlendShapeFrameInfo[weights.Length];

                for (var i = 0; i < weights.Length; i++)
                {
                    var index = bufferIndices[i].BufferIndex;
                    var weight = weights[i];
                    frames[i] = new BlendShapeFrameInfo(weight, index);
                    buffer.DeltaVertices[index] = positionBuffers[i];
                    buffer.DeltaNormals[index] = normalBuffers[i];
                    buffer.DeltaTangents[index] = tangentBuffers[i];
                }
                return frames;
            }
        }

        private void WarnErrorConflicts(List<(string name, float weight, List<string> sources)> mapping, Dictionary<string, float> weightMap, BuildContext context)
        {
            var animationComponent = context.GetAnimationComponent(Target);

            // check for weight / animation conflicts
            // weight conflicts are error; animation conflicts can be false positive so warning
            foreach (var (name, _, sources) in mapping)
            {
                if (name == "")
                {
                    BuildLog.LogError("RenameBlendShape:error:empty-name", string.Join(", ", sources));
                    continue; // empty name should not be checked
                }

                if (sources.Count <= 1) continue; // will not conflict
             
                // check for weights
                if (sources.Select(x => weightMap[x]).Distinct().Count() > 1)
                    BuildLog.LogWarning("RenameBlendShape:warning:weight-conflict", name, string.Join(", ", sources));

                // animation checks (we only check for isAnimated, for now
                var partiallyAnimated = sources.Select(source => animationComponent.TryGetFloat($"blendShape.{source}", out _)).Distinct().Count() > 1;
                if (partiallyAnimated)
                    BuildLog.LogWarning("RenameBlendShape:warning:animation-conflict", name, string.Join(", ", sources));
            }            
        }

        public static List<(string name, float weight, List<string> sources)> CollectBlendShapeSources(
            IEnumerable<(string, float)> blendShapes, Dictionary<string, string?> mapping)
        {
            var newList = new List<(string, float, List<string> sources)>();

            foreach (var (name, weight) in blendShapes)
            {
                if (!mapping.TryGetValue(name, out var newName) || newName == null)
                    newName = name;

                var found = newList.Find(x => x.Item1 == newName);
                if (newName == "")
                {
                    // empty name should be kept as is and not merged
                    newList.Add((newName, weight, new List<string> { name }));
                }
                else if (found.sources == null)
                {
                    // new one
                    newList.Add((newName, weight, new List<string> { name }));
                }
                else
                {
                    // merging to existing
                    found.sources.Add(name);
                }
            }

            return newList;
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this, upstream);

        class MeshInfoComputer : AbstractMeshInfoComputer
        {
            private readonly RenameBlendShapeProcessor _processor;

            public MeshInfoComputer(RenameBlendShapeProcessor processor, IMeshInfoComputer upstream) : base(upstream)
                => _processor = processor;

            public override (string, float)[] BlendShapes()
            {
                return CollectBlendShapeSources(base.BlendShapes(), _processor.Component.nameMap.GetAsMap())
                    .Select(x => (x.name, x.weight))
                    .ToArray();
            }
        }
    }
}
