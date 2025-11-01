# Avatar Optimizer: System Assumptions and Constraints

This document catalogs the assumptions and constraints that Avatar Optimizer's codebase relies upon. These assumptions are critical for understanding system behavior, maintaining correctness, and guiding future development.

## Table of Contents

1. [Platform and Environment Assumptions](#platform-and-environment-assumptions)
2. [Unity and NDMF Framework Assumptions](#unity-and-ndmf-framework-assumptions)
3. [Data Structure and State Management Assumptions](#data-structure-and-state-management-assumptions)
4. [API Contract Assumptions](#api-contract-assumptions)
5. [VRChat-Specific Assumptions](#vrchat-specific-assumptions)
6. [Animator and Animation Assumptions](#animator-and-animation-assumptions)
7. [Mesh and Rendering Assumptions](#mesh-and-rendering-assumptions)
8. [Object Lifecycle and Timing Assumptions](#object-lifecycle-and-timing-assumptions)
9. [Threading and Concurrency Assumptions](#threading-and-concurrency-assumptions)

---

## Platform and Environment Assumptions

### Unity Version Requirements
- **Minimum Unity Version**: Unity 2022.3 or later is required
  - Location: `package.json` - `"unity": "2022.3"`
  - Rationale: Animator optimizer requires newer C# features available in Unity 2021.3+

### VRChat SDK Dependency
- **VRChat SDK Version**: Requires VRChat SDK 3.7.0 or later, but less than 3.10.0
  - Location: `package.json` - `"com.vrchat.avatars": ">=3.7.0 <3.10.0"`
  - Impact: Many optimizations are specifically designed for VRChat's runtime behavior

### NDMF Framework Dependency
- **NDMF Version**: Requires nadena.dev.ndmf 1.8.0 or later, but less than 2.0.0
  - Location: `package.json` - `"nadena.dev.ndmf": ">=1.8.0 <2.0.0"`
  - Impact: Avatar Optimizer is built as an NDMF plugin and relies on NDMF's build pipeline

### Plugin Execution Order
- **Assumption**: Avatar Optimizer is designed to run as late as possible in the NDMF build pipeline
  - Location: `Editor/OptimizerPlugin.cs:7-19`
  - Implementation: Uses special Unicode character `\uFFDC` in namespace to ensure late alphabetical sorting
  - Rationale: NDMF sorts plugins by their full name using ordinal ordering; running late allows AAO to optimize after other plugins

---

## Unity and NDMF Framework Assumptions

### Editor-Only Context
- **Assumption**: All optimization processing occurs in Unity Editor at build time, not at runtime
  - Location: `Editor/OptimizerPlugin.cs:40` - "Run early steps before EditorOnly objects are purged"
  - Impact: Components and processors are editor-only; runtime behavior is purely from generated/modified assets

### Build Phase Separation
- **Resolving Phase**: Early processing before EditorOnly objects are removed
  - Location: `Editor/OptimizerPlugin.cs:41-54`
  - Includes: UnusedBonesByReferencesTool, MakeChildren (early), FetchOriginalState

- **Optimizing Phase**: Main optimization sequence
  - Location: `Editor/OptimizerPlugin.cs:60-132`
  - Requires specific extension contexts (MeshInfo2Context, ObjectMappingContext, DestroyTracker)

### Single-Threaded Execution
- **Assumption**: All NDMF passes execute sequentially in a single thread
  - Rationale: No explicit thread synchronization primitives found in codebase
  - Impact: No need for lock/mutex protection in processors

---

## Data Structure and State Management Assumptions

### Transform Instance Uniqueness
- **Assumption**: The same Transform instance is not used for multiple different purposes
  - Location: `Editor/Processors/OriginalState.cs:10`
  - Class: `OriginalState`
  - Impact: OriginalState stores original transform matrices assuming each Transform maps to exactly one usage

### Immutability of Data Structures

#### MeshInfo2.BlendShapeBuffer
- **Assumption**: BlendShapeBuffer is generally immutable except for removing blendShapes
  - Location: `Internal/MeshInfo2/MeshInfo2.cs:1292`
  - Rationale: Adding data would require creating new arrays, which is expensive

#### PropModNode
- **Assumption**: Most PropModNode instances are immutable, but some nodes are mutable
  - Location: `Editor/AnimatorParserV2/PropModNode.cs:51`
  - Impact: Mutation behavior must be carefully controlled in specific node types

### BlendShape Frame Ordering
- **Assumption**: BlendShape frames must be sorted by weight
  - Location: `Internal/MeshInfo2/MeshInfo2.cs:1426`
  - Impact: Code relies on this ordering for correct blendshape interpolation

### Component Instance ID Stability
- **Assumption**: Unity instance IDs remain stable during the build process
  - Location: `Editor/ObjectMapping/ObjectMapping.cs:24-48`
  - Usage: Instance IDs are used as dictionary keys throughout object mapping
  - Impact: If Unity reassigns instance IDs during build, mapping will break

### No Duplicate BlendShape Names
- **Handling**: System detects duplicate blendShape names
  - Location: `Internal/MeshInfo2/MeshInfo2.cs:1355` - "duplicated blendShape name detected"
  - Impact: Duplicate names may cause undefined behavior or errors

---

## API Contract Assumptions

### ComponentInformation Registration

#### Class Structure Requirements
- **MUST**: Derive from `ComponentInformation<TComponent>`
- **MUST**: Have a constructor without parameters
- **MUST**: Have type parameter assignable from `TargetType`
  - Location: `API-Editor/ComponentInformation.cs:13-18`

#### Uniqueness Constraint
- **REQUIRED**: Only one ComponentInformation can exist for each component type
- **SHOULD NOT**: Declare ComponentInformation for external components
  - Location: `API-Editor/ComponentInformation.cs:20-21`
  - Rationale: Prevents conflicts between different plugins

#### Method Call Order Constraint
- **MUST NOT**: Call methods on `IComponentDependencyInfo` after calling `AddDependency` another time
  - Location: `API-Editor/ComponentInformation.cs:214`
  - Rationale: Previous invocation context becomes invalid

#### Backward Compatibility Constraint
- **MUST NOT**: Add abstract methods to public API classes
  - Location: `API-Editor/ComponentInformation.cs:102`
  - Location: `API-Editor/ShaderInformation.cs:159`
  - Rationale: Would break existing external implementations

### ShaderInformation Registration

#### Registration Timing
- **SHOULD**: Register on `InitializeOnLoad`, `RuntimeInitializeOnLoadMethod`, or constructor
  - Location: `API-Editor/ShaderInformation.cs:15`

#### Asset-based Shaders
- **SHOULD**: Use `RegisterShaderInformationWithGUID` for asset shaders instead of `RegisterShaderInformation`
  - Location: `API-Editor/ShaderInformation.cs:64`
  - Rationale: Loading assets might not be possible during `InitializeOnLoadAttribute` time

#### Registration Priority
- **Behavior**: Information registered with `RegisterShaderInformation` takes precedence over `RegisterShaderInformationWithGUID`
  - Location: `API-Editor/ShaderInformation.cs:26`, `API-Editor/ShaderInformation.cs:59`

### MeshRemovalProvider Constraints

#### Mesh Data Immutability
- **SHOULD NOT**: Change mesh data after creating MeshRemovalProvider except for evacuated UV channels
  - Location: `API-Editor/MeshRemovalProvider.cs:18`
  - Exception: UV channels registered with `UVUsageCompabilityAPI` can be modified

#### Disposal Requirement
- **SHOULD**: Call `Dispose()` when MeshRemovalProvider is no longer needed
  - Location: `API-Editor/MeshRemovalProvider.cs:68`

### UVUsageCompabilityAPI Usage Restrictions

#### Build-Time Only
- **Intended Use**: Non-Destructive tools on build time (in `IVRCSDKPreprocessAvatar`)
- **MUST NOT**: Use from in-place edit mode tools
  - Location: `API-Editor/UVUsageCompabilityAPI.cs:18-19`

#### UV Channel Bounds
- **Constraint**: UV channel parameter should be 0-7 (inclusive)
  - Location: `API-Editor/UVUsageCompabilityAPI.cs:56`

#### UV Tile Discard Special Case
- **SHOULD NOT**: Register integer-only UV usage (like UV Tile Discard) as Other UV Usage
  - Location: `API-Editor/ShaderInformation.cs:250`

---

## VRChat-Specific Assumptions

### Default Vertex Index Usage
- **Assumption**: If shader information is not provided, Avatar Optimizer assumes vertex indices are **not** used
  - Location: `API-Editor/ShaderInformation.cs:184`
  - Impact: Affects mesh merging optimization decisions

### PhysBone Behavior
- **Bug/Documentation Issue**: VRChat PhysBone has unexpected behavior with certain configurations
  - Location: `Editor/Processors/TraceAndOptimize/OptimizePhysBone.cs:175`
  - Reference: Twitter thread at https://twitter.com/anatawa12_vrc/status/1918997361096786164

### Entry-Exit to BlendTree Optimization
- **Assumption**: This optimization heavily depends on VRChat's specific animator behavior
  - Location: `Internal/AnimatorOptimizer/EntryExitToBlendTree.cs:41`
  - Platform Restriction: Only runs on `WellKnownPlatforms.VRChatAvatar30`

### Multi-Pass Rendering Polygon Increase
- **Assumption**: FlattenMultiPassRendering increases polygon count in VRChat
  - Location: `Editor/Processors/TraceAndOptimize/AutoMergeSkinnedMesh.cs:87`
  - Location: `Editor/Processors/TraceAndOptimize/MergeMaterialSlots.cs:48`
  - Impact: Not suitable for Trace and Optimize optimization

### VRChat Parameter Drivers
- **Handling**: Special handling for parameter type changes from bool/int to float
  - Location: `Internal/AnimatorOptimizer/EntryExitToBlendTree.cs:1111-1113`
  - Reference: Based on NDMF PR #693

---

## Animator and Animation Assumptions

### Entry-Exit State Machine Limitations

The Entry-Exit to BlendTree optimization has strict requirements:

#### State Machine Structure
- **NO** child state machines allowed
- **NO** state machine behaviors allowed
  - Location: `Internal/AnimatorOptimizer/EntryExitToBlendTree.cs:27-28`

#### Transition Requirements
- All transitions associated with **same single parameter**
- All states connected from **entry transition**
- All states connected to **exit**
- **NO** other transitions except entry and exit
- States must leave to exit when parameter value is not listed in entry transitions
  - Location: `Internal/AnimatorOptimizer/EntryExitToBlendTree.cs:30-34`

#### State Requirements
- All states have **same write defaults value**
- If write defaults is off, all states have **same animating properties**
- States **MUST NOT** have motion time (use 1D blend tree for gesture weight instead)
  - Location: `Internal/AnimatorOptimizer/EntryExitToBlendTree.cs:38-40`

### Default Animation Controllers
- **Assumption**: Default animation controllers don't have AnimatorLayerWeightControl (as of VRCSDK 3.5.0)
  - Location: `Editor/Processors/InitializeAnimatorOptimizer.cs:32`

### RuntimeAnimatorController Conversion
- **Requirement**: RuntimeAnimatorController must be flattened to AnimatorController
  - Location: `Editor/ObjectMapping/ObjectMappingContext.cs:92`
  - Location: `Editor/Processors/InitializeAnimatorOptimizer.cs:19`
  - Rationale: Enables animator optimization on base controller instead of override controller

### Null Motion Handling
- **Assumption**: Unity Editor ignores null motion in BlendTree
  - Location: `Editor/AnimatorParserV2/AnimationParser.cs:59`
  - Impact: Null motions are skipped during animation parsing

### Unknown Parameter Handling
- **Assumption**: Unknown parameters are assumed as not satisfying tautology conditions
  - Location: `Internal/AnimatorOptimizer/AnyStateToEntryExit.cs:444`

### Zero Duration Transitions
- **Assumption**: Interruption should not happen when transition duration is zero
  - Location: `Internal/AnimatorOptimizer/EntryExitToBlendTree.cs:361`
  - Location: `Internal/AnimatorOptimizer/EntryExitToBlendTree.cs:471`

### Default State First Tick Behavior
- **Assumption**: First tick of default state should not change the behavior of layers
  - Location: `Internal/AnimatorOptimizer/AnyStateToEntryExit.cs:164`

### Condition Satisfaction
- **Assumption**: One of the conditions must be satisfied at any time
  - Location: `Internal/AnimatorOptimizer/AnyStateToEntryExit.cs:124`

---

## Mesh and Rendering Assumptions

### Error-Free Pre-checks
- **Assumption**: MergeSkinnedMesh implementation assumes no errors in previous validation checks
  - Location: `Editor/Processors/SkinnedMeshes/MergeSkinnedMeshProcessor.cs:240`
  - Impact: Processor assumes data integrity is already validated

### Vertex Index Stability
- **Assumption**: Vertex indices may change when merging meshes
- **Warning**: Not guaranteed; upgrading Avatar Optimizer may break avatars relying on specific vertex indices
  - Location: `Editor/Processors/TraceAndOptimize/AutoMergeSkinnedMesh.cs:104-108`
  - Impact: Users must not rely on vertex index values for material configuration

### Texture Information Failure Handling
- **Assumption**: When texture information retrieval fails, assume texture is used on all UV channels
- **Assumption**: Also assume those UV channels have non-texture usage
  - Location: `Editor/Processors/TraceAndOptimize/OptimizeTexture.cs:375-376`

### sRGB vs Linear Texture Format
- **Assumption**: If color consists of only 0 or 1, sRGB doesn't matter and assume it's linear
  - Location: `Editor/Processors/TraceAndOptimize/OptimizeTexture.cs:951`

### UV Discard Detection
- **Assumption**: If both UV discard properties are disabled, assume UV discard is not enabled
  - Location: `Editor/ShaderKnowledge.cs:65`

### Infinimation Technique (Future Planning)
- **Note**: Future NDMF toolchain (as of 2025-08-08) plans to use technique called "infinimation"
  - Location: `Internal/MeshInfo2/MeshInfo2.cs:866`
  - Impact: May affect how mesh animation data is handled

### Material Usage Uniqueness
- **Assumption**: The material should not be used by other renderers (in certain optimization contexts)
  - Location: `Editor/Processors/TraceAndOptimize/OptimizeTexture.cs:436`

### Non-Animated Constraint
- **Constraint**: Some objects must not be animated for certain optimizations
  - Location: `Editor/Processors/TraceAndOptimize/FindUnusedObjectsProcessor.cs:242`

### Multi-Pass Rendering and SubMesh Duplication
- **Behavior**: For non-last submesh, system duplicates submesh for multi-pass rendering
  - Location: `Internal/MeshInfo2/MeshInfo2.cs:667`

### Index Buffer Optimization Limitation
- **Note**: Currently no optimization for index buffer to apply certain optimizations perfectly
  - Location: `Internal/MeshInfo2/MeshInfo2.cs:698`

---

## Object Lifecycle and Timing Assumptions

### Cache Invalidation
- **Behavior**: ComponentInfoRegistry cache is invalidated to support newly added assets
  - Location: `Editor/OptimizerPlugin.cs:74`

### m_Enabled Property Invalidation
- **Behavior**: Changing m_Enabled is often unexpected behavior and is invalidated
- **Note**: This invalidation doesn't affect m_Enabled property of merged mesh
  - Location: `Editor/Processors/SkinnedMeshes/MergeSkinnedMeshProcessor.cs:403-405`

### User Override Assumptions in FreezeBlendShape
- **Assumption**: When blendshape values conflict, system assumes user wants to override
  - Location: `Editor/Processors/SkinnedMeshes/FreezeBlendShapeProcessor.cs:51`
  - Location: `Editor/Processors/SkinnedMeshes/FreezeBlendShapeProcessor.cs:56`

### Component Disable Dependency Requirements

#### Always Required Dependency
- Indicated by `IComponentDependencyInfo.AlwaysRequired()`
- Dependency is required even if dependent component is disabled
  - Location: `API-Editor/ComponentInformation.cs:312`

#### Conditional Dependency
- Indicated by `IComponentDependencyInfo.AsSerializedReference()`
- Dependency is not required if dependency component is disabled
  - Location: `API-Editor/ComponentInformation.cs:334`

### AssetDescription External Parameters
- **Assumption**: Avatar Optimizer assumes parameters might be changed by external tools
  - Location: `Editor/AssetDescription.cs:43`
  - Impact: Affects optimization decisions for certain properties

---

## Threading and Concurrency Assumptions

### Single-Threaded Execution Model
- **Assumption**: All Avatar Optimizer code runs in Unity's main thread
- **Evidence**: No lock, mutex, concurrent, or volatile keywords found in non-test code
- **Impact**: No thread-safety measures are implemented

### Unity API Thread Restrictions
- **Assumption**: All Unity API calls happen on the main thread during build time
- **Context**: Unity's ScriptableObject, Component, and GameObject APIs are not thread-safe

---

## Additional Implementation Notes

### Nullable Reference Types
- **Status**: Limited nullable annotation support (6 files with `#nullable enable`)
  - Files: API-Editor/ComponentInformation.cs, API-Editor/ShaderInformation.cs, Editor/Processors/OriginalState.cs, and 3 others
- **Impact**: Most of the codebase does not have explicit null-safety annotations

### Component Detection via AvatarTagComponent
- **Pattern**: When you find an instance of `AvatarTagComponent`, you can assume it's part of Avatar Optimizer
  - Location: `Runtime/AvatarTagComponent.cs:8`
  - Usage: Used to detect if Avatar Optimizer is active on an avatar

### EditSkinnedMeshComponent Internal Use
- **Note**: `EditSkinnedMeshComponent` is not expected to be used directly by end users
  - Location: `Runtime/EditSkinnedMeshComponent.cs:8`
  - Purpose: Internal component for mesh editing operations

### MergePhysBone API Status
- **Note**: When MergePhysBone becomes a public API, should consider removing INetworkID implementation
  - Location: `Runtime/MergePhysBone.cs:18`

### First Bone Natural Selection
- **Assumption**: First bone found is assumed to be the most natural bone
  - Location: `Editor/Processors/MergeBoneProcessor.cs:176`
  - Context: When multiple bones could be selected for merging

### ComponentInfo Dependency Assumptions (FinalIK)
- **Note**: FinalIK ComponentInfo has low information quality
- **Assumption**: All references are assumed to be dependencies
  - Location: `Editor/APIInternal/ComponentInfos.FinalIK.cs:7`
  - Impact: May result in over-conservative dependency detection

### Trace and Optimize Behavior Considerations
- **AutoFreezeBlendShape**: Different behavior when using MergeSkinnedMesh rename modes
  - `RenameToAvoidConflict`: Considered by AutoFreezeBlendShape
  - `UseSourceBlendShapeName`: Not considered by AutoFreezeBlendShape
  - Location: `Runtime/MergeSkinnedMesh.cs:54-66`

### Y-Size Sorting Expectation
- **Expectation**: Certain texture operations expect Y-size sorted input
  - Location: `Editor/Processors/TraceAndOptimize/OptimizeTexture.cs:1116`

---

## Guidelines for Developers

### When Adding New Features

1. **Verify Assumptions**: Check if your code introduces new assumptions or relies on existing ones
2. **Document Assumptions**: Add new assumptions to this document with clear location references
3. **Validate Constraints**: Ensure your code respects API contracts and constraints
4. **Consider Thread Safety**: Remember that all code runs single-threaded
5. **Maintain Backward Compatibility**: Don't add abstract methods to public API classes

### When Modifying Existing Code

1. **Review Related Assumptions**: Check this document for assumptions related to the code you're modifying
2. **Update Documentation**: If assumptions change, update this document
3. **Test Edge Cases**: Ensure modifications don't violate documented assumptions
4. **Validate Ordering**: For processor changes, verify execution order dependencies

### When Integrating External Components

1. **Check Component Information**: Verify ComponentInformation requirements
2. **One ComponentInfo Per Type**: Ensure no duplicate ComponentInformation exists
3. **Avoid External ComponentInfo**: Don't declare ComponentInformation for external components
4. **Respect Build Phases**: Understand Resolving vs Optimizing phase distinctions

---

## Document Maintenance

This document should be updated whenever:
- New assumptions are discovered in code review
- System architecture changes
- New optimizations are added
- API contracts are modified
- Platform requirements change

**Last Updated**: 2025-11-01
**Version**: 1.9.0-beta.2
