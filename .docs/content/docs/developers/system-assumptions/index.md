---
title: "System Assumptions"
weight: 50
---

# Assumptions we make for external tools

## Transform Instance Uniqueness {#transform-instance-uniqueness}

The same Transform instance is not used for multiple different purposes. For example, the Transform is used to calculate remove mesh in box.

If you create tools that manipulate transforms, ensure each Transform has a single, well-defined purpose.

## Vertex Index Usage {#vertex-index-usage}

Vertex index is not used in shaders or other plugins that run after Avatar Optimizer, unless explicitly registered to use in ShaderInformation.

Avatar Optimizer may merge meshes and change vertex indices during optimization. If your shader or tool uses vertex indices, you must register this usage via ShaderInformation API to prevent incorrect optimization.

## External Parameter Modifications {#external-parameter-modifications}

We assume parameters are not modified externally unless explicitly stated to be modified by externals (like OSC) with AssetDescription.

Avatar Optimizer optimizes based on the assumption that parameter values remain unchanged unless marked otherwise. If your tool modifies parameters at runtime, ensure they are properly registered as external parameters via AssetDescription.
