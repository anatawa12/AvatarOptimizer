---
title: Frequently Asked Questions
weight: 2
---

# Frequently Asked Questions {#faq}

Here is the list of frequently asked questions about AvatarOptimizer.
If you have some other questions, please feel free to ask on the [GitHub Discussions] or on [fediverse].
I'm usually use Japanese, but I can ask me with English.

## Mesh is invisible even though it is in the field of view {#mesh-is-invisible-even-though-it-is-in-the-field-of-view}

The problem that the mesh is invisible even though it is in the field of view is often due to forgetting to specify the Root Bone of the Skinned Mesh Renderer.

[`MergeSkinnedMesh`] does not automatically set the Root Bone, so you need to manually set the Root Bone of the Skinned Mesh Renderer for MergeSkinnedMesh.

If you are using [Modular Avatar], you can add [`MA Mesh Settings`] to the root of the avatar to set the Root Bone and Bounds for the entire avatar.

## The brightness of the meshes merged with `MergeSkinnedMesh` is different from other meshes {#the-brightness-of-the-meshes-merged-with-mergeskinnedmesh-is-different-from-other-meshes}

The problem that the brightness of the meshes merged with [`MergeSkinnedMesh`] is different from other meshes is often due to forgetting to specify the Anchor Override of the merged meshes.

[`MergeSkinnedMesh`] does not automatically set the Anchor Override, so you need to manually set the Anchor Override of the Skinned Mesh Renderer for MergeSkinnedMesh.

If you are using [Modular Avatar], you can add [`MA Mesh Settings`] to the root of the avatar to set the Anchor Override for the entire avatar.

## Material animations conflict when using `MergeSkinnedMesh` {#material-animations-conflict-when-using-mergeskinnedmesh}

The problem that material animations conflict when using [`MergeSkinnedMesh`] is a known bug and is currently expected to conflicts.

When merging meshes that are animating materials, be careful not to conflict when merging.
If there is a conflict, a warning will be displayed, so please check the warning.

Issue of this problem: [#340](https://github.com/anatawa12/AvatarOptimizer/issues/340)

## BlendShape animations conflict when using `MergeSkinnedMesh` {#blendshape-animations-conflict-when-using-mergeskinnedmesh}

The problem that BlendShape animations conflict when using [`MergeSkinnedMesh`] is a known bug and is currently expected to conflicts.

When merging meshes that are animating BlendShapes, be careful not to conflict when merging.

If there is a conflict, a warning will be displayed, so please check the warning.

Issue of this problem: [#568](https://github.com/anatawa12/AvatarOptimizer/issues/568)

## The behavior of the avatar is changed when using `Trace and Optimize` {#avatar-behavior-changed-when-using-trace-and-optimize}

If the behavior of the avatar is changed when using `Trace and Optimize`, it is always be a bug unless you depend on bugs in other components of AvatarOptimizer.

Please report it on [GitHub issues], [misskey][fediverse] (mastodon), [twitter], etc.

## I want to support the development of Avatar Optimizer {#i-want-to-support-the-development-of-avatar-optimizer}

If you want to support the development of Avatar Optimizer, feedback on [GitHub Discussions], bug reports, feature requests, pull requests, etc. on [GitHub issues] are welcome.

Issues with [good first issue] are relatively easy to implement. It is recommended for your first pull request.
Also, issues with [help wanted] are issues that lack developers or information. Your participation in discussions and development would be appreciated.

I also accept financial support on [github sponsors] and [booth].

[fediverse]: https://misskey.niri.la/@anatawa12
[GitHub Discussions]: https://github.com/anatawa12/AvatarOptimizer/discussions
[GitHub issues]: https://github.com/anatawa12/AvatarOptimizer/issues/new/choose
[`MergeSkinnedMesh`]: ../reference/merge-skinned-mesh/
[Modular Avatar]: https://modular-avatar.nadena.dev/ja/
[`MA Mesh Settings`]: https://modular-avatar.nadena.dev/ja/docs/reference/mesh-settings
[twitter]: https://twitter.com/anatawa12_vrc
[github sponsors]: https://github.com/sponsors/anatawa12
[booth]: https://anatawa12.booth.pm/items/4885109
[good first issue]: https://github.com/anatawa12/AvatarOptimizer/labels/good%20first%20issue
[help wanted]: https://github.com/anatawa12/AvatarOptimizer/labels/help%20wanted
