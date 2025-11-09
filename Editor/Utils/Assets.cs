using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal class Assets
    {
        private static CachedGuidLoader<Shader> _toonLitShader = "affc81f3d164d734d8f13053effb1c5c";
        public static Shader ToonLitShader => _toonLitShader.Value;
        
        private static CachedGuidLoader<Shader> _mergeTextureHelper = "2d4f01f29e91494bb5eafd4c99153ab0";
        public static Shader MergeTextureHelper => _mergeTextureHelper.Value;
        
        private static CachedGuidLoader<Shader> _mergeTextureHelperV2 = "43be1a4e52a840a2a309cc8b9715b4fe";
        public static Shader MergeTextureHelperV2 => _mergeTextureHelperV2.Value;

        private static CachedGuidLoader<Texture2D> _previewHereTex = "617775211fe634657ae06fc9f81b6ceb";
        public static Texture2D PreviewHereTex => _previewHereTex.Value;
    }
}
