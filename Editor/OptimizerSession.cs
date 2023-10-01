using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.Processors;
using Anatawa12.AvatarOptimizer.ndmf;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal class OptimizerSession
    {
        private readonly BuildContext _buildContext;
        public bool IsTest { get; }
        public ObjectMappingBuilder MappingBuilder => _buildContext.Extension<ObjectMappingContext>().MappingBuilder;
        private MeshInfo2Holder MeshInfo2Holder => _buildContext.Extension<MeshInfo2Context>().Holder;

        public MeshInfo2 GetMeshInfoFor(SkinnedMeshRenderer renderer) => MeshInfo2Holder.GetMeshInfoFor(renderer);
        public MeshInfo2 GetMeshInfoFor(MeshRenderer renderer) => MeshInfo2Holder.GetMeshInfoFor(renderer);

        public static implicit operator OptimizerSession(BuildContext context)
        {
            return context.Extension<OptimizerContext>().session;
        }

        public OptimizerSession(BuildContext context)
        {
            IsTest = false;
            _buildContext = context;
        }

        public IEnumerable<T> GetComponents<T>() where T : Component => _buildContext.GetComponents<T>();
    }
}
