using CustomLocalization4EditorExtension;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/Automatic Configuration")]
    [DisallowMultipleComponent]
    internal class AutomaticConfiguration : AvatarGlobalComponent
    {
        [CL4EELocalized("AutomaticConfiguration:prop:freezeBlendShape")]
        [ToggleLeft]
        public bool freezeBlendShape = true;
        [CL4EELocalized("AutomaticConfiguration:prop:dontFreezeMmdShapes")]
        [ToggleLeft]
        public bool dontFreezeMmdShapes = true;
    }
}