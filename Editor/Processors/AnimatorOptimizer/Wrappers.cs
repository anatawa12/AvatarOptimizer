using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
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
                var syncedLayerIndex = layer.syncedLayerIndex;
                if (syncedLayerIndex != -1)
                {
                    var syncedLayer = layers[syncedLayerIndex];
                    layer.SyncedLayer = syncedLayer;
                    syncedLayer.IsSyncedToOtherLayer = true;
                }
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

        public AOAnimatorControllerLayer AddLayer(string layerName)
        {
            var layer = new AnimatorControllerLayer
            {
                name = layerName,
                stateMachine = new AnimatorStateMachine
                {
                    name = layerName,
                    hideFlags = HideFlags.HideInHierarchy
                }
            };
            var wrappedLayer = new AOAnimatorControllerLayer(layer);

            // add to controller
            var animatorControllerLayers = _animatorController.layers;
            ArrayUtility.Add(ref animatorControllerLayers, layer);
            _animatorController.layers = animatorControllerLayers;

            // update our layers
            var wrappedLayers = layers;
            ArrayUtility.Add(ref wrappedLayers, wrappedLayer);
            layers = wrappedLayers;

            return wrappedLayer;
        }
    }

    class AOAnimatorControllerLayer
    {
        private readonly AnimatorControllerLayer _layer;

        public AOAnimatorControllerLayer([NotNull] AnimatorControllerLayer layer) =>
            _layer = layer ?? throw new ArgumentNullException(nameof(layer));

        public bool IsSynced => _layer.syncedLayerIndex != -1;
        public bool IsSyncedToOtherLayer = false;
        [CanBeNull] public AOAnimatorControllerLayer SyncedLayer { get; internal set; }

        public AnimatorWeightChange WeightChange;

        // ReSharper disable InconsistentNaming
        public float defaultWeight
        {
            get => _layer.defaultWeight;
            set => _layer.defaultWeight = value;
        }

        public int syncedLayerIndex => _layer.syncedLayerIndex;
        public AnimatorStateMachine stateMachine => _layer.stateMachine ? _layer.stateMachine : null;
        public string name => _layer.name;
        // ReSharper restore InconsistentNaming

        public Motion GetOverrideMotion(AnimatorState state) => _layer.GetOverrideMotion(state);

        public IEnumerable<Motion> GetMotions() => SyncedLayer == null
            ? ACUtils.AllStates(stateMachine).Select(x => x.motion)
            : ACUtils.AllStates(SyncedLayer.stateMachine).Select(GetOverrideMotion);
    }
}
