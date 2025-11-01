---
title: "System Assumptions"
weight: 50
---

# System Assumptions and Constraints

Avatar Optimizer relies on various assumptions about system behavior, platform capabilities, and data structures. Understanding these assumptions is critical for:

- Developing compatible external tools
- Contributing to Avatar Optimizer
- Understanding optimization behavior
- Debugging issues

## Documentation Location

The comprehensive list of system assumptions is maintained in the repository root:

**[ASSUMPTIONS.md](https://github.com/anatawa12/AvatarOptimizer/blob/master/ASSUMPTIONS.md)**

This document catalogs both explicit and implicit assumptions throughout the codebase, organized by category:

- **Platform and Environment**: Unity version, VRChat SDK, NDMF dependencies
- **Framework Integration**: NDMF build phases, execution order
- **Data Structures**: Transform uniqueness, immutability constraints, instance ID stability
- **API Contracts**: Component registration, shader information, mesh provider requirements
- **VRChat-Specific**: PhysBone behavior, parameter drivers, multi-pass rendering
- **Animator System**: State machine limitations, transition requirements
- **Mesh Processing**: Vertex indices, texture handling, blend shapes
- **Threading Model**: Single-threaded execution assumptions

## For External Tool Developers

If you're developing tools that integrate with Avatar Optimizer, please review the ASSUMPTIONS.md document to understand:

1. **API Requirements**: What your ComponentInformation or ShaderInformation implementations must satisfy
2. **Build Time Behavior**: When and how Avatar Optimizer runs in the NDMF pipeline
3. **Data Constraints**: What assumptions AAO makes about mesh data, components, and transforms
4. **VRChat Dependencies**: Platform-specific optimizations and behaviors

## For Contributors

When contributing to Avatar Optimizer:

1. **Review related assumptions** before modifying code
2. **Document new assumptions** in ASSUMPTIONS.md with clear location references
3. **Validate constraints** to ensure your changes don't violate documented assumptions
4. **Update documentation** if assumptions change

## Example Assumptions

Here are some key examples from the document:

### Transform Instance Uniqueness
> The same Transform instance is not used for multiple different purposes
> 
> *Location: `Editor/Processors/OriginalState.cs:10`*

This means OriginalState stores original transform matrices assuming each Transform maps to exactly one usage context.

### ComponentInformation Registration
> Your class MUST derive from `ComponentInformation<TComponent>` and MUST have constructor without parameters
>
> *Location: `API-Editor/ComponentInformation.cs:13-18`*

When implementing ComponentInformation for external components, these requirements must be satisfied.

### BlendShape Frame Ordering
> BlendShape frames must be sorted by weight
>
> *Location: `Internal/MeshInfo2/MeshInfo2.cs:1426`*

The code relies on this ordering for correct blend shape interpolation.

## Keeping Up to Date

The ASSUMPTIONS.md document is updated as the codebase evolves. Always refer to the latest version in the repository:

https://github.com/anatawa12/AvatarOptimizer/blob/master/ASSUMPTIONS.md

For questions about specific assumptions or to report missing assumptions, please open an issue on GitHub.
