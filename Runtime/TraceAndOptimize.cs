using System;
using CustomLocalization4EditorExtension;
using UnityEngine;
using UnityEngine.Animations;

namespace Anatawa12.AvatarOptimizer
{
    // previously known as Automatic Configuration
    [AddComponentMenu("Avatar Optimizer/AAO Trace And Optimize")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/trace-and-optimize/")]
    internal class TraceAndOptimize : AvatarGlobalComponent
    {
        [NotKeyable]
        [CL4EELocalized("TraceAndOptimize:prop:freezeBlendShape")]
        [ToggleLeft]
        public bool freezeBlendShape = true;
        [NotKeyable]
        [CL4EELocalized("TraceAndOptimize:prop:removeUnusedObjects")]
        [ToggleLeft]
        public bool removeUnusedObjects = true;

        // Remove Unused Objects Options
        [NotKeyable]
        [CL4EELocalized("TraceAndOptimize:prop:preserveEndBone",
            "TraceAndOptimize:tooltip:preserveEndBone")]
        [ToggleLeft]
        public bool preserveEndBone;

        // common parsing configuration
        [NotKeyable]
        [CL4EELocalized("TraceAndOptimize:prop:mmdWorldCompatibility",
            "TraceAndOptimize:tooltip:mmdWorldCompatibility")]
        [ToggleLeft]
        public bool mmdWorldCompatibility = true;

        // for compatibility, this is not inside AdvancedSettings but this is part of Advanced Settings
        [NotKeyable]
        [InspectorName("Use Advanced Animator Parser")]
        [Tooltip("Advanced Animator Parser will parse your AnimatorController, including layer structure.")]
        [ToggleLeft]
        public bool advancedAnimatorParser = true;

        [NotKeyable]
        public AdvancedSettings advancedSettings;
        
        [Serializable]
        public struct AdvancedSettings
        {
            [Tooltip("Exclude some GameObjects from Trace and Optimize")]
            public GameObject[] exclusions;
            [Tooltip("Use Legacy algorithm for Remove Unused Objects")]
            [ToggleLeft]
            public bool useLegacyGc;
            [Tooltip("Add GC Debug Components instead of setting GC components")]
            [ToggleLeft]
            public bool gcDebug;
            [Tooltip("Do Not Configure MergeBone in New GC algorithm")]
            [ToggleLeft]
            public bool noConfigureMergeBone;
        }
    }
}