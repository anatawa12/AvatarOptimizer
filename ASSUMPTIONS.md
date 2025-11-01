# Avatar Optimizer: Assumptions for External Tool Developers

This document lists the assumptions that Avatar Optimizer makes about data structures, components, and behavior. **If you are developing tools that integrate with Avatar Optimizer**, you need to be aware of these assumptions to ensure compatibility.

This document focuses on assumptions that affect external tool developers. For internal implementation details, see the source code comments.

## Table of Contents

1. [Transform and GameObject Assumptions](#transform-and-gameobject-assumptions)
2. [API Contract Requirements](#api-contract-requirements)
3. [Mesh and BlendShape Assumptions](#mesh-and-blendshape-assumptions)
4. [Component Dependency Semantics](#component-dependency-semantics)
5. [Backward Compatibility Requirements](#backward-compatibility-requirements)
6. [Property Mapping for Components](#property-mapping-for-components)
7. [External Parameters and Tool Integration](#external-parameters-and-tool-integration)

---

## Transform and GameObject Assumptions

### Transform Instance Uniqueness
**Assumption**: The same Transform instance is not used for multiple different purposes.

**Location**: `Editor/Processors/OriginalState.cs:10`

**Impact**: OriginalState stores original transform matrices assuming each Transform maps to exactly one usage context. If you create tools that manipulate transforms, ensure each Transform has a single, well-defined purpose.

### Component Instance ID Stability
**Assumption**: Unity instance IDs remain stable during the build process.

**Location**: `Editor/ObjectMapping/ObjectMapping.cs:24-48`

**Impact**: Instance IDs are used as dictionary keys throughout object mapping. External tools should not cause Unity to reassign instance IDs during the build process.

---

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

---

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

---

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

---

## Backward Compatibility Requirements

### No New Abstract Methods
You **MUST NOT** add abstract methods to public API classes (`ComponentInformation`, `ShaderInformation`), as this would break existing external implementations.

**Locations**: 
- `API-Editor/ComponentInformation.cs:102`
- `API-Editor/ShaderInformation.cs:159`

---

## Property Mapping for Components

### TryMapProperty Usage
For properties on components that are highly related to your component, use `TryMapProperty` to properly map properties during object replacement.

**Location**: `API-Editor/ComponentInformation.cs:417`

**Requirement**: You must register the property as a modified property by calling `ModifyProperties` in `CollectMutations`.

**Location**: `API-Editor/ComponentInformation.cs:428`

---

## External Parameters and Tool Integration

### AssetDescription External Parameters
**Assumption**: Avatar Optimizer assumes that parameters marked in AssetDescription might be changed by external tools.

**Location**: `Editor/AssetDescription.cs:43`

**Impact**: This affects optimization decisions for certain properties. If your tool modifies parameters, ensure they are properly registered as external parameters.

---

## Guidelines for External Tool Developers

### When Implementing ComponentInformation

1. Follow all registration requirements strictly
2. Do not implement ComponentInformation for components owned by other tools
3. Ensure proper cleanup by implementing disposal patterns
4. Document your component's dependencies clearly

### When Implementing ShaderInformation

1. Register early in the Unity load process
2. Provide accurate vertex index usage information
3. Specify UV channel usage correctly
4. Use GUID-based registration for asset shaders

### When Manipulating Meshes

1. Do not rely on vertex index stability
2. Ensure BlendShape frames are sorted by weight
3. Respect mesh data immutability constraints
4. Properly dispose of MeshRemovalProvider instances

### When Defining Dependencies

1. Use AlwaysRequired for critical dependencies
2. Use AsSerializedReference for optional dependencies
3. Register modified properties in CollectMutations
4. Implement TryMapProperty for related component properties

---

## Document Maintenance

This document should be updated whenever:
- New API contracts are added
- Assumptions affecting external tools change
- Breaking changes are introduced

For questions about specific assumptions or to report issues, please open an issue on GitHub.

**Last Updated**: 2025-11-01
**Version**: 1.9.0-beta.2
