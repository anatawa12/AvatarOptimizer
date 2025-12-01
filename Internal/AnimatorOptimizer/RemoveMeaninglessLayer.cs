using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using nadena.dev.ndmf;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer
{
    public class RemoveMeaninglessLayer : AnimOptPassBase<RemoveMeaninglessLayer>
    {
        private protected override void Execute(BuildContext context, AOAnimatorController controller, TraceAndOptimizeState settings)
        {
            if (settings.RemoveMeaninglessAnimatorLayer) Execute(controller);
        }

        public static void Execute(AOAnimatorController controller)
        {
            var newLayers = new List<AOAnimatorControllerLayer>();
            for (var i = 0; i < controller.layers.Length; i++)
            {
                var layer = controller.layers[i];
                if (!layer.IsRemovable || !IsMeaningless(layer))
                {
                    if (i != newLayers.Count)
                        layer.OnLayerIndexUpdated(newLayers.Count);
                    newLayers.Add(layer);
                }
            }
            controller.SetLayersUnsafe(newLayers.ToArray());
        }

        private static bool IsMeaningless(AOAnimatorControllerLayer layer)
        {
            if (layer.avatarMask != null) return false;
            var stateMachine = layer.IsSynced ? layer.SyncedLayer?.stateMachine :  layer.stateMachine;
            if (stateMachine == null) return true;
            return stateMachine.states.Length == 0 && stateMachine.stateMachines.Length == 0;
        }
    }
}
