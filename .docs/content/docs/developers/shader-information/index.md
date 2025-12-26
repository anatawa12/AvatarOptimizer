---
title: Shader Information API
---

# Shader Information API

Since Avatar Optimizer v1.0.0, Avatar Optimizer provides the Shader Information API to help optimize materials with custom shaders.
By registering shader information, you can enable Avatar Optimizer to perform advanced optimizations like texture atlasing and UV packing for your custom shaders.

## What is Shader Information? {#what-is-shader-information}

Shader Information is a way to tell Avatar Optimizer how your shader uses textures, UV channels, and other material properties.
This information enables Avatar Optimizer to:

- **Pack multiple textures into texture atlases** (with components like `AAO Merge Material`)
- **Optimize UV channels** by understanding which UV channels are used
- **Preserve vertex indices** when shaders rely on them
- **Remove unused material properties** safely

Without Shader Information, Avatar Optimizer treats your shader conservatively and cannot perform these optimizations.

## Built-in Shader Support {#built-in-support}

Avatar Optimizer provides built-in Shader Information for popular shaders:

- **Unity Built-in Shaders** (Standard, Unlit, etc.)
- **VRChat SDK Shaders** (Standard Lite, Toon Lit, Toon Standard)
- **lilToon** (all variants up to version 45)

If you're using these shaders, no additional setup is required.

## When to Provide Shader Information {#when-to-provide}

You should provide Shader Information for your shader if:

1. **Your shader is used on VRChat avatars** and users want to optimize them with Avatar Optimizer
2. **Your shader uses textures with UV transforms** (like `_MainTex_ST` scale/offset)
3. **Your shader uses vertex indices** for special effects
4. **You want to enable texture atlasing** with `AAO Merge Material` component

If your shader is only for simple effects and doesn't need advanced optimizations, you may not need to provide Shader Information.

## Getting Started {#getting-started}

To provide Shader Information for your shader, follow these steps:

### 1. Create an Assembly Definition {#create-asmdef}

If your shader package doesn't have an Editor assembly definition, create one.
The assembly should be Editor-only since Shader Information is only used at build time.

### 2. Add Assembly Reference {#add-reference}

Add `com.anatawa12.avatar-optimizer.api.editor` to your assembly definition's references.

If you don't want to require Avatar Optimizer, use [Version Defines] with the symbol `AVATAR_OPTIMIZER`:

![version-defines.png](../make-your-components-compatible-with-aao/version-defines.png)

Recommended version range: `[1.0,2.0)` (supports v1.x.x but will require updates for v2.0.0)

### 3. Create Shader Information Class {#create-class}

Create a class that extends `ShaderInformation` and register it with `ShaderInformationRegistry`:

```csharp
#if AVATAR_OPTIMIZER && UNITY_EDITOR

using Anatawa12.AvatarOptimizer.API;
using UnityEditor;
using UnityEngine;

namespace YourNamespace
{
    [InitializeOnLoad]
    internal class YourShaderInformation : ShaderInformation
    {
        static YourShaderInformation()
        {
            // Register with shader GUID (recommended for shader assets)
            string shaderGuid = "your-shader-guid-here";
            ShaderInformationRegistry.RegisterShaderInformationWithGUID(
                shaderGuid, 
                new YourShaderInformation()
            );
        }

        public override ShaderInformationKind SupportedInformationKind =>
            ShaderInformationKind.TextureAndUVUsage;

        public override void GetMaterialInformation(MaterialInformationCallback matInfo)
        {
            // Register texture and UV usage here (see examples below)
        }
    }
}

#endif
```

## ShaderInformationKind Flags {#information-kinds}

The `SupportedInformationKind` property tells Avatar Optimizer what information you're providing:

### `TextureAndUVUsage` {#texture-uv-usage}

Indicates you provide information about:
- Which textures the shader uses
- Which UV channels each texture samples from
- UV transform matrices (scale/offset)
- Sampler states (wrap mode, filter mode)

Providing this enables **texture atlasing** and **UV packing** optimizations.

### `VertexIndexUsage` {#vertex-index-usage}

Indicates your shader uses vertex indices (e.g., `SV_VertexID`).

If you **don't** provide this flag, Avatar Optimizer assumes vertex indices are **not used** and may shuffle vertices during optimization.
If your shader uses vertex indices, you **must** set this flag to prevent incorrect rendering.

### Combining Flags {#combining-flags}

You can combine flags with the `|` operator:

