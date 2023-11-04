using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class MeshInfo2Context : IExtensionContext
    {
        [CanBeNull] public MeshInfo2Holder Holder { get; private set; }
        public void OnActivate(BuildContext context)
        {
            Holder = new MeshInfo2Holder(context.AvatarRootObject);
        }

        public void OnDeactivate(BuildContext context)
        {
            Debug.Assert(Holder != null, nameof(Holder) + " != null");
            Holder.SaveToMesh();
            Holder = null;
        }
    }

    internal class MeshInfo2Holder
    {
        private readonly Dictionary<SkinnedMeshRenderer, MeshInfo2> _skinnedCache =
            new Dictionary<SkinnedMeshRenderer, MeshInfo2>();

        private readonly Dictionary<MeshRenderer, MeshInfo2> _staticCache = new Dictionary<MeshRenderer, MeshInfo2>();

        public MeshInfo2Holder(GameObject rootObject)
        {
            var avatarTagComponent = rootObject.GetComponentInChildren<AvatarTagComponent>(true);
            if (avatarTagComponent == null) return;
            foreach (var renderer in rootObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Profiler.BeginSample($"Read Skinned Mesh");
                GetMeshInfoFor(renderer);
                Profiler.EndSample();
            }
            
            foreach (var renderer in rootObject.GetComponentsInChildren<MeshRenderer>(true))
            {
                Profiler.BeginSample($"Read Static Mesh");
                GetMeshInfoFor(renderer);
                Profiler.EndSample();
            }
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

                Profiler.BeginSample($"Save Skinned Mesh {targetRenderer.name}");
                keyValuePair.Value.WriteToSkinnedMeshRenderer(targetRenderer);
                Profiler.EndSample();
            }

            foreach (var keyValuePair in _staticCache)
            {
                var targetRenderer = keyValuePair.Key;
                if (!targetRenderer) continue;
                Profiler.BeginSample($"Save Static Mesh {targetRenderer.name}");
                keyValuePair.Value.WriteToMeshRenderer(targetRenderer);
                Profiler.EndSample();
            }
        }
    }
}
