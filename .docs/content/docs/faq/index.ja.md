---
title: よくある質問
weight: 2
---

# よくある質問 {#faq}

AvatarOptimizerに関するよくある質問のリストです。
他に質問がある場合は、[GitHub Discussions]または[fediverse]でお気軽にお尋ねください。

## メッシュが視界の中にあるのに非表示になってしまう {#mesh-is-invisible-even-though-it-is-in-the-field-of-view}

メッシュが視界の中にあるのに非表示になってしまう問題は、多くの場合、Skinned Mesh RendererのRoot Boneの指定しわすれである事が多いです。

[`MergeSkinnedMesh`]はRoot Boneを自動的に設定しないため、MergeSkinnedMesh用のSkinned Mesh RendererのRoot Boneも手動で設定する必要があります。

もし[Modular Avatar]を使用している場合には、アバターのルートに[`MA Mesh Settings`]を追加して、Root Boneを設定することでアタバー全体のRoot BoneとBoundsを設定することができます。

## `MergeSkinnedMesh`で結合したメッシュの明るさが他のメッシュと異なる {#the-brightness-of-the-meshes-merged-with-mergeskinnedmesh-is-different-from-other-meshes}

[`MergeSkinnedMesh`]で結合したメッシュの明るさが他のメッシュと異なる問題は、多くの場合、結合したメッシュのAnchor Overrideの指定し忘れが原因です。

[`MergeSkinnedMesh`]はAnchor Overrideを自動的に設定しないため、MergeSkinnedMesh用のSkinned Mesh RendererのAnchor Overrideも手動で設定する必要があります。

もし[Modular Avatar]を使用している場合には、アバターのルートに[`MA Mesh Settings`]を追加して、Anchor Overrideを設定することでアタバー全体のAnchor Overrideを設定することができます。

## `MergeSkinnedMesh`を使用するとマテリアルのアニメーションが競合する {#material-animations-conflict-when-using-mergeskinnedmesh}

[`MergeSkinnedMesh`]を使用するとマテリアルのアニメーションが競合する問題は、既知のバグで、現在のところは競合するのが仕様です。
マテリアルのアニメーションを行っているメッシュを結合する際には、競合しないように気をつけて結合してください。
競合する場合には警告が表示されるので、警告を確認してください。

この問題のissue: [#340](https://github.com/anatawa12/AvatarOptimizer/issues/340)

## `MergeSkinnedMesh`を使用するとBlendShapeのアニメーションが競合する {#blendshape-animations-conflict-when-using-mergeskinnedmesh}

[`MergeSkinnedMesh`]を使用するとBlendShapeのアニメーションが競合する問題は、既知のバグで、現在のところは競合するのが仕様です。
BlendShapeのアニメーションを行っているメッシュを結合する際には、競合しないように気をつけて結合してください。
競合する場合には警告が表示されるので、警告を確認してください。

この問題のissue: [#568](https://github.com/anatawa12/AvatarOptimizer/issues/568)

## `Trace and Optimize`を使用するとアバターの振る舞いが変わる {#avatar-behavior-changed-when-using-trace-and-optimize}

`Trace and Optimize`を使用するとアバターの振る舞いが変わる場合は、AvatarOptimizerの他のコンポーネントのバグに依存していない限り、バグです。
[GitHub issues], [misskey][fediverse] (mastodon), [twitter] などで報告してください。

## Avatar Optimizerの開発を支援したい {#i-want-to-support-the-development-of-avatar-optimizer}

Avatar Optimizerの開発を支援したい場合は、[GitHub Discussions]でのフィードバックや[GitHub issues]でのバグ報告、機能リクエスト、プルリクエスト等を歓迎します。

[good first issue]がついたissueは比較的実装が簡単なissueです。初めてのプルリクエストにおすすめです。
また、[help wanted]がついたissueは開発者や情報などが足りないissueです。議論や開発に参加していただけると助かります。

[github sponsors] や [booth] で金銭的な支援も受け付けています。

[fediverse]: https://misskey.niri.la/@anatawa12
[GitHub Discussions]: https://github.com/anatawa12/AvatarOptimizer/discussions
[GitHub issues]: https://github.com/anatawa12/AvatarOptimizer/issues/new/choose
[`MergeSkinnedMesh`]: ../reference/merge-skinned-mesh/
[Modular Avatar]: https://modular-avatar.nadena.dev/
[`MA Mesh Settings`]: https://modular-avatar.nadena.dev/docs/reference/mesh-settings
[twitter]: https://twitter.com/anatawa12_vrc
[github sponsors]: https://github.com/sponsors/anatawa12
[booth]: https://anatawa12.booth.pm/items/4885109
[good first issue]: https://github.com/anatawa12/AvatarOptimizer/labels/good%20first%20issue
[help wanted]: https://github.com/anatawa12/AvatarOptimizer/labels/help%20wanted