```csharp
public override ShaderInformationKind SupportedInformationKind =>
    ShaderInformationKind.TextureAndUVUsage | ShaderInformationKind.VertexIndexUsage;
```

## Registering Material Information {#registering-information}

The `GetMaterialInformation` method is called for each material using your shader.
Use the `MaterialInformationCallback` to register texture and UV usage.

### Reading Material Properties {#reading-properties}

The callback provides methods to read material properties:

```csharp
// Read float properties
float? value = matInfo.GetFloat("_PropertyName");

// Read int properties  
int? value = matInfo.GetInt("_PropertyName");

// Read Vector4 properties (like _MainTex_ST)
Vector4? value = matInfo.GetVector("_MainTex_ST");

// Check if shader keyword is enabled
bool? enabled = matInfo.IsShaderKeywordEnabled("KEYWORD_NAME");
```

These methods return `null` if:
- The property doesn't exist
- The property is animated (when `considerAnimation: true`, which is default)
- The value is unknown or mixed

### Registering Texture Usage {#registering-textures}

Use `RegisterTextureUVUsage` to tell Avatar Optimizer about each texture:

```csharp
public override void GetMaterialInformation(MaterialInformationCallback matInfo)
{
    // Get the UV transform (scale/offset)
    var mainTexST = matInfo.GetVector("_MainTex_ST");
    Matrix2x3? uvMatrix = mainTexST is { } st 
        ? Matrix2x3.NewScaleOffset(st) 
        : null;

    // Register the texture
    matInfo.RegisterTextureUVUsage(
        textureMaterialPropertyName: "_MainTex",
        samplerState: "_MainTex",  // Uses sampler from _MainTex property
        uvChannels: UsingUVChannels.UV0,
        uvMatrix: uvMatrix
    );
}
```

#### Parameters {#texture-params}

