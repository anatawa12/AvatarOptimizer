---
title: Component Scripting API
---

# Component Scripting API

Since Avatar Optimizer v1.7.0, Avatar Optimizer provides Component API to add Avatar Optimizer components to the Avatar.
By using this API, you can create tools or components that add Avatar Optimizer components.

## Components that supported by Component API  {#supported-components}

Currently, not all components are supported by Component API.
Here is the list of components that are supported by Component API.

- `RemoveMeshInBox` - Adding component and configuring for that is supported
- `RemoveMeshByBlendShape` - Adding component and configuring for that is supported
- `RemoveMeshByMask` - Adding component and configuring for that is supported
- `TraceAndOptimize` - Adding with default configuration is supported but configuring is not supported

For components that supports configuring, to keep compatibility with future features that enabled by default,
you need extra attention for the configuration. See document below for more details.

## Getting Started

To use Component API, you have to reference `com.anatawa12.avatar-optimizer.runtime` assembly in your assembly definition file.
Since Avatar Optimizer does not work on the runtime, you should not depends on `com.anatawa12.avatar-optimizer.runtime` assembly for runtime build.\
We may remove some classes from `com.anatawa12.avatar-optimizer.runtime` on runtime build in the future.
In other words, it's recommended to avoid use of `com.anatawa12.avatar-optimizer.runtime` in runtime assembly, you should use it only in editor assembly.

Second, If you want to configure components, you have to call `void Initialize(int version)` method to ensure the compatibility with future features.
The default setting of Avatar Optimizer can be changed in the future (as described in [Behavior Stability](../../basic-concept/#behavior-stability)).\
The default setting of Components will be affected to the components added with `GameObject.AddComponent<T>()` method.
Therefore, to keep behavior compability with future versions, you have to call `Initialize` method with the version of default configuration you want to use.
The default configuration version should be described in the document of the `Initialize` method.

<blockquote class="book-hint warning">

Without calling `Initialize` method, component will behave unexpectedly, or you may get error with future versions.

</blockquote>

<blockquote class="book-hint info">

Configuring component is only supported just after adding component, and configuring already existing component on the GameObject is unsupported.
This is because some future functionality might be incompatible with the existing component configuration.

For example, enabling the inversion option of `AAO Remove Mesh By Box` component, added in v1.8.0, changes the meaning of box, which makes it incompatible with tools intended only for v1.7 and earlier.

</blockquote>
