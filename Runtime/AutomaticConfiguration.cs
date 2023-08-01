using CustomLocalization4EditorExtension;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/Automatic Configuration")]
    [DisallowMultipleComponent]
    internal class AutomaticConfiguration : AvatarGlobalComponent
    {
        [CL4EELocalized("AutomaticConfiguration:prop:freezeBlendShape")]
        [ToggleLeft]
        public bool freezeBlendShape = true;
        [CL4EELocalized("AutomaticConfiguration:prop:removeUnusedObjects",
            "AutomaticConfiguration:tooltip:removeUnusedObjects")]
        [ToggleLeft]
        public bool removeUnusedObjects = true;
        [CL4EELocalized("AutomaticConfiguration:prop:mmdWorldCompatibility",
            "AutomaticConfiguration:tooltip:mmdWorldCompatibility")]
        [ToggleLeft]
        public bool mmdWorldCompatibility = true;
    }
}