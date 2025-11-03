---
title: "System Assumptions"
weight: 50
---

# Assumptions we make for external tools

This page describes assumptions Avatar Optimizer makes about external tools and data structures. If you are developing tools that integrate with Avatar Optimizer, please ensure your tools respect these assumptions.

## Transform Instance Usage {#transform-instance-usage}

The same instance of Transform is used to express the same bone in Avatar Optimizer.

Avatar Optimizer may use transform matrices from the resolving phase to determine which vertices will be removed with remove mesh in box.

If you create tools that manipulate transforms, ensure each Transform instance represents a single, consistent bone or purpose throughout the optimization process.

## Vertex Index Usage {#vertex-index-usage}

Vertex indices are not used in shaders or other plugins that run after Avatar Optimizer, unless explicitly registered in ShaderInformation.

Avatar Optimizer may merge meshes and change vertex indices during optimization. If your shader or tool uses vertex indices, you must register this usage via ShaderInformation API to prevent incorrect optimization.

## External Parameter Usage {#external-parameter-usage}

We assume parameters are not modified or read by external tools unless explicitly declared in AssetDescription.

Avatar Optimizer optimizes based on the assumption that parameter values are not modified or read externally unless marked otherwise. If your tool modifies or reads parameters at runtime (like OSC), ensure they are properly registered as external parameters via AssetDescription.
