---
title: "System Assumptions"
weight: 50
---

# Assumptions we make for external tools

This page describes assumptions Avatar Optimizer makes about external tools and data structures. If you are developing tools that integrate with Avatar Optimizer, please ensure your tools respect these assumptions.

## Transform Instance Usage {#transform-instance-uniqueness}

The same instance of Transform is used to express the same bone in Avatar Optimizer.

Avatar Optimizer may use transform matrices from the resolving phase to determine which vertices will be removed with remove mesh in box.

If you create tools that manipulate transforms, ensure each Transform instance represents a single, consistent bone or purpose throughout the optimization process.

## Vertex Index Usage {#vertex-index-usage}

Vertex index is not used in shaders or other plugins that run after Avatar Optimizer, unless explicitly registered in ShaderInformation.

Avatar Optimizer may merge meshes and change vertex indices during optimization. If your shader or tool uses vertex indices, you must register this usage via ShaderInformation API to prevent incorrect optimization.

## External Parameter Modifications {#external-parameter-modifications}

We assume parameters are not modified externally unless explicitly stated to be modified by externals (like OSC) with AssetDescription.

Avatar Optimizer optimizes based on the assumption that parameter values remain unchanged unless marked otherwise. If your tool modifies parameters at runtime, ensure they are properly registered as external parameters via AssetDescription.
