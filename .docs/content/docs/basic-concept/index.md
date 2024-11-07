---
weight: 1
title: "Basic Concepts"
---

# Basic Concepts of Avatar Optimizer {#basic-concepts}

This page describes the basic concepts of Avatar Optimizer.

## What is Avatar Optimizer? {#what-is-avatar-optimizer}

Avatar Optimizer is a non-destructive tool that helps you optimize your avatar.
This tool is initially designed for VRChat avatars, but it might be usable for other Avatars supported by [NDMF].

Non-destructive means (in Avatar Optimizer):
- You don't have to unpack your prefab to apply Avatar Optimizer.
  - What you have to do is adding component.
- Avatar Optimizer doesn't affects avatar saved in your project, only affects temporal copy of avatar for building.

Please note that non-VRChat avatar support is generally community-based and may not be supported by the Avatar Optimizer dev team.


## What is the goal of Avatar Optimizer? {#what-is-the-goal-of-avatar-optimizer}

The main goal of this tool is to help you optimize your avatar in performance without affecting the appearance so much, in non-destructive way.\
This tool is not designed to make changes of the avatar behavior.

All non-configured changes in the avatar behavior is treated as a bug, even if it might be useful in some use cases.\
In some cases, we may put off the bug since it does not affect so much for most use cases.\
However, the buggy behavior may not be considered in other components.

## How is the behavior of Avatar Optimizer stable for future versions? {#behavior-stability}

Avatar Optimizer uses the [Semantic Versioning] for versioning and mostly applies the rules of Semantic Versioning to the behavior of the components.

We guarantee that the behavior of already added components will not be changed in the same major version.\
However, we may add new features for already implemented component and the behavior of newly added components may be changed.

In some bugs, fixing them may change the behavior of the component widely.\
To fix such bugs, we usually add new flag to enable the new behavior.\
By making the flag disabled by default for already added components, we keep the behavior of already added components and by making the flag enabled by default for newly added components, we fix the bug for newly added components.

There are few exceptions for component behavior stability.
- The features only for debugging the components are not guaranteed to follow the rules above.\
  For example, `Debug Options` on `AAO Trace and Optimize` component might be changed in any version.
- The features marked as experimental are not guaranteed to follow the rules above.
- The behavior of `AAO Trace and Optimize` component might be changed by implementing new optimization.\
  However, the default settings of `AAO Trace and Optimize` component will never change the behavior and appearance of your avatar, so changes must not affect the avatar.

When you encountered any unnatural or strange behavior other than these exceptions, please see [FAQ] first.

[NDMF]: https://ndmf.nadena.dev/
[Semantic Versioning]: https://semver.org/
[FAQ]: ../faq/
