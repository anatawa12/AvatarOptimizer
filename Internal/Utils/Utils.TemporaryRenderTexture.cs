using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
namespace Anatawa12.AvatarOptimizer;

partial class Utils
{
    public static TemporaryRenderTextureScope TemporaryRenderTexture(
      int width,
      int height,
      int depthBuffer,
      GraphicsFormat format,
      int antiAliasing = 1,
      RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None,
      VRTextureUsage vrUsage = VRTextureUsage.None,
      bool useDynamicScale = false)
    => new(RenderTexture.GetTemporary(width, height, depthBuffer, format, antiAliasing, memorylessMode, vrUsage, useDynamicScale));

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
