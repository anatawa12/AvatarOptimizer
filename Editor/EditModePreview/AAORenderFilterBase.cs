using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using nadena.dev.ndmf.preview;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    internal abstract class AAORenderFilterBase<T> : IRenderFilter
        where T : EditSkinnedMeshComponent
    {
        private readonly TogglablePreviewNode _toggleNode;
        public IEnumerable<TogglablePreviewNode> GetPreviewControlNodes() => new[] { _toggleNode };
        public bool IsEnabled(ComputeContext context) => context.Observe(_toggleNode.IsEnabled);

        protected AAORenderFilterBase(string name, string component)
        {
            _toggleNode = TogglablePreviewNode.Create(() => name, $"com.anatawa12.avatar-optimizer.{component}");
        }

        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext ctx)
        {
            // currently remove meshes are only supported
            var components = ctx.GetComponentsByType<T>();

            var componentsByRenderer = new Dictionary<Renderer, List<T>>();
            foreach (var component in components)
            {
                if (component.TryGetComponent<MergeSkinnedMesh>(out _))
                {
                    // the component applies to MergeSkinnedMesh, which is not supported for now
                    // TODO: rollup the remove operation to source renderers of MergeSkinnedMesh
                    continue;
                }

                if (!component.TryGetComponent<SkinnedMeshRenderer>(out var renderer)) continue;
                if (renderer.sharedMesh == null) continue;

                if (!componentsByRenderer.TryGetValue(renderer, out var list))
                    componentsByRenderer.Add(renderer, list = new List<T>());

                list.Add(component);
            }

            return componentsByRenderer
                .Where(x => SupportsMultiple() ? x.Value.Count >= 1 : x.Value.Count == 1)
                .Select(pair => RenderGroup.For(pair.Key).WithData(pair.Value.ToArray()))
                .ToImmutableList();
        }

        public async Task<IRenderFilterNode?> Instantiate(RenderGroup group,
            IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            var pair = proxyPairs.Single();
            if (!(pair.Item1 is SkinnedMeshRenderer original)) return null;
            if (!(pair.Item2 is SkinnedMeshRenderer proxy)) return null;

            // we modify the mesh so we need to clone the mesh

            var components = group.GetData<T[]>();

            var node = CreateNode();

            await node.Process(original, proxy, components, context);

            return node;
        }

        protected abstract AAORenderFilterNodeBase<T> CreateNode();
        protected abstract bool SupportsMultiple();
    }

    internal abstract class AAORenderFilterNodeBase<T> : IRenderFilterNode
        where T : EditSkinnedMeshComponent
    {
        private Mesh? _duplicated;

        RenderAspects IRenderFilterNode.WhatChanged => RenderAspects.Mesh | RenderAspects.Shapes;

        protected abstract ValueTask Process(
            SkinnedMeshRenderer original,
            SkinnedMeshRenderer proxy,
            [NotNull] T[] components,
            Mesh duplicated,
            ComputeContext context);

        internal async ValueTask Process(
            SkinnedMeshRenderer original,
            SkinnedMeshRenderer proxy,
            [NotNull] T[] components,
            ComputeContext context)
        {
            UnityEngine.Profiling.Profiler.BeginSample($"RemoveMeshByBlendShapeRendererNode.Process({original.name})");

            var duplicated = Object.Instantiate(proxy.sharedMesh);
            duplicated.name = proxy.sharedMesh.name + " (AAO Generated)";

            await Process(original, proxy, components, duplicated, context);

            proxy.sharedMesh = duplicated;
            _duplicated = duplicated;

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void IRenderFilterNode.OnFrame(Renderer original, Renderer proxy)
        {
            if (_duplicated == null) return;
            if (proxy is SkinnedMeshRenderer skinnedMeshProxy)
                skinnedMeshProxy.sharedMesh = _duplicated;
        }

        void IDisposable.Dispose()
        {
            if (_duplicated != null)
            {
                Object.DestroyImmediate(_duplicated);
                _duplicated = null;
            }
        }
    }
}
