using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ndmf;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors;

/// <summary>
/// VRCSDK 3.10.6 added VRC Native Face Tracking.
/// VRC Native Face Tracking specifies blendshapes for Face Tracking by their names,
/// but it does not allow specifying different BlendShape Name for each Skinned Mesh Renderers.
/// Therefore, since AAO 1.9.21, that adds VRCSDK 3.10.6 support, We introduced BlendShapeGroup internal feature.
/// The AAO will tries to name BlendShapes in same BlendShapeGroup the same name.
/// Detailed behavior is not fully defined in edge cases like name conflicts due to manual rename & merge,
/// so this is not publicly available feature.
/// </summary>
internal class ApplyBlendShapeGroup : Pass<ApplyBlendShapeGroup>
{
    protected override void Execute(BuildContext context)
    {
        if (!context.GetState<AAOEnabled>().Enabled) return;

        var groupState = context.GetState<BlendShapeGroupState>();

        var nodeLookup = new Dictionary<AnimationPropertyInfo<PropertyInfo>, BlendShapeGroupNode>();
        foreach (var groupDefinition in groupState.BlendShapeGroup)
        {
            BlendShapeGroupNode? groupNode = null;
            foreach (var blendShapeProp in groupDefinition)
            {
                var mapped = blendShapeProp.MappedProperty;
                if (mapped.Component == null || mapped.Name == null) continue;
                if (!nodeLookup.TryGetValue(mapped, out var node))
                    nodeLookup.Add(mapped, node = new BlendShapeGroupNode(
                        ParseBlendShapeProperty(mapped.Name),
                        (SkinnedMeshRenderer)mapped.Component.TargetComponent.Value));
                node.OriginalNames.Add(ParseBlendShapeProperty(blendShapeProp.Name!));
                groupNode ??= node;
                UnionFind.Merge(groupNode, node);
            }
        }

        var nodesByGroup = new Dictionary<BlendShapeGroupNode, List<BlendShapeGroupNode>>();
        foreach (var blendShapeGroupNode in nodeLookup.Values)
        {
            var root = blendShapeGroupNode.FindRoot();
            if (!nodesByGroup.TryGetValue(root, out var nodeList))
                nodesByGroup.Add(root, nodeList = new());
            nodeList.Add(blendShapeGroupNode);
        }

        var groupId = 0;

        foreach (var groupNodes in nodesByGroup.Values)
        {
            if (groupNodes.Count <= 1) continue; // No Need to merge

            var groupByRenderer = groupNodes.GroupBy(x => x.Renderer).ToList();
            if (groupByRenderer.Any(x => x.Count() > 1))
            {
                BuildLog.LogError("BlendShapeGroup:error:BlendShapesMerged",
                    string.Join(", ", groupNodes.SelectMany(x => x.OriginalNames).Distinct()));
            }

            var groupShapeName = $"AAO-BlendShapeGroup-{groupId++}-{string.Join("", groupNodes.Select(x => $"-{x.MappedBlendShapeName}-"))}";

            foreach (var group in groupByRenderer)
            {
                var renderer = group.Key;
                var originalName = group.First().MappedBlendShapeName;

                var meshInfo2 = context.GetMeshInfoFor(renderer);
                var index = meshInfo2.BlendShapes.FindIndex(x => x.name == originalName);

                if (index != -1) meshInfo2.BlendShapes[index] = (groupShapeName, meshInfo2.BlendShapes[index].weight);

                foreach (var vertexPerBuffer in meshInfo2.Vertices.Select(x => x.BlendShapeBuffer).Distinct())
                {
                    if (vertexPerBuffer.Shapes.Remove(originalName, out var shapeShape))
                        vertexPerBuffer.Shapes.Add(groupShapeName, shapeShape);
                }

                context.RecordMoveProperty(renderer, $"blendShape.{originalName}", $"blendShape.{groupShapeName}");
            }
        }
    }

    protected static string ParseBlendShapeProperty(string prop) =>
        prop.StartsWith("blendShape.", StringComparison.Ordinal)
            ? prop.Substring("blendShape.".Length)
            : throw new Exception("invalid blendShape property");

    private class BlendShapeGroupNode : UnionFindNode<BlendShapeGroupNode>
    {
        public readonly List<string> OriginalNames = new();
        public readonly string MappedBlendShapeName;
        public readonly SkinnedMeshRenderer Renderer;

        public BlendShapeGroupNode(string mappedBlendShapeName, SkinnedMeshRenderer renderer)
        {
            MappedBlendShapeName = mappedBlendShapeName;
            Renderer = renderer;
        }
    }
}
