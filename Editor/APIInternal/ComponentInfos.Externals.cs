using Anatawa12.AvatarOptimizer.API;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIInternal.Externals
{
    /// <summary>
    /// Marker interface for fallback ComponentInformation(s) for external library.
    ///
    /// Thanks to this interface, upstream author can register those component types without causing duplication error
    /// and override information by Avatar Optimizer.
    /// </summary>
    internal interface IExternalMarker
    {
    }

    #region Satania's KiseteneEx Components

    [ComponentInformationWithGUID("e78466b6bcd24e5409dca557eb81d45b", 11500000)] // KiseteneComponent
    [ComponentInformationWithGUID("7f9c3fe1cfb9d1843a9dc7da26352ce2", 11500000)] // FlyAvatarSetupTool
    [ComponentInformationWithGUID("95f6e1368d803614f8a351322ab09bac", 11500000)] // BlendShapeOverrider
    internal class SataniaKiseteneExComponents : ComponentInformation<Component>, IExternalMarker
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
        }
    }

    #endregion

    
    #region VRCQuestTools

    [ComponentInformationWithGUID("f055e14e1beba894ea68aedffde8ada6", 11500000)] // VertexColorRemover
    internal class VRCQuestToolsComponents : ComponentInformation<Component>, IExternalMarker
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
        }
    }

    #endregion
}