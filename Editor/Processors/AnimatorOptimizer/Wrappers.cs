using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;

// Animator Optimizer AnimatorController Wrapper classes

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer
{
    class AOAnimatorController
    {
        private AnimatorController _animatorController;

        public AOAnimatorController([NotNull] AnimatorController animatorController)
        {
            if (!animatorController) throw new ArgumentNullException(nameof(animatorController));
            _animatorController = animatorController;
            layers = _animatorController.layers.Select(x => new AOAnimatorControllerLayer(x)).ToArray();
            foreach (var layer in layers)
            {
                var syncedLayer = layer.syncedLayerIndex;
                if (syncedLayer != -1) layers[syncedLayer].IsSyncedToOtherLayer = true;
            }
        }

        // ReSharper disable InconsistentNaming
        public AnimatorControllerParameter[] parameters
        {
            get => _animatorController.parameters;
            set => _animatorController.parameters = value;
        }

        // do not assign to this field
        public AOAnimatorControllerLayer[] layers { get; private set; }
        // ReSharper restore InconsistentNaming
    }

    class AOAnimatorControllerLayer
    {
        private readonly AnimatorControllerLayer _layer;

        public AOAnimatorControllerLayer([NotNull] AnimatorControllerLayer layer) =>
            _layer = layer ?? throw new ArgumentNullException(nameof(layer));

        public bool IsSynced => _layer.syncedLayerIndex != -1;
        public bool IsSyncedToOtherLayer = false;

        // ReSharper disable InconsistentNaming
        public int syncedLayerIndex => _layer.syncedLayerIndex;
        public AnimatorStateMachine stateMachine => _layer.stateMachine ? _layer.stateMachine : null;
        public string name => _layer.name;
        // ReSharper restore InconsistentNaming
    }
}
