using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;

public class AutoMergeBlendShape: TraceAndOptimizePass<AutoMergeBlendShape>
{
    public override string DisplayName => "T&O: Auto Merge Blend Shape";

    protected override void Execute(BuildContext context, TraceAndOptimizeState state)
    {
        if (!state.OptimizeBlendShape) return;
        if (state.SkipAutoMergeBlendShape) return;

        foreach (var skinnedMeshRenderer in context.GetComponents<SkinnedMeshRenderer>())
        {
            if (state.Exclusions.Contains(skinnedMeshRenderer.gameObject)) continue; // manual exclusion

            ErrorReport.WithContextObject(skinnedMeshRenderer, () => DoAutoMerge(context.GetMeshInfoFor(skinnedMeshRenderer), context));
        }
    }

    private static void DoAutoMerge(MeshInfo2 meshInfo2, BuildContext context)
    {
        var animationComponent = context.GetAnimationComponent(meshInfo2.SourceRenderer);
        var groups = new Dictionary<MergeKey, List<string>>();

        foreach (var (name, weight) in meshInfo2.BlendShapes)
        {
            if (MergeKey.Create(weight, name, animationComponent) is { } key)
            {
                if (!groups.TryGetValue(key, out var list))
                    groups.Add(key, list = new List<string>());
                list.Add(name);
            }
        }

        // nothing to merge
        if (groups.Values.All(x => x.Count <= 1)) return;

        // prepare merge
        var buffers = meshInfo2.Vertices.Select(x => x.BlendShapeBuffer).ToArray();

        // bulk remove to optimize removing blendshape process
        var removeNames = new HashSet<string>();

        var i = 0;
        // there is thing to merge
        foreach (var (key, names) in groups)
        {
            // validate the blendShapes are simple enough to merge
            // if not, skip
            foreach (var buffer in buffers)
            {
                float? commonFrameWeight = null;

                foreach (var name in names)
                {
                    if (buffer.Shapes.TryGetValue(name, out var shapeShape))
                    {
                        if (shapeShape.FrameCount != 1) goto next_shape;
                        var frameWeight = shapeShape.Frames[0].Weight;
                        if (commonFrameWeight is { } common)
                        {
                            if (!common.Equals(frameWeight)) goto next_shape;
                        }
                        else
                        {
                            commonFrameWeight = frameWeight;
                        }
                    }
                }
            }

            // validation passed, merge
            var newName = $"AAO_Merged_{string.Join("_", names)}_{i++}";

            // process meshInfo2
            meshInfo2.BlendShapes.Add((newName, key.defaultWeight));
            removeNames.UnionWith(names);

            // actually merge data
            foreach (var buffer in buffers)
            {
                BlendShapeShape? newShapeShape = null;

                foreach (var name in names)
                {
                    if (buffer.Shapes.Remove(name, out var shapeShape))
                    {
                        if (newShapeShape == null)
                        {
                            newShapeShape = shapeShape;
                        }
                        else
                        {
                            var vertices = new MergeBlendShapeJob
                            {
                                vertices = buffer.DeltaVertices[newShapeShape.Frames[0].BufferIndex],
                                toMerge = buffer.DeltaVertices[shapeShape.Frames[0].BufferIndex],
                            }.Schedule(buffer.VertexCount, 64);
                            var normals = new MergeBlendShapeJob
                            {
                                vertices = buffer.DeltaNormals[newShapeShape.Frames[0].BufferIndex],
                                toMerge = buffer.DeltaNormals[shapeShape.Frames[0].BufferIndex],
                            }.Schedule(buffer.VertexCount, 64);
                            var tangents = new MergeBlendShapeJob
                            {
                                vertices = buffer.DeltaTangents[newShapeShape.Frames[0].BufferIndex],
                                toMerge = buffer.DeltaTangents[shapeShape.Frames[0].BufferIndex],
                            }.Schedule(buffer.VertexCount, 64);
                            JobHandle.CombineDependencies(vertices, normals, tangents).Complete();
                        }
                    }
                }

                // null means there are no shapes to merge for this buffer
                if (newShapeShape != null)
                {
                    buffer.Shapes.Add(newName, newShapeShape);
                }
            }

            next_shape: ;
        }

        // remove merged blendShapes
        meshInfo2.BlendShapes.RemoveAll(x => removeNames.Contains(x.name));
    }

    readonly struct MergeKey : IEquatable<MergeKey>
    {
        public readonly float defaultWeight;
        public readonly EqualsHashSet<AnimationLocation> animationLocations;

        public MergeKey(float defaultWeight, EqualsHashSet<AnimationLocation> animationLocations)
        {
            this.defaultWeight = defaultWeight;
            this.animationLocations = animationLocations;
        }

        public static MergeKey? Create(float defaultWeight, string name, AnimationComponentInfo<PropertyInfo> animationComponent)
        {
            var node = animationComponent.GetFloatNode($"blendShape.{name}");
            // to merge, all nodes must be AnimatorPropModNode
            if (!node.ComponentNodes.All(x => x is AnimatorPropModNode<FloatValueInfo>)) return null;
            var animationLocations = AnimationLocation.CollectAnimationLocation(node).ToEqualsHashSet();
            return new MergeKey(defaultWeight, animationLocations);
        }

        public bool Equals(MergeKey other) =>
            defaultWeight.Equals(other.defaultWeight) &&
            animationLocations.Equals(other.animationLocations);

        public override bool Equals(object? obj) => obj is MergeKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(defaultWeight, animationLocations);
        public static bool operator ==(MergeKey left, MergeKey right) => left.Equals(right);
        public static bool operator !=(MergeKey left, MergeKey right) => !left.Equals(right);
    }

    struct MergeBlendShapeJob : IJobParallelFor
    {
        public NativeArray<Vector3> vertices;
        public NativeArray<Vector3> toMerge;

        public void Execute(int index) => vertices[index] += toMerge[index];
    }
}
