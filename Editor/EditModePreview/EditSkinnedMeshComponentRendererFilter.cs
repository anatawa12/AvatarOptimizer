using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using nadena.dev.ndmf.preview;
using nadena.dev.ndmf.rq;
using nadena.dev.ndmf.rq.unity.editor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    internal class EditSkinnedMeshComponentRendererFilter : IRenderFilter
    {
        public static EditSkinnedMeshComponentRendererFilter Instance { get; } =
            new EditSkinnedMeshComponentRendererFilter();

        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext ctx)
        {
            // currently remove meshes are only supported
            var rmInBox = ctx.GetComponentsByType<RemoveMeshInBox>();
            var rmByBlendShape = ctx.GetComponentsByType<RemoveMeshByBlendShape>();
            var rmByMask = ctx.GetComponentsByType<RemoveMeshByMask>();

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

            return targets.Select(RenderGroup.For).ToImmutableList();
        }

        public async Task<IRenderFilterNode> Instantiate(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
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

        public RenderAspects Reads => RenderAspects.Mesh | RenderAspects.Shapes;
        public RenderAspects WhatChanged => RenderAspects.Mesh | RenderAspects.Shapes;

        delegate bool ShouldRemovePolygon(Span<int> indices);

        public async Task Process(
            SkinnedMeshRenderer original,
            SkinnedMeshRenderer proxy,
            [CanBeNull] RemoveMeshInBox rmInBox,
            [CanBeNull] RemoveMeshByBlendShape rmByBlendShape,
            [CanBeNull] RemoveMeshByMask rmByMask,
            ComputeContext context)
        {
            var duplicated = Object.Instantiate(proxy.sharedMesh);
            duplicated.name = proxy.sharedMesh.name + " (AAO Generated)";

            var materialSettings = rmByMask?.materials;
            for (var subMeshI = 0; subMeshI < duplicated.subMeshCount; subMeshI++)
            {
                List<ShouldRemovePolygon> checks = new List<ShouldRemovePolygon>();

                if (materialSettings != null && subMeshI < materialSettings.Length)
                {
                    var materialSetting = materialSettings[subMeshI];
                    if (!materialSetting.enabled) continue;
                    if (materialSetting.mask == null) continue;
                    if (!materialSetting.mask.isReadable) continue;

                    var editingTexture = MaskTextureEditor.Window.ObservePreviewTextureFor(original, subMeshI, context);
                    var mask = editingTexture ? editingTexture : context.Observe(materialSetting.mask);
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

                    var uv = duplicated.uv;

                    switch (materialSetting.mode)
                    {
                        case RemoveMeshByMask.RemoveMode.RemoveWhite:
                            checks.Add(indices =>
                            {
                                foreach (var index in indices)
                                {
                                    if (!(GetValue(uv[index].x, uv[index].y) > 127))
                                    {
                                        return false;
                                    }
                                }

                                return true;
                            });
                            break;
                        case RemoveMeshByMask.RemoveMode.RemoveBlack:
                            checks.Add(indices =>
                            {
                                foreach (var index in indices)
                                {
                                    if (!(GetValue(uv[index].x, uv[index].y) <= 127))
                                    {
                                        return false;
                                    }
                                }

                                return true;
                            });
                            break;
                        default:
                            BuildLog.LogError("RemoveMeshByMask:error:unknownMode");
                            continue;
                    }
                }

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

                var triangles = duplicated.GetTriangles(subMeshI);
                var modifiedTriangles = new List<int>(triangles.Length);

                for (var primitiveI = 0; primitiveI < triangles.Length; primitiveI += vertexPerPrimitive)
                {
                    if (!ShouldRemovePolygonAny(checks, triangles.AsSpan().Slice(primitiveI, vertexPerPrimitive)))
                    {
                        for (var vertexI = 0; vertexI < vertexPerPrimitive; vertexI++)
                            modifiedTriangles.Add(triangles[primitiveI + vertexI]);
                    }

                    continue;

                    bool ShouldRemovePolygonAny(List<ShouldRemovePolygon> checkList, Span<int> indices)
                    {
                        foreach (var shouldRemovePolygon in checkList)
                            if (shouldRemovePolygon(indices))
                                return true;

                        return false;
                    }
                }

                duplicated.SetTriangles(modifiedTriangles, subMeshI);
            }

            proxy.sharedMesh = duplicated;
            _duplicated = duplicated;
        }

        public Task<IRenderFilterNode> Refresh(IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context, RenderAspects updatedAspects)
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
