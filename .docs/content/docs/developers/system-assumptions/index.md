---
title: "System Assumptions"
weight: 50
---

# Assumptions we make for external tools

This page describes assumptions Avatar Optimizer makes about external tools and data structures.\
If you are developing tools that integrate with Avatar Optimizer, please ensure your tools respect these assumptions.

## Transform Instance Usage {#transform-instance-usage}

The same instance of Transform is used to express the same bone in Avatar Optimizer.

Avatar Optimizer may use transform matrices from the Resolving phase to determine which vertices will be removed with Remove Mesh By Box.

If you create tools that manipulate transforms, ensure each Transform instance represents a single, consistent bone or purpose throughout the optimization process.

## Vertex Index Usage {#vertex-index-usage}

Unless explicitly registered in ShaderInformation, vertex indices are treated as not being used in shaders or other plugins that run after Avatar Optimizer.

Avatar Optimizer may merge meshes and change vertex indices during optimization.
If your shader or tool uses vertex indices, you must register this usage via [ShaderInformation API][ShaderInformation] to prevent incorrect optimization.

[ShaderInformation]: ../shader-information

## External Parameter Usage {#external-parameter-usage}

Avatar Optimizer may optimize unnecessary animations and other objects based on the usage and value range of animator parameters.\
However, parameters used by external tools cannot be reliably inferred from the avatar data, so unless explicitly declared in AssetDescription, they are treated as not being externally modified or read.

If your tool modifies or reads parameters at runtime (like OSC), ensure they are properly registered as externally used parameters via [AssetDescription].

[AssetDescription]: ../asset-description
