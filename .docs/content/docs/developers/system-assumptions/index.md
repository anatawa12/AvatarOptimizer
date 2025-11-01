---
title: "System Assumptions"
weight: 50
---

# System Assumptions for External Tool Developers

This document lists the assumptions that Avatar Optimizer makes about data structures, components, and behavior. If you are developing tools that integrate with Avatar Optimizer, you need to be aware of these assumptions to ensure compatibility.

## Transform and GameObject Assumptions

### Transform Instance Uniqueness
**Assumption**: The same Transform instance is not used for multiple different purposes.

**Location**: `Editor/Processors/OriginalState.cs:10`

**Impact**: OriginalState stores original transform matrices assuming each Transform maps to exactly one usage context. If you create tools that manipulate transforms, ensure each Transform has a single, well-defined purpose.

### Component Instance ID Stability
**Assumption**: Unity instance IDs remain stable during the build process.

**Location**: `Editor/ObjectMapping/ObjectMapping.cs:24-48`

**Impact**: Instance IDs are used as dictionary keys throughout object mapping. External tools should not cause Unity to reassign instance IDs during the build process.

## API Contract Requirements

### ComponentInformation Registration

**Requirements**:
- Your class **MUST** derive from `ComponentInformation<TComponent>`
- Your class **MUST** have a constructor without parameters
- The type parameter **MUST** be assignable from `TargetType`
- Only **ONE** ComponentInformation can exist for each component type
- You **SHOULD NOT** declare ComponentInformation for external components (to avoid conflicts)

**Location**: `API-Editor/ComponentInformation.cs:13-21`

**Method Call Constraint**: You **MUST NOT** call methods on `IComponentDependencyInfo` after calling `AddDependency` another time, as the previous invocation context becomes invalid.

**Location**: `API-Editor/ComponentInformation.cs:214`

### ShaderInformation Registration

**Registration Timing**: You should register shader information on `InitializeOnLoad`, `RuntimeInitializeOnLoadMethod`, or in the shader constructor.

**Location**: `API-Editor/ShaderInformation.cs:15`

**Asset-based Shaders**: For shaders stored as assets, use `RegisterShaderInformationWithGUID` instead of `RegisterShaderInformation`, since loading assets might not be possible during `InitializeOnLoadAttribute` time.

**Location**: `API-Editor/ShaderInformation.cs:64`

**Default Assumption**: If you don't provide shader information, Avatar Optimizer assumes that vertex indices are **not** used by the shader.

**Location**: `API-Editor/ShaderInformation.cs:184`

**UV Tile Discard**: If your shader only uses the integer part of UV coordinates (like UV Tile Discard), you should not register the UV usage as "Other UV Usage".

**Location**: `API-Editor/ShaderInformation.cs:250`

### MeshRemovalProvider Constraints

**Mesh Data Immutability**: You should not change mesh data after creating MeshRemovalProvider, except for evacuated UV channels registered with `UVUsageCompabilityAPI`.

**Location**: `API-Editor/MeshRemovalProvider.cs:18`

**Disposal Requirement**: You should call `Dispose()` when MeshRemovalProvider is no longer needed.

**Location**: `API-Editor/MeshRemovalProvider.cs:68`

### UVUsageCompabilityAPI Usage Restrictions

**Build-Time Only**: This API is intended for Non-Destructive tools at build time (in `IVRCSDKPreprocessAvatar`). It **MUST NOT** be used from in-place edit mode tools.

**Location**: `API-Editor/UVUsageCompabilityAPI.cs:18-19`

**UV Channel Bounds**: The UV channel parameter should be 0-7 (inclusive).

**Location**: `API-Editor/UVUsageCompabilityAPI.cs:56`

## Mesh and BlendShape Assumptions

### BlendShape Frame Ordering
**Assumption**: BlendShape frames must be sorted by weight.

**Location**: `Internal/MeshInfo2/MeshInfo2.cs:1426`

**Impact**: The code relies on this ordering for correct blend shape interpolation. If your tool creates or modifies blend shapes, ensure frames are properly sorted.

### BlendShapeBuffer Immutability
**Assumption**: BlendShapeBuffer is generally immutable except for removing blendShapes.

**Location**: `Internal/MeshInfo2/MeshInfo2.cs:1292`

**Rationale**: Adding data would require creating new arrays, which is expensive.

### Vertex Index Instability Warning
**Warning**: Vertex indices may change when Avatar Optimizer merges meshes. Upgrading Avatar Optimizer may break avatars that rely on specific vertex indices.

**Location**: `Editor/Processors/TraceAndOptimize/AutoMergeSkinnedMesh.cs:104-108`

**Impact**: Users and tools must not rely on vertex index values remaining stable across optimization.

## Component Dependency Semantics

### Always Required Dependencies
Dependencies marked with `IComponentDependencyInfo.AlwaysRequired()` are required even if the dependent component is disabled.

**Location**: `API-Editor/ComponentInformation.cs:312`

### Conditional Dependencies
Dependencies marked with `IComponentDependencyInfo.AsSerializedReference()` are not required if the dependency component is disabled.

**Location**: `API-Editor/ComponentInformation.cs:334`

### Dependency Assumption
If a component can be enabled and the dependency exists, the dependency will be assumed as required.

**Location**: `API-Editor/ComponentInformation.cs:210`

## Backward Compatibility Requirements

### No New Abstract Methods
You **MUST NOT** add abstract methods to public API classes (`ComponentInformation`, `ShaderInformation`), as this would break existing external implementations.

**Locations**: 
- `API-Editor/ComponentInformation.cs:102`
- `API-Editor/ShaderInformation.cs:159`

## Property Mapping for Components

### TryMapProperty Usage
For properties on components that are highly related to your component, use `TryMapProperty` to properly map properties during object replacement.

**Location**: `API-Editor/ComponentInformation.cs:417`

**Requirement**: You must register the property as a modified property by calling `ModifyProperties` in `CollectMutations`.

**Location**: `API-Editor/ComponentInformation.cs:428`

## External Parameters and Tool Integration

### AssetDescription External Parameters
**Assumption**: Avatar Optimizer assumes that parameters marked in AssetDescription might be changed by external tools.

**Location**: `Editor/AssetDescription.cs:43`

**Impact**: This affects optimization decisions for certain properties. If your tool modifies parameters, ensure they are properly registered as external parameters.

## Notes

For a complete list of all system assumptions including internal implementation details, see [ASSUMPTIONS.md](https://github.com/anatawa12/AvatarOptimizer/blob/master/ASSUMPTIONS.md) in the repository.
