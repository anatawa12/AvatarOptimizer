#nullable enable

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using ArgumentNullException = System.ArgumentNullException;

namespace Anatawa12.AvatarOptimizer.API
{
    /// <summary>
    /// The registry of shader information.
    ///
    /// You should register information on InitializeOnLoad or RuntimeInitializeOnLoadMethod or on creating the shader with constructor.
    /// </summary>
    [PublicAPI]
    public static class ShaderInformationRegistry
    {
        private static Dictionary<Shader, ShaderInformation> shaderInformation = new();
        private static Dictionary<string, ShaderInformation> shaderInformationWithGUID = new();

        /// <summary>
        /// Register the shader information with the GUID of the shader.
        ///
        /// If the shader is registerd with both <see cref="RegisterShaderInformation"/> and this function, the information registered with <see cref="RegisterShaderInformation"/> will be used.
        /// </summary>
        /// <param name="guid">The GUID of the shader to register the information.</param>
        /// <param name="information">The information to register.</param>
        /// <returns></returns>
        [PublicAPI]
        public static IDisposable RegisterShaderInformationWithGUID(string guid, ShaderInformation information)
        {
            if (guid == null) throw new ArgumentNullException(nameof(guid));
            if (information == null) throw new ArgumentNullException(nameof(information));

            if (shaderInformationWithGUID.TryAdd(guid, information))
                return new UnregisterGUIDDisposable(guid);

            if (shaderInformationWithGUID[guid].IsInternalInformation)
            {
                shaderInformationWithGUID[guid] = information;
                return NoopDisposable.Instance;
            }

            var existing = shaderInformationWithGUID[guid];
            if (existing.IsInternalInformation)
            {
                shaderInformationWithGUID[guid] = information;
                return new UnregisterGUIDDisposable(guid);
            }
            
            throw new InvalidOperationException("the shader is already registered.");
        }

        /// <summary>
        /// Register the shader information.
        ///
        /// If the shader is registered with both <see cref="RegisterShaderInformation"/> and <see cref="RegisterShaderInformationWithGUID"/>, the information registered with <see cref="RegisterShaderInformation"/> will be used.
        /// </summary>
        /// <remarks>
        /// This function is mainly for runtime-generated shaders.
        /// 
        /// If you have a shader as a asset, you should use <see cref="RegisterShaderInformationWithGUID"/> instead since loading assets might not be possible to load on <see cref="InitializeOnLoadAttribute"/> time.
        /// </remarks>
        /// <param name="shader">The shader to register the information.</param>
        /// <param name="information">The information to register.</param>
        /// <returns>The disposable to unregister the information.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="shader"/> or <paramref name="information"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The shader is already registered.</exception>
        [PublicAPI]
        public static IDisposable RegisterShaderInformation(Shader shader, ShaderInformation information)
        {
            if (shader == null) throw new ArgumentNullException(nameof(shader));
            if (information == null) throw new ArgumentNullException(nameof(information));
            if (information.IsInternalInformation) throw new InvalidOperationException("the shader is already registered.");

            if (shaderInformation.TryAdd(shader, information))
                return new UnregisterDisposable(shader);

            var existing = shaderInformation[shader];
            if (existing.IsInternalInformation)
            {
                shaderInformation[shader] = information;
                return new UnregisterDisposable(shader);
            }

            throw new InvalidOperationException("the shader is already registered.");
        }

        private class UnregisterDisposable : IDisposable
        {
            private readonly Shader _shader;

            public UnregisterDisposable(Shader shader) => _shader = shader;

            public void Dispose() => shaderInformation.Remove(_shader);
        }

        private class UnregisterGUIDDisposable : IDisposable
        {
            private readonly string _guid;
            public UnregisterGUIDDisposable(string guid) => _guid = guid;
            public void Dispose() => shaderInformationWithGUID.Remove(_guid);
        }

        private class NoopDisposable : IDisposable
        {
            public static NoopDisposable Instance { get; } = new();

            public void Dispose()
            {
            }
        }

