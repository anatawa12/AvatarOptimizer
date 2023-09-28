using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class MeshInfo2Holder
    {
        private readonly Dictionary<SkinnedMeshRenderer, MeshInfo2> _skinnedCache =
            new Dictionary<SkinnedMeshRenderer, MeshInfo2>();

        private readonly Dictionary<MeshRenderer, MeshInfo2> _staticCache = new Dictionary<MeshRenderer, MeshInfo2>();

        public MeshInfo2Holder(GameObject rootObject)
        {
            foreach (var renderer in rootObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                GetMeshInfoFor(renderer);
            
            foreach (var renderer in rootObject.GetComponentsInChildren<MeshRenderer>(true))
                GetMeshInfoFor(renderer);
        }

        public MeshInfo2 GetMeshInfoFor(SkinnedMeshRenderer renderer) =>
            _skinnedCache.TryGetValue(renderer, out var cached)
                ? cached
                : _skinnedCache[renderer] = new MeshInfo2(renderer);


        public MeshInfo2 GetMeshInfoFor(MeshRenderer renderer) =>
            _staticCache.TryGetValue(renderer, out var cached)
                ? cached
                : _staticCache[renderer] = new MeshInfo2(renderer);

        public void SaveToMesh()
        {
            foreach (var keyValuePair in _skinnedCache)
            {
                var targetRenderer = keyValuePair.Key;
                if (!targetRenderer) continue;

                keyValuePair.Value.WriteToSkinnedMeshRenderer(targetRenderer);
            }

            foreach (var keyValuePair in _staticCache)
            {
                var targetRenderer = keyValuePair.Key;
                if (!targetRenderer) continue;
                var meshInfo = keyValuePair.Value;
                var meshFilter = targetRenderer.GetComponent<MeshFilter>();

                BuildReport.ReportingObject(targetRenderer, () =>
                {
                    var mesh = new Mesh { name = $"AAOGeneratedMesh{targetRenderer.name}" };
                    meshInfo.WriteToMesh(mesh);
                    meshFilter.sharedMesh = mesh;
                    targetRenderer.sharedMaterials = meshInfo.SubMeshes.Select(x => x.SharedMaterial).ToArray();
                });
            }
        }
    }
}
