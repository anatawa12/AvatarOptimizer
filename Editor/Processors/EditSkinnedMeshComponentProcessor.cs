using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class EditSkinnedMeshComponentProcessor
    {
        public void Process(OptimizerSession session)
        {
            var graph = new SkinnedMeshEditorSorter();
            foreach (var component in session.GetComponents<EditSkinnedMeshComponent>())
                graph.AddComponent(component);

            var holder = new MeshInfo2Holder();

            var renderers = session.GetComponents<SkinnedMeshRenderer>();
            var processorLists = graph.GetSortedProcessors(renderers);
            foreach (var processors in processorLists)
            {
                var target = holder.GetMeshInfoFor(processors.Target);

                foreach (var processor in processors.GetSorted())
                {
                    // TODO
                    BuildReport.ReportingObject(processor.Component, () => processor.Process(session, target, holder));
                    target.AssertInvariantContract(
                        $"after {processor.GetType().Name} " +
                        $"for {processor.Target.gameObject.name}");
                    Object.DestroyImmediate(processor.Component);
                }
            }

            holder.SaveToMesh(session);
        }
    }

    internal class MeshInfo2Holder
    {
        private readonly Dictionary<SkinnedMeshRenderer, MeshInfo2> _skinnedCache =
            new Dictionary<SkinnedMeshRenderer, MeshInfo2>();

        private readonly Dictionary<MeshRenderer, MeshInfo2> _staticCache = new Dictionary<MeshRenderer, MeshInfo2>();

        public MeshInfo2 GetMeshInfoFor(SkinnedMeshRenderer renderer) =>
            _skinnedCache.TryGetValue(renderer, out var cached)
                ? cached
                : _skinnedCache[renderer] = new MeshInfo2(renderer);


        public MeshInfo2 GetMeshInfoFor(MeshRenderer renderer) =>
            _staticCache.TryGetValue(renderer, out var cached)
                ? cached
                : _staticCache[renderer] = new MeshInfo2(renderer);

        public void SaveToMesh(OptimizerSession session)
        {
            foreach (var keyValuePair in _skinnedCache)
            {
                var targetRenderer = keyValuePair.Key;
                if (!targetRenderer) continue;

                keyValuePair.Value.WriteToSkinnedMeshRenderer(targetRenderer, session);
            }

            foreach (var keyValuePair in _staticCache)
            {
                var targetRenderer = keyValuePair.Key;
                if (!targetRenderer) continue;
                var meshInfo = keyValuePair.Value;
                var meshFilter = targetRenderer.GetComponent<MeshFilter>();

                var mesh = meshFilter.sharedMesh
                    ? session.MayInstantiate(meshFilter.sharedMesh)
                    : session.AddToAsset(new Mesh());
                meshInfo.WriteToMesh(mesh);
                meshFilter.sharedMesh = mesh;
                targetRenderer.sharedMaterials = meshInfo.SubMeshes.Select(x => x.SharedMaterial).ToArray();
            }
        }
    }
}
