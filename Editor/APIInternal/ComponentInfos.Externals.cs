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
}
