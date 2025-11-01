---
title: "System Assumptions"
weight: 50
---

# System Assumptions for External Tool Developers

This document lists key assumptions that Avatar Optimizer makes about data structures and behavior. If you are developing tools that integrate with Avatar Optimizer, be aware of these assumptions to ensure compatibility.

**Note**: For API contract requirements (ComponentInformation, ShaderInformation, etc.), please refer to the doc comments in the API source files:
- `API-Editor/ComponentInformation.cs`
- `API-Editor/ShaderInformation.cs`
- `API-Editor/MeshRemovalProvider.cs`
- `API-Editor/UVUsageCompabilityAPI.cs`

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

## Mesh and BlendShape Assumptions

### BlendShape Frame Ordering
**Assumption**: BlendShape frames must be sorted by weight.

**Location**: `Internal/MeshInfo2/MeshInfo2.cs:1426`

**Impact**: The code relies on this ordering for correct blend shape interpolation. If your tool creates or modifies blend shapes, ensure frames are properly sorted.

### Vertex Index Instability Warning
**Warning**: Vertex indices may change when Avatar Optimizer merges meshes. Upgrading Avatar Optimizer may break avatars that rely on specific vertex indices.

**Location**: `Editor/Processors/TraceAndOptimize/AutoMergeSkinnedMesh.cs:104-108`

**Impact**: Users and tools must not rely on vertex index values remaining stable across optimization.

---

## External Parameters

### AssetDescription External Parameters
**Assumption**: Avatar Optimizer assumes that parameters marked in AssetDescription might be changed by external tools.

**Location**: `Editor/AssetDescription.cs:43`

**Impact**: This affects optimization decisions for certain properties. If your tool modifies parameters, ensure they are properly registered as external parameters.
