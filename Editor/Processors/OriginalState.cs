using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    /// <summary>
    /// The state that holds original transform matrix information
    ///
    /// This class assumes that same Transform instance is not used for another usage.
    /// </summary>
    internal class OriginalState
    {
        // avatar root => transform matrix (root.worldToLocalMatrix * transform.localToWorldMatrix)
        // we can local to world by root.localToWorldMatrix * thisMatrix
        private Dictionary<Transform, Matrix4x4> _originalTransforms = new Dictionary<Transform, Matrix4x4>();
        public Transform AvatarRoot { get; set; } = null!; // set by FetchOriginalStatePass

        public void Register(Transform transform)
        {
            _originalTransforms.Add(transform, AvatarRoot.worldToLocalMatrix * transform.localToWorldMatrix);
        }

        public Matrix4x4 GetOriginalLocalToWorld(Transform transform)
        {
            for (var current = transform; current != null && current != AvatarRoot; current = current.parent)
            {
                if (_originalTransforms.TryGetValue(current, out var matrix))
                    return AvatarRoot.localToWorldMatrix * matrix * current.worldToLocalMatrix * transform.localToWorldMatrix;
            }
            return transform.localToWorldMatrix;
        }
    }

    internal class FetchOriginalStatePass : Pass<FetchOriginalStatePass>
    {
        protected override void Execute(BuildContext context)
        {
            var originalState = context.GetState<OriginalState>();
            originalState.AvatarRoot = context.AvatarRootTransform;
            foreach (var component in context.GetComponents<Transform>())
            {
                originalState.Register(component);
            }
        }
    }
}
