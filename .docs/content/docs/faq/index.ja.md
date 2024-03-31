---
title: よくある質問
weight: 2
---

# よくある質問 {#faq}

AvatarOptimizerに関するよくある質問のリストです。
他に質問がある場合は、[GitHub Discussions]または[fediverse]でお気軽にお尋ねください。

## メッシュが視界の中にあるのに非表示になってしまう {#mesh-is-invisible-even-though-it-is-in-the-field-of-view}

多くの場合、この問題はSkinned Mesh Rendererの`Root Bone`を指定し忘れていることが原因です。

[`AAO Merge Skinned Mesh`]コンポーネントは`Root Bone`の設定を自動的には行わないため、統合先のメッシュ[^merged-mesh]の`Root Bone`を手動で設定する必要があります。

[Modular Avatar]を使用している場合は、アバターのルートに[`MA Mesh Settings`]コンポーネントを追加して設定することにより、アバター全体の`Root Bone`と`Bounds`を設定することができます。

## `AAO Merge Skinned Mesh`コンポーネントで統合したメッシュの明るさが他のメッシュと異なる {#the-brightness-of-the-meshes-merged-with-aao-merge-skinned-mesh-component-is-different-from-other-meshes}

多くの場合、この問題は統合先のメッシュの`Anchor Override`を指定し忘れていることが原因です。

[`AAO Merge Skinned Mesh`]コンポーネントは`Anchor Override`の設定を自動的には行わないため、統合先のメッシュ[^merged-mesh]の`Anchor Override`を手動で設定する必要があります。

[Modular Avatar]を使用している場合は、アバターのルートに[`MA Mesh Settings`]コンポーネントを追加して設定することにより、アバター全体の`Anchor Override`を設定することができます。

## `AAO Merge Skinned Mesh`コンポーネントを使用すると、マテリアルに対するアニメーションが競合する {#material-animations-conflict-when-using-aao-merge-skinned-mesh-component}

この問題は既知のバグであり、現時点では競合してしまう仕様です。
マテリアルに対してアニメーションされるメッシュを統合する際は、競合しないようご注意ください。
競合する場合は警告が表示されるので、そちらを確認してください。

この問題のissue: [#340](https://github.com/anatawa12/AvatarOptimizer/issues/340)

## `AAO Merge Skinned Mesh`コンポーネントを使用すると、BlendShapeに対するアニメーションが競合する {#blendshape-animations-conflict-when-using-aao-merge-skinned-mesh-component}

この問題は既知のバグであり、現時点では競合してしまう仕様です。
BlendShapeに対してアニメーションされるメッシュを統合する際は、競合しないようご注意ください。
競合する場合は警告が表示されるので、そちらを確認してください。

この問題のissue: [#568](https://github.com/anatawa12/AvatarOptimizer/issues/568)

## `AAO Trace and Optimize`コンポーネントを使用すると、アバターの振る舞いや見た目が変わる {#avatar-behavior-changed-when-using-aao-trace-and-optimize-component}

`AAO Trace and Optimize`コンポーネントを使用して、アバターの振る舞いや見た目が変わってしまった場合は、(アバターがAvatarOptimizerの他のコンポーネントのバグ挙動に依存していない限り、)全てバグです。
[GitHub issues]や[misskey][fediverse] (mastodon)、[twitter]などから報告をお願いします。

## Avatar Optimizerの開発を支援したい {#i-want-to-support-the-development-of-avatar-optimizer}

Avatar Optimizerの開発を支援したい場合、[GitHub Discussions]でのフィードバックや[GitHub issues]でのバグ報告、機能追加の要望、またプルリクエストなどを歓迎しています。

[good first issue]が付いているissueは、比較的実装が簡単なissueです。初めてのプルリクエストにおすすめです。
また、[help wanted]が付いているissueは、開発者や情報などが不足しているissueです。議論や開発に参加していただけると助かります。

なお、[github sponsors]や[booth]での金銭的な支援も受け付けています。

[fediverse]: https://misskey.niri.la/@anatawa12
[GitHub Discussions]: https://github.com/anatawa12/AvatarOptimizer/discussions
[GitHub issues]: https://github.com/anatawa12/AvatarOptimizer/issues/new/choose
[`AAO Merge Skinned Mesh`]: ../reference/merge-skinned-mesh/
[Modular Avatar]: https://modular-avatar.nadena.dev/
[`MA Mesh Settings`]: https://modular-avatar.nadena.dev/docs/reference/mesh-settings
[twitter]: https://twitter.com/anatawa12_vrc
[github sponsors]: https://github.com/sponsors/anatawa12
[booth]: https://anatawa12.booth.pm/items/4885109
[good first issue]: https://github.com/anatawa12/AvatarOptimizer/labels/good%20first%20issue
[help wanted]: https://github.com/anatawa12/AvatarOptimizer/labels/help%20wanted

[^merged-mesh]: 統合先のメッシュとは、`AAO Merge Skinned Mesh`コンポーネントと一緒に付いているSkinned Mesh Rendererのことです。
