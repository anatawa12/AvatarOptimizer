---
title: Component Scripting API
---

# Component Scripting API

Since Avatar Optimizer v1.7.0, Avatar Optimizer provides Component API to add Avatar Optimizer components to the Avatar.\
By using this API, you can create tools or components that add Avatar Optimizer components.

## Components that supported by Component API  {#supported-components}

Currently, not all components are supported by Component API.
Here is the list of components that are supported by Component API.

- `RemoveMeshInBox` - Adding component and configuring for that is supported
- `RemoveMeshByBlendShape` - Adding component and configuring for that is supported
- `RemoveMeshByMask` - Adding component and configuring for that is supported
- `MergePhysBone` - Adding component and configuring for that is supported
- `TraceAndOptimize` - Adding with default configuration is supported but configuring is not supported

For components that supports configuring, to keep compatibility with future features that enabled by default,
you need extra attention for the configuration. See document below for more details.

## Getting Started

To use Component API, you have to reference `com.anatawa12.avatar-optimizer.runtime` assembly in your Assembly Definition file.
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

<blockquote class="book-hint info">

Since Avatar Optimizer v1.9.0, Avatar Optimizer provides Component API for the `MergePhysBone` component, but it is special with semantic versioning.

This component is deeply integrated with the PhysBone component of VRChat SDK.
If the implementation of the PhysBone component is changed, corresponding changes in `MergePhysBone` may be required to maintain functionality.
In that case, we may add new properties or change existing properties of `MergePhysBone` without following semantic versioning.

1. When we add that new backward-compatible properties to `MergePhysBone` for following new PhysBone properties.\
This is typically done in bumps[^vrcsdk-versioning] version of VRChat SDK.\
We treat adding support for new PhysBone properties as "bugfix for unsupported PhysBone features", so this is done in patch version of Avatar Optimizer.

2. When we change signature of existing properties (in other words, introduce breaking changes) of `MergePhysBone` for following breaking changes of existing PhysBone properties.\
Avatar Optimizer declares compatibility with specific range of breaking versions of VRChat SDK,
so this change can only be done when Avatar Optimizer introduces support for new breaking[^vrcsdk-versioning] version of VRChat SDK.\
We work to minimize such breaking changes, but please be aware of this possibility when using Component API for `MergePhysBone`.\
In some cases, even when using the same version of Avatar Optimizer, property types and others may be different depending on used VRChat SDK version.

To protect your code from breaking changes like above, we recommend to check the VRChat SDK version with vpmDependencies.\
Breaking changes like above only happen when Avatar Optimizer introduces support for new breaking version of VRChat SDK.\
Therefore, if you lock the VRChat SDK version with vpmDependencies, your code will be safe from such breaking changes, as long as no conflict in package version happens.

</blockquote>

[^vrcsdk-versioning]: VRChat SDK uses 'Branding.Breaking.Bumps' versioning scheme where Breaking will be incremented for breaking changes, and Bumps will be incremented for backward compatible changes. See [Official Documentation][b.b.b-docs] for more details.

[b.b.b-docs]: https://vcc.docs.vrchat.com/vpm/packages/#brandingbreakingbumps
