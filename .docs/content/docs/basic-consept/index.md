---
weight: 1
title: "Basic Concepts"
---

# Basic Concepts of Avatar Optimizer {#basic-concepts}

This page explains the basic concepts of Avatar Optimizer.

## What is Avatar Optimizer? {#what-is-avatar-optimizer}

Avatar Optimizer is a tool that helps you optimize your avatar.
This tool is initially designed for VRChat avatars, but it might be used for other Avatars supported by [NDMF].

## What is the goal of Avatar Optimizer? {#what-is-the-goal-of-avatar-optimizer}

This tool is designed to help you optimize your avatar in performance without affecting the appearance so much.\
This tool is not designed to make change behavior of the avatar.

All non-configured changes in the behavior of the avatar is treated as a bug, even if it might be useful some use cases.\
In some cases, we may put off the bug since it does not affects much for most use cases.\
However, the buggy behavior may not be considered in other components.

For example, MergeSkinnedMesh in 1.7.x or older merges BlendShapes.\
This behavior is treated as bug since this makes impossibe to animate the BlendShapes separately.\
We may use this bug to sync the blendShape animation of an Skinned Mesh Renderer with another Skinned Mesh Renderer.\
However, this is not supported behavior and some other components may break the behavior.\
For example, `Automatically Freeze BlendShape` in Trace and Optimize will freezes the blendShapes that might be merged by Merge Skinned Mesh.

## How is the behavior of Avatar Optimizer stable for future versions? {#behavior-stability}

The Avatar Optimizer uses the [Semantic Versioning] for versioning and mostly applies the rules of Semantic Versioning to the behavior of the components.

We guarantee that the behavior of already added components will not be changed in the same major version.\
However, we may add new features for existing component types and behavior of newly added components may be changed.

In some bugs, fixing the bug may change the behavior of the component widely.\
To fix such bugs, we usually add new flag to enable the new behavior.\
By making the flag disabled by default for existing components, we keep the behavior of the existing components and by making the flag enabled by default for newly added components, we fix the bug for new components.

There are few exception for this behavior stability.
- The features only for debugging the components are not guaranteed to follow the rules above.\
  For example, Advanced Options on the Trace and Optimize might be changed in any version.
- The features marked as experimental are not guaranteed to follow the rules above.
- The behavior of `Trace and Optimize` component might be changed by implementing new optimization.\
  However, the default settings of `Trace and Optimize` component will never change the behavior of your avatar, so changes must not affect the avatar.
  (If your avatar behavior is changed by the `Trace and Optimize` component, please report it as a bug.)

[NDMF]: https://ndmf.nadena.dev/
[Semantic Versioning]: https://semver.org/
