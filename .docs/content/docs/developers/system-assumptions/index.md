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

## Transform Assumptions

### Transform Instance Uniqueness
**Assumption**: The same Transform instance is not used for multiple different purposes.

**Location**: `Editor/Processors/OriginalState.cs:10`

**Impact**: OriginalState stores original transform matrices assuming each Transform maps to exactly one usage context. If you create tools that manipulate transforms, ensure each Transform has a single, well-defined purpose.

---

## Mesh Processing Assumptions

### Vertex Index Usage
**Assumption**: Vertex index is not used in shaders or other plugins that run after Avatar Optimizer, unless explicitly registered to use in ShaderInformation.

**Location**: `Editor/Processors/TraceAndOptimize/AutoMergeSkinnedMesh.cs:104-108`

**Impact**: Avatar Optimizer may merge meshes and change vertex indices during optimization. If your shader or tool uses vertex indices, you must register this usage via ShaderInformation API to prevent incorrect optimization.

---

## Parameter Modification Assumptions

### External Parameter Modifications
**Assumption**: We assume parameters are not modified externally unless explicitly stated to be modified by externals (like OSC) with AssetDescription.

**Location**: `Editor/AssetDescription.cs:43`

**Impact**: Avatar Optimizer optimizes based on the assumption that parameter values remain unchanged unless marked otherwise. If your tool modifies parameters at runtime, ensure they are properly registered as external parameters via AssetDescription.
