using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Anatawa12.AvatarOptimizer;

partial class Utils
{
    private static TemporaryRenderTextureScope TemporaryRenderTextureImpl(
        int width,
        int height,
        GraphicsFormat depthStencilFormat,
        GraphicsFormat colorFormat,
        int antiAliasing = 1,
        RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None,
        VRTextureUsage vrUsage = VRTextureUsage.None,
        bool useDynamicScale = false)
    {
        return TemporaryRenderTexture(new RenderTextureDescriptor(width, height, colorFormat, depthStencilFormat)
        {
            msaaSamples = antiAliasing,
            memoryless = memorylessMode,
            vrUsage = vrUsage,
            useDynamicScale = useDynamicScale
        });
    }

    internal static GraphicsFormat GetDepthStencilFormatLegacy(
        int depthBits,
        GraphicsFormat colorFormat)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        return colorFormat == GraphicsFormat.ShadowAuto
#pragma warning restore CS0618 // Type or member is obsolete
            ? GraphicsFormatUtility.GetDepthStencilFormat(depthBits, 0)
            : GraphicsFormatUtility.GetDepthStencilFormat(depthBits, 8);
    }
    
    internal static GraphicsFormat GetDepthStencilFormatLegacy(
        int depthBits,
        RenderTextureFormat format)
    {
        return GetDepthStencilFormatLegacy(depthBits, format == RenderTextureFormat.Shadowmap);
    }
    
    internal static GraphicsFormat GetDepthStencilFormatLegacy(
        int depthBits,
        bool requestedShadowMap)
    {
        return requestedShadowMap ? GraphicsFormatUtility.GetDepthStencilFormat(depthBits, 0) : GraphicsFormatUtility.GetDepthStencilFormat(depthBits, 8);
    }

    public static TemporaryRenderTextureScope TemporaryRenderTexture(
      int width,
      int height,
      int depthBuffer = 0,
      RenderTextureFormat format = RenderTextureFormat.Default,
      RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default,
      int antiAliasing = 1,
      RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None,
      VRTextureUsage vrUsage = VRTextureUsage.None,
      bool useDynamicScale = false)
    {
      var compatibleFormat = GetCompatibleFormat(format, readWrite);
      var stencilFormatLegacy = GetDepthStencilFormatLegacy(depthBuffer, format);

      return TemporaryRenderTextureImpl(width, height, stencilFormatLegacy, compatibleFormat, antiAliasing, memorylessMode, vrUsage, useDynamicScale);
    }

    public static TemporaryRenderTextureScope TemporaryRenderTexture(
      int width,
      int height,
      int depthBuffer,
      GraphicsFormat format,
      int antiAliasing = 1,
      RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None,
      VRTextureUsage vrUsage = VRTextureUsage.None,
      bool useDynamicScale = false)
    {
      return TemporaryRenderTextureImpl(width, height, GetDepthStencilFormatLegacy(depthBuffer, format), format, antiAliasing, memorylessMode, vrUsage, useDynamicScale);
    }

    private static GraphicsFormat GetCompatibleFormat(
        RenderTextureFormat renderTextureFormat,
        RenderTextureReadWrite readWrite)
    {
        var graphicsFormat = GraphicsFormatUtility.GetGraphicsFormat(renderTextureFormat, readWrite);
        var compatibleFormat = SystemInfo.GetCompatibleFormat(graphicsFormat, FormatUsage.Render);
        if (graphicsFormat == compatibleFormat)
            return graphicsFormat;
        return compatibleFormat;
    }

    public static TemporaryRenderTextureScope TemporaryRenderTexture(RenderTextureDescriptor descriptor) =>
        new(RenderTexture.GetTemporary(descriptor));

    public struct TemporaryRenderTextureScope : IDisposable
    {
        public RenderTexture RenderTexture { get; }

        internal TemporaryRenderTextureScope(RenderTexture texture)
        {
            RenderTexture = texture;
        }

        public void Dispose()
        {
            RenderTexture.ReleaseTemporary(RenderTexture);
        }
    }
}
