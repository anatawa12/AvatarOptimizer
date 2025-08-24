using System.Diagnostics.CodeAnalysis;
using Anatawa12.AvatarOptimizer.API;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIInternal;

// https://creators.vrchat.com/avatars/shader-fallback-system
internal static class VRCFallbackShaderInformations
{
    public static ShaderInformation Standard = new StandardShaderInformation();

    public static ShaderInformation Hidden = new VRCFallbackHiddenShaderInformation();
    public static ShaderInformation Toon = new VRCFallbackToonShaderInformation();
    public static ShaderInformation ToonCutout = new VRCFallbackToonCutoutShaderInformation();
    public static ShaderInformation Unlit = new VRCFallbackUnlitShaderInformation();
    public static ShaderInformation UnlitCutout = new VRCFallbackUnlitCutoutShaderInformation();
    public static ShaderInformation UnlitTransparent = new VRCFallbackUnlitTransparentShaderInformation();

    // Estimate the behavior is the same as a known shader and use it.
    public static ShaderInformation VertexLit = new MobileVertexLitShaderInformation(); // Mobile/Vertex Lit
    public static ShaderInformation Particle = new VRCSDKParticleMultiplyShaderInformation(); // VRChat/Mobile/Particles/Multiply
    public static ShaderInformation Sprite = new VRCSDKSpriteDefaultShaderInformation(); // VRChat/Sprites/Default
    public static ShaderInformation Matcap = new VRCSDKMatcapLitShaderInformation(); // VRChat/Mobile/MatCap Lit
    public static ShaderInformation MobileToon = new VRCSDKToonLitShaderInformation(); // VRChat/Mobile/Toon Lit

    public static ShaderInformation ToonStandard = new VRCSDKToonStandardShaderInformation(false);
    public static ShaderInformation ToonStandardOutline = new VRCSDKToonStandardShaderInformation(true);
    

    // return null if we failed to get information
	public static ShaderInformation? GetInformation(Material material)
	{
		// VRChat 2021.4.2 Fallback System
        if (TryProcess202142(material, out var result))
        {
            return result;
        }

		// Old Fallback System
		return null; // TODO: implement old fallback system
	}

    private static bool TryProcess202142(Material material, [NotNullWhen(true)] out ShaderInformation? result)
    {
        result = null;

		var fallbackTag = material.GetTag("VRCFallback", false);
		if (string.IsNullOrEmpty(fallbackTag)) return false;

		var raw = fallbackTag;
		var tag = raw.Replace(" ", string.Empty).ToLowerInvariant();

		// Special uncombinable variants
		if (tag == "toonstandard")
		{
			result = ToonStandard;
			return true;
		}   
		if (tag == "toonstandardoutline")
		{
			result = ToonStandardOutline;
			return true;
		}

		// Parse composite flags
		var hasUnlit = tag.Contains("unlit");
		var hasVertexLit = tag.Contains("vertexlit");
		var hasToon = tag.Contains("toon");
		var hasTransparent = tag.Contains("transparent");
		var hasCutout = tag.Contains("cutout");
		var hasFade = tag.Contains("fade");
		var hasParticle = tag.Contains("particle");
		var hasSprite = tag.Contains("sprite");
		var hasMatcap = tag.Contains("matcap");
		var hasMobileToon = tag.Contains("mobiletoon");
		var hasDoubleSided = tag.Contains("doublesided");
		var hasHidden = tag.Contains("hidden");


		if (hasHidden)
		{
            // hidden → mesh hidden
			result = Hidden;
			return true;
		}

		if (hasToon)
		{
            // can be combined with Transparent, Cutout and Fade tags 
            // doublesided tag can also be combined, but it is ignored here.

			if (hasTransparent || hasFade)
			{
				result = UnlitTransparent;
				return true;
			}
            if (hasCutout)
            {
                result = ToonCutout;
                return true;
            }
            result = Toon;
            return true;
		}

		if (hasUnlit)
        {
            // can be combined with Transparent, Cutout and Fade tags 
            // doublesided tag cannot be combined.

			if (hasTransparent || hasFade)
            {
                result = UnlitTransparent;
                return true;
            }
            if (hasCutout)
            {
                result = UnlitCutout;
                return true;
            }
            result = Unlit;
            return true;
        }


        if (hasVertexLit)
        {
            result = VertexLit;
            return true;
        }
        if (hasParticle)
        {
            result = Particle;
            return true;
        }
        if (hasSprite)
        {
            result = Sprite;
            return true;
        }
        if (hasMatcap)
        {
            result = Matcap;
            return true;
        }
        if (hasMobileToon)
        {
            result = MobileToon;
            return true;
        }

		// If we've reached here, it means a VRCFallback tag was present, but it didn't match any of the specific rules above.
		result = Standard;
		return true;
    }
}
