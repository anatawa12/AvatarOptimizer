---
weight: 1
title: "Basic Concepts"
---

# Basic Concepts of Avatar Optimizer {#basic-concepts}

This page describes the basic concepts of Avatar Optimizer.

## What is Avatar Optimizer? {#what-is-avatar-optimizer}

Avatar Optimizer is a tool that helps you optimize your avatar.
This tool is initially designed for VRChat avatars, but it might be usable for other Avatars supported by [NDMF].

Non-VRChat avatar support is completely community-based and may not be supported by the Avatar Optimizer dev team.

## What is the goal of Avatar Optimizer? {#what-is-the-goal-of-avatar-optimizer}

This tool is designed to help you optimize your avatar in performance without affecting the appearance so much.\
Therefore, it is not intended to make changes of the avatar behavior.

All non-configured changes in the avatar behavior is treated as a bug, even if it might be useful in some use cases.\
In some cases, we may put off the bug since it does not affect so much for most use cases.\
However, the buggy behavior may not be considered in other components.

For example, `AAO Merge Skinned Mesh` component in 1.7.x or older merges BlendShapes which have the same name.\
This behavior is treated as a bug since this makes impossible to animate them separately.\
You may use this bug to sync the BlendShape animation of an Skinned Mesh Renderer with one of another Skinned Mesh Renderer.\
However, this is not supported behavior and some other components may break the behavior.\
For example, `Automatically Freeze BlendShape` in `AAO Trace and Optimize` component will freezes the BlendShapes which might be animated with this buggy behavior by being merged by `AAO Merge Skinned Mesh` component.

## How is the behavior of Avatar Optimizer stable for future versions? {#behavior-stability}

Avatar Optimizer uses the [Semantic Versioning] for versioning and mostly applies the rules of Semantic Versioning to the behavior of the components.

We guarantee that the behavior of already added components will not be changed in the same major version.\
However, we may add new features for already implemented component and the behavior of newly added components may be changed.

In some bugs, fixing them may change the behavior of the component widely.\
To fix such bugs, we usually add new flag to enable the new behavior.\
By making the flag disabled by default for already added components, we keep the behavior of already added components and by making the flag enabled by default for newly added components, we fix the bug for newly added components.

There are few exceptions for component behavior stability.
- The features only for debugging the components are not guaranteed to follow the rules above.\
  For example, `Advanced Options` on `AAO Trace and Optimize` component might be changed in any version.
- The features marked as experimental are not guaranteed to follow the rules above.
- The behavior of `AAO Trace and Optimize` component might be changed by implementing new optimization.\
  However, the default settings of `AAO Trace and Optimize` component will never change the behavior and appearance of your avatar, so changes must not affect the avatar.

When you encountered any unnatural or strange behavior other than these exceptions, please see [FAQ] first.

[NDMF]: https://ndmf.nadena.dev/
[Semantic Versioning]: https://semver.org/
[FAQ]: ../faq/