        internal static ShaderInformation? GetShaderInformation(Shader shader)
        {
            if (shaderInformation.TryGetValue(shader, out var info)) return info;
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(shader));
            if (shaderInformationWithGUID.TryGetValue(guid, out var infoWithGUID)) return infoWithGUID;
            return null;
        }
    }

    /// <summary>
    /// The class that provides the information about the shader.
    ///
    /// If your shader is a Shader Suite, you can share shaderInformation class with multiple Shader asset.
    /// </summary>
    [PublicAPI]
    public abstract class ShaderInformation
    {
        [PublicAPI]
        protected ShaderInformation()
        {
        }

        /// <summary>
        /// If this is true, some other information can override this information.
        /// </summary>
        internal virtual bool IsInternalInformation => false;

        /// <summary>
        /// Gets the texture usage information for the material.
        ///
        /// This function should call the callback to provide the texture usage information for the material.
        /// </summary>
        /// <param name="matInfo">The callback to provide the texture usage information for the material.</param>
        /// <returns>Whether the information is provided successfully. If false, the information is considered as not provided.</returns>
        [PublicAPI]
        public abstract bool GetTextureUsageInformationForMaterial(TextureUsageInformationCallback matInfo);

        // note for future this class update: this class is extandable public abstract class so you must not add new abstract method to this class.
    }

    /// <summary>
    /// The callback for the texture usage information.
    /// </summary>
    [PublicAPI]
    public abstract class TextureUsageInformationCallback
    {
        internal TextureUsageInformationCallback()
        {
        }

        /// <summary>
        /// Returns the integer value for the property in the material, or null if the property is not set or not found.
        /// </summary>
        /// <param name="propertyName">The name of the property in the material.</param>
        /// <param name="considerAnimation">Whether to consider the animation of the property. If this is true, this function will never </param>
        /// <returns>The integer value for the property in the material, which is same as <see cref="Material.GetInteger(string)"/>, or null if the property is not set or not found.</returns>
        [PublicAPI]
        public abstract int? GetInteger(string propertyName, bool considerAnimation = true);

        /// <summary>
        /// Returns the float value for the property in the material, or null if the property is not set or not found.
        /// </summary>
        /// <param name="propertyName">The name of the property in the material.</param>
        /// <param name="considerAnimation">Whether to consider the animation of the property. If this is true, this function will never </param>
        /// <returns>The integer value for the property in the material, which is same as <see cref="Material.GetFloat(string)"/>, or null if the property is not set or not found.</returns>
        [PublicAPI]
        public abstract float? GetFloat(string propertyName, bool considerAnimation = true);

        /// <summary>
        /// Returns the float value for the property in the material, or null if the property is not set or not found.
        /// </summary>
        /// <param name="propertyName">The name of the property in the material.</param>
        /// <param name="considerAnimation">Whether to consider the animation of the property. If this is true, this function will never </param>
        /// <returns>The integer value for the property in the material, which is same as <see cref="Material.GetVector(string)"/>, or null if the property is not set or not found.</returns>
        [PublicAPI]
        public abstract Vector4? GetVector(string propertyName, bool considerAnimation = true);

        /// <summary>
        /// Registers UV Usage that are not considered by Avatar Optimizer.
        ///
        /// This will the UV Channel not affected by optimizations of Avatar Optimizer.
        /// </summary>
        /// <param name="uvChannel">The UVChannels that are used in the shader.</param>
        [PublicAPI]
        public abstract void RegisterOtherUVUsage(UsingUVChannels uvChannel);

        /// <summary>
        /// Registers Texture Usage and UV Usage that are considered by Avatar Optimizer.
        /// 
        /// The texture might go to the atlas / UV Packing if the UsingUVChannels is set and the UV Matrix is known
        /// </summary>
        /// <param name="textureMaterialPropertyName">The name of the texture property in the material.</param>
        /// <param name="samplerState">The information about the sampler state used for the specified texture.</param>
        /// <param name="uvChannels">The UVChannels that are used in the shader to determine the UV for the texture.</param>
        /// <param name="uvMatrix">The UV Transform Matrix for the texture. This includes textureName_ST scale offset. Null if the UV transfrom is not known.</param>
        /// <remarks>
        /// This section describes the current and planned implementation of UV Packing in the Avatar Optimizer about this function.
        /// 
        /// Currently, Avatar Optimizer does UV Packing if (non-exclusive):
        /// - Texture is reasonably used by small set of materials
        /// - UsingUVChannels is set to only one of UV Channels (per material)
        /// - UV Matrix is known and identity matrix
        /// 
        /// However, Avatar Optimizer will support more complex UV Packing in the future:
        /// - Support UV Matrix with scale is smaller and rotation is multiple of 90 degrees
        /// - multiple UV Channel texture
        /// </remarks>
        [PublicAPI]
        public abstract void RegisterTextureUVUsage(
            string textureMaterialPropertyName,
            SamplerStateInformation samplerState,
            UsingUVChannels uvChannels,
            Matrix4x4? uvMatrix);
    }

    /// <summary>
    /// The flags to express which UV Channels might be used in the shader.
    ///
    /// Usage of the UV channels might be specified with some other APIs.
    /// </summary>
    [Flags]
    [PublicAPI]
    public enum UsingUVChannels
    {
        UV0 = 1,
        UV1 = 2,
        UV2 = 4,
        UV3 = 8,
        UV4 = 16,
        UV5 = 32,
        UV6 = 64,
        UV7 = 128,

        /// <summary>
        /// The UV Channels not from the Mesh UV.
        /// For example, screenspace or color.
        /// </summary>
        NonMesh = 256,
        Unknown = 0x7FFFFFFF,
    }

    /// <summary>
    /// The information about the sampler state for the specified texture.
    ///
    /// You can combine multiple SamplerStateInformation for the texture with `|` operator.
    ///
    /// You can cast string to <c>SamplerStateInformation</c> to use the sampler state for
    /// the specified texture like <c>sampler_MainTex</c> by <c>(SamplerStateInformation)"_MainTex"</c>.
    ///
    /// If your shader is using hardcoded sampler state, you can use the predefined sampler state like
    /// <see cref="SamplerStateInformation.PointClampSampler"/> or <see cref="SamplerStateInformation.LinearRepeatSampler"/>.
    /// </summary>
    [PublicAPI]
    public readonly struct SamplerStateInformation
    {
        private readonly string _textureName;
        private readonly bool _materialProperty;

        [PublicAPI]
        public SamplerStateInformation(string textureName)
        {
            _textureName = textureName;
            _materialProperty = true;
        }

        // construct builtin non-material property sampler state
        private SamplerStateInformation(string textureName, bool dummy)
        {
            _textureName = textureName;
            _materialProperty = false;
        }

        // I don't want to expose equals to public API so I made this internal function instead of overriding Equals
        internal static bool EQ(SamplerStateInformation left, SamplerStateInformation right)
        {
            if (left._materialProperty != right._materialProperty) return false;
            if (left._textureName != right._textureName) return false;
            return true;
        }

        /// <summary>Unknown Sampler. The Avatar Optimizer will never optimize depends on sampler state information</summary>
        [PublicAPI]
        public static SamplerStateInformation Unknown { get; } = new("Unknown", false);

        /// <summary>The hard-coded inline Sampler with clamp texture wrapping and point texture filtering mode</summary>
        [PublicAPI]
        public static SamplerStateInformation PointClampSampler { get; } = new("PointClamp", false);

        /// <summary>The hard-coded inline Sampler with repeat texture wrapping and point texture filtering mode</summary>
        [PublicAPI]
        public static SamplerStateInformation PointRepeatSampler { get; } = new("PointRepeat", false);

        /// <summary>The hard-coded inline Sampler with mirror texture wrapping and point texture filtering mode</summary>
        [PublicAPI]
        public static SamplerStateInformation PointMirrorSampler { get; } = new("PointMirror", false);

        /// <summary>The hard-coded inline Sampler with mirror-once texture wrapping and point texture filtering mode</summary>
        [PublicAPI]
        public static SamplerStateInformation PointMirrorOnceSampler { get; } =
            new("PointMirrorOnce", false);

        /// <summary>The hard-coded inline Sampler with clamp texture wrapping and linear texture filtering mode</summary>
        [PublicAPI]
        public static SamplerStateInformation LinearClampSampler { get; } = new("LinearClamp", false);

        /// <summary>The hard-coded inline Sampler with repeat texture wrapping and linear texture filtering mode</summary>
        [PublicAPI]
        public static SamplerStateInformation LinearRepeatSampler { get; } = new("LinearRepeat", false);

        /// <summary>The hard-coded inline Sampler with mirror texture wrapping and linear texture filtering mode</summary>
        [PublicAPI]
        public static SamplerStateInformation LinearMirrorSampler { get; } = new("LinearMirror", false);

        /// <summary>The hard-coded inline Sampler with mirror-once texture wrapping and linear texture filtering mode</summary>
        [PublicAPI]
        public static SamplerStateInformation LinearMirrorOnceSampler { get; } =
            new("LinearMirrorOnce", false);

        /// <summary>The hard-coded inline Sampler with clamp texture wrapping and anisotropic texture filtering mode</summary>
        [PublicAPI]
        public static SamplerStateInformation TrilinearClampSampler { get; } = new("TrilinearClamp", false);

        /// <summary>The hard-coded inline Sampler with repeat texture wrapping and anisotropic texture filtering mode</summary>
        [PublicAPI]
        public static SamplerStateInformation TrilinearRepeatSampler { get; } =
            new("TrilinearRepeat", false);

        /// <summary>The hard-coded inline Sampler with mirror texture wrapping and anisotropic texture filtering mode</summary>
        [PublicAPI]
        public static SamplerStateInformation TrilinearMirrorSampler { get; } =
            new("TrilinearMirror", false);

        /// <summary>The hard-coded inline Sampler with mirror-once texture wrapping and anisotropic texture filtering mode</summary>
        [PublicAPI]
        public static SamplerStateInformation TrilinearMirrorOnceSampler { get; } =
            new("TrilinearMirrorOnce", false);

        [PublicAPI]
        public static implicit operator SamplerStateInformation(string textureName) => new(textureName);

        [PublicAPI]
        public static SamplerStateInformation operator |(SamplerStateInformation left, SamplerStateInformation right) =>
            Combine(left, right);

        private static SamplerStateInformation Combine(SamplerStateInformation left, SamplerStateInformation right)
        {
            // we may implement better logic in the future
            if (EQ(left, right)) return left;
            return Unknown;
        }
    }
}
