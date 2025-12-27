#if UNITY_EDITOR
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CreateAssetMenu(menuName = "Avatar Optimizer/Experimental/AAO Trace And Optimize Platform Settings (Experimental)",
        fileName = "TraceAndOptimizePlatformSettings")]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/trace-and-optimize-platform-settings/")]
    internal class TraceAndOptimizePlatformSettings : ScriptableObject
    {
        internal const int CurrentExperimentalPlatformSettingsVersion = 1;

        // basic info of this settings
        public string platformQualifiedName = "";
        public int experimentalPlatformSettingsVersion = CurrentExperimentalPlatformSettingsVersion;

        // settings below
        // Note currently the following settings is almost the same as debug options in Trace and Optimize.
        // But it was designed for debugging purposes, so those settings are too specific so we should refactor to
        // have more abstract and user-friendly settings in the future before stabilization.
        // Some ideas for abstract settings:
        // - trust constant animation or trust animation curves
        // - animation parameters auto casting
        // - use animators

        // optimize blendShape
        [Section]
        [ToggleLeft] public bool optimizeBlendShape = true;
        [ToggleLeft] public bool freezingNonAnimatedBlendShape = true;
        [ToggleLeft] public bool freezingMeaninglessBlendShape = true;
        [ToggleLeft] public bool autoMergeBlendShape = true;

        // removeUnusedObjects
        [Section]
        [ToggleLeft] public bool removeUnusedObjects = true;
        [ToggleLeft] public bool sweepComponents = true;
        [ToggleLeft] public bool configureLeafMergeBone = true;
        [ToggleLeft] public bool configureMiddleMergeBone = true;
        [ToggleLeft] public bool activenessAnimation = true;
        [ToggleLeft] public bool removeEmptySubMesh = true;
        [ToggleLeft] public bool removeMaterialUnusedProperties = true;
        [ToggleLeft] public bool removeMaterialUnusedTextures = true;
        [ToggleLeft] public bool mergeMaterials = true;

        // optimizePhysBone
        [Section]
        [ToggleLeft] public bool optimizePhysBone = true;
        [ToggleLeft] public bool isAnimatedOptimization = true;
        [ToggleLeft] public bool mergePhysBoneCollider = true;
        [ToggleLeft] public bool replaceEndBoneWithEndpointPosition = true;
        [ToggleLeft] public bool mergePhysBones = true;

        // removeZeroSizedPolygons
        [Section]
        [ToggleLeft] public bool removeZeroSizedPolygons = true;

        // optimize animator
        [Section]
        [ToggleLeft] public bool optimizeAnimator = true;
        [ToggleLeft] public bool entryExitToBlendTree = true;
        [ToggleLeft] public bool removeUnusedAnimatingProperties = true;
        [ToggleLeft] public bool mergeBlendTreeLayer = true;
        [ToggleLeft] public bool removeMeaninglessAnimatorLayer = true;
        [ToggleLeft] public bool anyStateToEntryExit = true;
        [ToggleLeft] public bool completeGraphToEntryExit = true;

        // mergeSkinnedMesh
        [Section]
        [ToggleLeft] public bool mergeSkinnedMesh = true;

        // optimizeTexture
        [Section]
        [ToggleLeft] public bool optimizeTexture = true;
    }

    class Section : PropertyAttribute
    {
    }
}
#endif
