#nullable enable

using JetBrains.Annotations;
using UnityEngine;
using System;

namespace Anatawa12.AvatarOptimizer.API
{
    /// <summary>
    /// The API for UV Usage compability
    ///
    /// Avatar Optimizer may use UV coordinates of the model to optimize the model.
    /// For example, Remove Mesh by Mask use UV coordinate and texture to choose triangles to remove.
    ///
    /// However, this will break with tools that changes UV coordinate like UV packing or atlas texture.
    /// This class provides the API to improve compability with such tools.
    ///
    /// This API is intended to be used by Non-Destructive tools on build time (in `IVRCSDKPreprocessAvatar`)
    /// and must not be used from in-place edit mode tools.
    ///
    /// This API is designed to reduce false negatives, so false positives might be possible.
    /// In other words, if this API returns false, the UV coordinates will not be used by Avatar Optimizer,
    /// but if this API returns true, there is a possibility that the UV coordinates will not be used by Avatar Optimizer.
    ///
    /// ## How to use this API to improve compability
    ///
    /// We do not want to limit how do we use UV coordinates in the future, so
    /// We want your tool to save original UV coordinates of one channel to another channel.
    ///
    /// This API will provide which UV channel will be used by Avatar Optimizer
    /// so that you can save the original UV coordinates to another channel.
    /// And then, you call this API to tell Avatar Optimizer to how did you saved the original UV coordinates.
    /// In the Avatar Optimizer process, Avatar Optimizer will use the saved UV coordinates instead of the original UV coordinates,
    /// and will remove saved UV coordinates after the process.
    ///
    /// ## Notes
    /// This API is introduced in AAO 1.8.0
    /// </summary>
    [PublicAPI]
    public static class UVUsageCompabilityAPI
    {
        internal static IUVUsageCompabilityAPIImpl? Impl;
        internal interface IUVUsageCompabilityAPIImpl
        {
            bool IsTexCoordUsed(SkinnedMeshRenderer renderer, int channel);
            void RegisterTexCoordEvacuation(SkinnedMeshRenderer renderer, int originalChannel, int savedChannel);
        }

        private static IUVUsageCompabilityAPIImpl GetImpl() =>
            Impl ?? throw new InvalidOperationException("UVUsageCompabilityAPI is not initialized");

        /// <summary>
        /// Returns if the specified UV channel is used by Avatar Optimizer.
        /// </summary>
        /// <param name="renderer">The renderer to check.</param>
        /// <param name="channel">The UV channel to check. Value should be 0~7 (inclusive)</param>
        /// <returns>Returns true if the specified UV channel is used by Avatar Optimizer.</returns>
        /// <exception cref="ArgumentOutOfRangeException">channel is out of range</exception>
        /// <exception cref="ArgumentNullException">some argument is null</exception>
        [PublicAPI]
        public static bool IsTexCoordUsed(SkinnedMeshRenderer renderer, int channel)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            if (channel is not (>= 0 and < 8)) throw new ArgumentOutOfRangeException(nameof(channel));
            return GetImpl().IsTexCoordUsed(renderer, channel);
        }

        /// <summary>
        /// Register the evacuation of the UV coordinates of the specified channel.
        /// </summary>
        /// <param name="renderer">The renderer to register the evacuation.</param>
        /// <param name="originalChannel">The original channel of the UV coordinates.</param>
        /// <param name="savedChannel">The saved channel of the UV coordinates.</param>
        /// <exception cref="ArgumentOutOfRangeException">channel is out of range</exception>
        /// <exception cref="ArgumentNullException">some argument is null</exception>
        /// <exception cref="InvalidOperationException">If evaluation failed. If savedChannel is used by AAO, evacuation fails.</exception>
        [PublicAPI]
        public static void RegisterTexCoordEvacuation(SkinnedMeshRenderer renderer, int originalChannel, int savedChannel)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            if (originalChannel is not (>= 0 and < 8)) throw new ArgumentOutOfRangeException(nameof(originalChannel));
            if (savedChannel is not (>= 0 and < 8)) throw new ArgumentOutOfRangeException(nameof(savedChannel));
            GetImpl().RegisterTexCoordEvacuation(renderer, originalChannel, savedChannel);
        }
    }
}
