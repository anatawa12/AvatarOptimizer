using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using nadena.dev.ndmf.preview;
using nadena.dev.ndmf.rq;
using nadena.dev.ndmf.rq.unity.editor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class EditSkinnedMeshComponentRendererFilter : IRenderFilter
    {
        public static EditSkinnedMeshComponentRendererFilter Instance { get; } =
            new EditSkinnedMeshComponentRendererFilter();

        public ReactiveValue<IImmutableList<IImmutableList<Renderer>>> TargetGroups { get; }

        public EditSkinnedMeshComponentRendererFilter()
        {
            TargetGroups = ReactiveValue<IImmutableList<IImmutableList<Renderer>>>.Create(
                "anatawa12.avatar-optimizer.EditSkinnedMeshComponentRendererFilter.TargetGroups",
                CollectTarget);
        }

        private async Task<IImmutableList<IImmutableList<Renderer>>> CollectTarget(ComputeContext ctx)
        {
            // currently remove meshes are only supported
            var rmInBoxTask = ctx.Observe(CommonQueries.GetComponentsByType<RemoveMeshInBox>());
            var rmByBlendShapeTask = ctx.Observe(CommonQueries.GetComponentsByType<RemoveMeshByBlendShape>());
            var rmByMaskTask = ctx.Observe(CommonQueries.GetComponentsByType<RemoveMeshByMask>());

            await Task.WhenAll(rmInBoxTask, rmByBlendShapeTask, rmByMaskTask);

            var rmInBox = rmInBoxTask.Result;
            var rmByBlendShape = rmByBlendShapeTask.Result;
            var rmByMask = rmByMaskTask.Result;

            var targets = new HashSet<Renderer>();

            foreach (var component in rmInBox.Concat<EditSkinnedMeshComponent>(rmByBlendShape).Concat(rmByMask))
            {
                if (component.GetComponent<MergeSkinnedMesh>())
                {
                    // the component applies to MergeSkinnedMesh, which is not supported for now
                    // TODO: rollup the remove operation to source renderers of MergeSkinnedMesh
                    continue;
                }

                var renderer = component.GetComponent<SkinnedMeshRenderer>();
                if (renderer == null) continue;
                if (renderer.sharedMesh == null) continue;

                targets.Add(renderer);
            }

            return targets.Select(r => (IImmutableList<Renderer>)ImmutableList.Create(r)).ToImmutableList();
        }

        public async Task<IRenderFilterNode> Instantiate(IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            var pair = proxyPairs.Single();
            if (!(pair.Item1 is SkinnedMeshRenderer original)) return null;
            if (!(pair.Item2 is SkinnedMeshRenderer proxy)) return null;

            // we modify the mesh so we need to clone the mesh

            var rmInBox = context.Observe(context.GetComponent<RemoveMeshInBox>(original.gameObject));
            var rmByBlendShape = context.Observe(context.GetComponent<RemoveMeshByBlendShape>(original.gameObject));
            var rmByMask = context.Observe(context.GetComponent<RemoveMeshByMask>(original.gameObject));

            var node = new EditSkinnedMeshComponentRendererNode();

            await node.Process(original, proxy, rmInBox, rmByBlendShape, rmByMask, context);

            return node;
        }
    }

    internal class EditSkinnedMeshComponentRendererNode : IRenderFilterNode
    {
        private Mesh _duplicated;

        public EditSkinnedMeshComponentRendererNode()
        {
        }

        public ulong Reads => IRenderFilterNode.Mesh | IRenderFilterNode.Shapes;
        public ulong WhatChanged => IRenderFilterNode.Mesh | IRenderFilterNode.Shapes;

        public async Task Process(
            SkinnedMeshRenderer original,
            SkinnedMeshRenderer proxy,
            RemoveMeshInBox rmInBox,
            RemoveMeshByBlendShape rmByBlendShape,
            RemoveMeshByMask rmByMask, 
            ComputeContext context)
        {
            var duplicated = Object.Instantiate(proxy.sharedMesh);
            duplicated.name = proxy.sharedMesh.name + " (AAO Generated)";

            if (rmByMask)
            {
                var materialSettings = rmByMask.materials;
                for (var subMeshI = 0; subMeshI < duplicated.subMeshCount && subMeshI < materialSettings.Length; subMeshI++)
                {
                    var materialSetting = materialSettings[subMeshI];
                    if (!materialSetting.enabled) continue;
                    if (materialSetting.mask == null) continue;
                    if (!materialSetting.mask.isReadable) continue;

                    var subMesh = duplicated.GetSubMesh(subMeshI);
                    int vertexPerPrimitive;
                    switch (subMesh.topology)
                    {
                        case MeshTopology.Triangles:
                            vertexPerPrimitive = 3;
                            break;
                        case MeshTopology.Quads:
                            vertexPerPrimitive = 4;
                            break;
                        case MeshTopology.Lines:
                            vertexPerPrimitive = 2;
                            break;
                        case MeshTopology.Points:
                            vertexPerPrimitive = 1;
                            break;
                        case MeshTopology.LineStrip:
                        default:
                            // unsupported topology
                            continue;
                    }

                    // TODO: use texture from Texture Editor
                    var mask = context.Observe(materialSetting.mask);
                    var textureWidth = mask.width;
                    var textureHeight = mask.height;
                    var pixels = mask.GetPixels32();
                    
                    int GetValue(float u, float v)
                    {
                        var x = Mathf.RoundToInt(v % 1 * textureHeight);
                        var y = Mathf.RoundToInt(u % 1 * textureWidth);
                        var pixel = pixels[x * textureWidth + y];
                        return Mathf.Max(Mathf.Max(pixel.r, pixel.g), pixel.b);
                    }

                    Func<float, float, bool> isRemoved;

                    switch (materialSetting.mode)
                    {
                        case RemoveMeshByMask.RemoveMode.RemoveWhite:
                            isRemoved = (u, v) => GetValue(u, v) > 127;
                            break;
                        case RemoveMeshByMask.RemoveMode.RemoveBlack:
                            isRemoved = (u, v) => GetValue(u, v) <= 127;
                            break;
                        default:
                            BuildLog.LogError("RemoveMeshByMask:error:unknownMode");
                            continue;
                    }

                    var triangles = duplicated.GetTriangles(subMeshI);
                    var modifiedTriangles = new List<int>(triangles.Length);
                    var uv = duplicated.uv;

                    for (var primitiveI = 0; primitiveI < triangles.Length; primitiveI += vertexPerPrimitive)
                    {
                        var removed = Enumerable.Range(0, vertexPerPrimitive)
                            .All(vertexI => isRemoved(
                                uv[triangles[primitiveI + vertexI]].x,
                                uv[triangles[primitiveI + vertexI]].y));

                        if (!removed)
                        {
                            for (var vertexI = 0; vertexI < vertexPerPrimitive; vertexI++)
                                modifiedTriangles.Add(triangles[primitiveI + vertexI]);
                        }
                    }

                    duplicated.SetTriangles(modifiedTriangles, subMeshI);
                }
            }

            proxy.sharedMesh = duplicated;
            _duplicated = duplicated;
        }

        public Task<IRenderFilterNode> Refresh(IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context, ulong updateFlags)
        {
            return Task.FromResult<IRenderFilterNode>(null);
        }

        public void OnFrame(Renderer original, Renderer proxy)
        {
            if (_duplicated == null) return;
            if (proxy is SkinnedMeshRenderer skinnedMeshProxy)
                skinnedMeshProxy.sharedMesh = _duplicated;
        }

        public void Dispose()
        {
            if (_duplicated != null)
            {
                Object.DestroyImmediate(_duplicated);
                _duplicated = null;
            }
        }
    }
}