- **`textureMaterialPropertyName`**: The texture property name (e.g., `"_MainTex"`)
- **`samplerState`**: Sampler to use (see [Sampler States](#sampler-states))
- **`uvChannels`**: Which UV channel(s) the texture uses (see [UV Channels](#uv-channels))
- **`uvMatrix`**: UV transform matrix, or `null` if unknown/dynamic

### Sampler States {#sampler-states}

Sampler states define texture wrapping and filtering. You can specify them in several ways:

#### Use Material Property Sampler {#material-sampler}

Most shaders use a sampler from a material property. Use the property name:

```csharp
matInfo.RegisterTextureUVUsage(
    "_MainTex",
    samplerState: "_MainTex",  // String implicitly converts
    UsingUVChannels.UV0,
    uvMatrix
);
```

Or explicitly:

```csharp
samplerState: new SamplerStateInformation("_MainTex")
```

#### Use Hard-coded Sampler {#hardcoded-sampler}

If your shader uses inline samplers (e.g., `SamplerState linearClampSampler`), use predefined constants:

```csharp
// Point filtering
SamplerStateInformation.PointClampSampler
SamplerStateInformation.PointRepeatSampler
SamplerStateInformation.PointMirrorSampler
SamplerStateInformation.PointMirrorOnceSampler

// Linear filtering
SamplerStateInformation.LinearClampSampler
SamplerStateInformation.LinearRepeatSampler
SamplerStateInformation.LinearMirrorSampler
SamplerStateInformation.LinearMirrorOnceSampler

// Trilinear/Anisotropic filtering
SamplerStateInformation.TrilinearClampSampler
SamplerStateInformation.TrilinearRepeatSampler
SamplerStateInformation.TrilinearMirrorSampler
SamplerStateInformation.TrilinearMirrorOnceSampler
```

Example:

```csharp
matInfo.RegisterTextureUVUsage(
    "_NoiseTexture",
    SamplerStateInformation.LinearRepeatSampler,
    UsingUVChannels.NonMesh,
    null
);
```

#### Unknown Sampler {#unknown-sampler}

If the sampler cannot be determined:

```csharp
samplerState: SamplerStateInformation.Unknown
```

This prevents optimizations that depend on sampler state.

### UV Channels {#uv-channels}

Specify which UV channel(s) the texture samples from using `UsingUVChannels`:

```csharp
UsingUVChannels.UV0  // TEXCOORD0
UsingUVChannels.UV1  // TEXCOORD1
UsingUVChannels.UV2  // TEXCOORD2
UsingUVChannels.UV3  // TEXCOORD3
UsingUVChannels.UV4  // TEXCOORD4
UsingUVChannels.UV5  // TEXCOORD5
UsingUVChannels.UV6  // TEXCOORD6
UsingUVChannels.UV7  // TEXCOORD7
UsingUVChannels.NonMesh  // Screen space, normals, etc.
UsingUVChannels.Unknown  // Cannot determine
```

#### Multiple UV Channels {#multiple-uv}

If the UV channel depends on a material property:

```csharp
var uvChannel = matInfo.GetFloat("_UVChannel") switch
{
    0 => UsingUVChannels.UV0,
    1 => UsingUVChannels.UV1,
    _ => UsingUVChannels.UV0 | UsingUVChannels.UV1  // Unknown, could be either
};

matInfo.RegisterTextureUVUsage("_DetailTex", "_DetailTex", uvChannel, uvMatrix);
```

#### Non-Mesh UV {#non-mesh-uv}

For textures that don't use mesh UVs (screen space, MatCap, view-direction based, etc.):

```csharp
matInfo.RegisterTextureUVUsage(
    "_MatCapTexture",
    "_MatCapTexture", 
    UsingUVChannels.NonMesh,  // Not from mesh UVs
    null  // No UV transform
);
```

### UV Transform Matrices {#uv-matrices}

UV transform matrices describe how UVs are scaled and offset (like `_MainTex_ST`).

#### From Scale/Offset Vector {#scale-offset}

Most Unity shaders use a Vector4 with `(scaleX, scaleY, offsetX, offsetY)`:

```csharp
var texST = matInfo.GetVector("_MainTex_ST");
Matrix2x3? uvMatrix = texST is { } st 
    ? Matrix2x3.NewScaleOffset(st)
    : null;
```

#### Manual Construction {#manual-matrix}

You can build matrices manually:

```csharp
// Identity (no transform)
Matrix2x3.Identity

// Scale only
Matrix2x3.Scale(2.0f, 2.0f)

// Translate only  
Matrix2x3.Translate(0.5f, 0.5f)

// Rotate (in radians)
Matrix2x3.Rotate(Mathf.PI / 4)

// Combine transforms with multiplication
var matrix = Matrix2x3.Scale(2, 2) * Matrix2x3.Translate(0.5f, 0.5f);

// Full manual construction
new Matrix2x3(
    m00: 1, m01: 0, m02: 0,  // First row: x transform
    m10: 0, m11: 1, m12: 0   // Second row: y transform
);
```

#### Dynamic or Unknown Transforms {#unknown-transform}

If the UV transform is animated or calculated at runtime, use `null`:

```csharp
matInfo.RegisterTextureUVUsage(
    "_ScrollingTexture",
    "_ScrollingTexture",
    UsingUVChannels.UV0,
    null  // Transform is dynamic, cannot optimize
);
```

### Registering Other UV Usage {#other-uv-usage}

If your shader uses UVs for purposes other than texture sampling (but only uses the integer part):

```csharp
// Example: UV-based mesh decimation that only uses floor(UV)
matInfo.RegisterOtherUVUsage(UsingUVChannels.UV1);
```

**Note**: Only use this if your shader only uses the integer part of UVs (like UV Tile Discard).
If your shader uses fractional UV values for calculations, this is incorrect.

### Registering Vertex Index Usage {#register-vertex-index}

If your shader uses vertex indices (e.g., for noise or special effects):

```csharp
public override void GetMaterialInformation(MaterialInformationCallback matInfo)
{
    // ... register textures ...

    // Tell Avatar Optimizer this shader uses vertex indices
    matInfo.RegisterVertexIndexUsage();
}
```

**Important**: Only call this if vertex indices significantly affect the visual result.
If vertex indices are only used for subtle noise or minor effects, you can omit this to allow better optimizations.

## Complete Examples {#examples}

### Simple Shader with Main Texture {#example-simple}

```csharp
[InitializeOnLoad]
internal class SimpleShaderInformation : ShaderInformation
{
    static SimpleShaderInformation()
    {
        ShaderInformationRegistry.RegisterShaderInformationWithGUID(
            "your-shader-guid",
            new SimpleShaderInformation()
        );
    }

    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? uvMatrix = mainTexST is { } st 
            ? Matrix2x3.NewScaleOffset(st) 
            : null;

        matInfo.RegisterTextureUVUsage(
            "_MainTex",
            "_MainTex",
            UsingUVChannels.UV0,
            uvMatrix
        );
    }
}
```

### Shader with Conditional Features {#example-conditional}

```csharp
[InitializeOnLoad]
internal class FeatureShaderInformation : ShaderInformation
{
    static FeatureShaderInformation()
    {
        ShaderInformationRegistry.RegisterShaderInformationWithGUID(
            "your-shader-guid",
            new FeatureShaderInformation()
        );
    }

    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        // Main texture (always present)
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? mainUVMatrix = mainTexST is { } st 
            ? Matrix2x3.NewScaleOffset(st) 
            : null;

        matInfo.RegisterTextureUVUsage(
            "_MainTex", "_MainTex", UsingUVChannels.UV0, mainUVMatrix
        );

        // Normal map (conditional on keyword)
        if (matInfo.IsShaderKeywordEnabled("_NORMALMAP") != false)
        {
            matInfo.RegisterTextureUVUsage(
                "_BumpMap", "_BumpMap", UsingUVChannels.UV0, mainUVMatrix
            );
        }

        // Detail texture (conditional on property)
        if (matInfo.GetFloat("_UseDetail") != 0)
        {
            var detailST = matInfo.GetVector("_DetailTex_ST");
            Matrix2x3? detailUVMatrix = detailST is { } st2 
                ? Matrix2x3.NewScaleOffset(st2) 
                : null;

            var detailUV = matInfo.GetFloat("_DetailUV") switch
            {
                0 => UsingUVChannels.UV0,
                1 => UsingUVChannels.UV1,
                _ => UsingUVChannels.UV0 | UsingUVChannels.UV1
            };

            matInfo.RegisterTextureUVUsage(
                "_DetailTex", "_DetailTex", detailUV, detailUVMatrix
            );
        }

        // MatCap (screen-space, no UV transform)
        if (matInfo.IsShaderKeywordEnabled("_MATCAP") != false)
        {
            matInfo.RegisterTextureUVUsage(
                "_MatCap",
                SamplerStateInformation.LinearClampSampler,
                UsingUVChannels.NonMesh,
                null
            );
        }
    }
}
```

### Shader Using Vertex Indices {#example-vertex-index}

```csharp
[InitializeOnLoad]
internal class VertexShaderInformation : ShaderInformation
{
    static VertexShaderInformation()
    {
        ShaderInformationRegistry.RegisterShaderInformationWithGUID(
            "your-shader-guid",
            new VertexShaderInformation()
        );
    }

    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.TextureAndUVUsage | 
        ShaderInformationKind.VertexIndexUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        // Register textures...
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? uvMatrix = mainTexST is { } st 
            ? Matrix2x3.NewScaleOffset(st) 
            : null;

        matInfo.RegisterTextureUVUsage(
            "_MainTex", "_MainTex", UsingUVChannels.UV0, uvMatrix
        );

        // Shader uses SV_VertexID for effects
        matInfo.RegisterVertexIndexUsage();
    }
}
```

## Registration Methods {#registration-methods}

There are two ways to register Shader Information:

### Register by GUID (Recommended) {#register-by-guid}

For shader assets, use the shader's GUID:

```csharp
ShaderInformationRegistry.RegisterShaderInformationWithGUID(
    "your-shader-asset-guid",
    new YourShaderInformation()
);
```

**How to find the shader GUID:**

1. Select the shader in the Project window
2. Right-click → Copy GUID
3. Or open the `.meta` file and copy the GUID

**Advantages:**
- Works even if the shader asset isn't loaded yet
- Recommended for shader assets distributed as packages

### Register by Shader Instance {#register-by-instance}

For runtime-generated shaders or when you have the shader instance:

```csharp
Shader shader = Shader.Find("Your/Shader/Name");
ShaderInformationRegistry.RegisterShaderInformation(
    shader,
    new YourShaderInformation()
);
```

**Advantages:**
- Works for runtime-generated shaders
- Direct reference to shader instance

**Note:** If the same shader is registered with both methods, the instance registration takes precedence.

## Best Practices {#best-practices}

### 1. Use InitializeOnLoad {#practice-initonload}

Register your Shader Information in a static constructor with `[InitializeOnLoad]`:

```csharp
[InitializeOnLoad]
internal class YourShaderInformation : ShaderInformation
{
    static YourShaderInformation()
    {
        // Registration happens automatically when Unity loads
        Register();
    }
    
    private static void Register()
    {
        ShaderInformationRegistry.RegisterShaderInformationWithGUID(
            "guid", new YourShaderInformation()
        );
    }
}
```

### 2. Handle Unknown Values {#practice-unknown}

Material properties might be animated or unknown. Handle `null` values:

```csharp
// Use pattern matching
var st = matInfo.GetVector("_MainTex_ST");
Matrix2x3? uvMatrix = st is { } st2 ? Matrix2x3.NewScaleOffset(st2) : null;

// Use null-coalescing for defaults
var uvChannel = matInfo.GetFloat("_UVChannel") switch
{
    0 => UsingUVChannels.UV0,
    1 => UsingUVChannels.UV1,
    null => UsingUVChannels.UV0 | UsingUVChannels.UV1,  // Unknown
    _ => UsingUVChannels.UV0 | UsingUVChannels.UV1
};
```

### 3. Check Keywords and Properties {#practice-keywords}

Only register textures that are actually used:

```csharp
if (matInfo.IsShaderKeywordEnabled("_NORMALMAP") != false)
{
    // Keyword might be enabled, register normal map
}

if (matInfo.GetFloat("_UseEmission") != 0)
{
    // Emission is enabled, register emission map
}
```

**Note:** `!= false` checks if the value is `true` or `null` (unknown).
This conservative approach assumes features are enabled if unknown.

### 4. Provide Accurate Information {#practice-accurate}

- Only set `VertexIndexUsage` if vertex indices truly matter
- Use correct sampler states (affects texture filtering during atlasing)
- Set UV matrices to `null` if they're dynamic or animated
- Use `UsingUVChannels.NonMesh` for screen-space UVs

### 5. Test Your Implementation {#practice-test}

Test with the `AAO Merge Material` component to verify:

1. Textures can be atlased correctly
2. UV transforms are applied properly
3. No visual artifacts after optimization
4. Materials with different settings are handled correctly

## Limitations and Future Plans {#limitations}

### Current Limitations {#current-limitations}

Avatar Optimizer currently performs UV packing when:

- Texture is used by a small set of materials
- UV channel is a single channel (per material)
- UV transform is identity matrix (no scale/offset/rotation)

These restrictions may be relaxed in future versions.

### Planned Improvements {#planned-improvements}

Future versions may support:

- UV transforms with scale ≤ 1.0 and rotation in multiples of 90°
- Multiple UV channel textures
- More complex atlasing strategies

Your Shader Information will continue to work as these features are added.

## Troubleshooting {#troubleshooting}

### Textures Not Being Atlased {#troubleshoot-no-atlas}

If `AAO Merge Material` doesn't atlas your textures:

1. **Check if Shader Information is registered:**
   - Add debug logging in your static constructor
   - Verify the shader GUID is correct

2. **Verify UV matrices:**
   - Currently only identity matrices are supported for atlasing
   - Set to `Matrix2x3.Identity` or `null` if you're using `_ST` of `(1,1,0,0)`

3. **Check UV channels:**
   - Only single UV channels are currently supported per material
   - Don't combine multiple UV channels

4. **Review sampler states:**
   - Ensure sampler states are compatible with atlasing

### Runtime Errors {#troubleshoot-errors}

If you get errors about Shader Information:

1. **"The shader is already registered"**
   - Don't register the same shader multiple times
   - Use `IsInternalInformation` if you're replacing built-in information

2. **Assembly reference errors**
   - Ensure `com.anatawa12.avatar-optimizer.api.editor` is in your asmdef
   - Wrap code in `#if AVATAR_OPTIMIZER && UNITY_EDITOR`

### Visual Artifacts {#troubleshoot-artifacts}

If materials look wrong after optimization:

1. **Check UV matrices** - Verify they match the shader's actual UV transforms
2. **Verify UV channels** - Ensure you're reporting the correct UV channels
3. **Review sampler states** - Wrong wrap mode can cause texture repeating issues
4. **Test vertex index usage** - If using vertex indices, ensure you call `RegisterVertexIndexUsage()`

## Support {#support}

If you have questions or need help:

- **Discord**: [NDMF Discord] (#avatar-optimizer channel)
- **Fediverse**: [@anatawa12@misskey.niri.la][fediverse]
- **GitHub Issues**: [AvatarOptimizer Issues]

When asking for help, please include:
- Your shader code (if possible)
- Your ShaderInformation implementation
- What optimizations you're trying to achieve
- Any error messages or unexpected behavior

[Version Defines]: https://docs.unity3d.com/2019.4/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html#define-symbols
[NDMF Discord]: https://discord.gg/dV4cVpewmM
[fediverse]: https://misskey.niri.la/@anatawa12
[AvatarOptimizer Issues]: https://github.com/anatawa12/AvatarOptimizer/issues
