---
title: よくある質問
weight: 2
---

# よくある質問 {#faq}

Avatar Optimizerに関するよくある質問のリストです。
他に質問がある場合は、[GitHub Discussions]、[NDMF Discord]、または[Fediverse (Misskey / Mastodon)][Fediverse]でお気軽にお尋ねください。

## `AAO Trace and Optimize`コンポーネントを使用すると、アバターの振る舞いや見た目が変わる {#avatar-behavior-or-appearance-changed-when-using-aao-trace-and-optimize-component}

`AAO Trace and Optimize`コンポーネントを使用して、アバターの振る舞いや見た目が変わってしまった場合は、(アバターがAvatar Optimizerの他のコンポーネントのバグ挙動に依存していない限り、)全てバグです。
[GitHub Issues]や[NDMF Discord]、[Fediverse (Misskey / Mastodon)][Fediverse]、[Twitter]などから報告をお願いします。

## メッシュが視界の中にあるのに非表示になってしまう {#mesh-is-invisible-even-though-it-is-in-the-field-of-view}

多くの場合、この問題はSkinned Mesh Rendererの`Root Bone`を指定し忘れていることが原因です。

[`AAO Merge Skinned Mesh`]コンポーネントは`Root Bone`の設定を自動的には行わないため、統合先のメッシュ[^merged-mesh]の`Root Bone`を手動で設定する必要があります。

[Modular Avatar]を使用している場合は、アバターのルートに[`MA Mesh Settings`]コンポーネントを追加して設定することにより、アバター全体の`Root Bone`と`Bounds`を設定することができます。

## `AAO Merge Skinned Mesh`コンポーネントで統合したメッシュの明るさが他のメッシュと異なる {#the-brightness-of-the-meshes-merged-with-aao-merge-skinned-mesh-component-is-different-from-other-meshes}

多くの場合、この問題は統合先のメッシュの`Anchor Override`を指定し忘れていることが原因です。\
[`AAO Merge Skinned Mesh`]コンポーネントは`Anchor Override`の設定を自動的には行わないため、統合先のメッシュ[^merged-mesh]の`Anchor Override`を手動で設定する必要があります。

[Modular Avatar]を使用している場合は、アバターのルートに[`MA Mesh Settings`]コンポーネントを追加して設定することにより、アバター全体の`Anchor Override`を設定することができます。

## `AAO Merge Skinned Mesh`コンポーネントを使用すると、マテリアルプロパティに対するアニメーションが競合する {#material-property-animations-conflict-when-using-aao-merge-skinned-mesh-component}

この問題は既知のバグであり、現時点では競合してしまう仕様です。

マテリアルプロパティに対してアニメーションされるメッシュを統合する際は、競合しないようご注意ください。
競合する場合は警告が表示されるので、そちらを確認してください。

この問題のissue: [#340](https://github.com/anatawa12/AvatarOptimizer/issues/340)

## `AAO Merge Skinned Mesh`コンポーネントを使用すると、マテリアルスロットに対するアニメーションが競合する {#material-slot-animations-conflict-when-using-aao-merge-skinned-mesh-component}

`AAO Merge Skinned Mesh`コンポーネントは、統合対象のメッシュで同じマテリアルを使用しているマテリアルスロットをデフォルトで統合します。
これにはアニメーションされるマテリアルスロットも含まれます。

アニメーションによってそれぞれのマテリアルを別のものに置き換える場合は、`AAO Merge Skinned Mesh`コンポーネントの`マテリアルの統合`の`統合する`をオフにしてください。

## `AAO Merge Skinned Mesh`コンポーネントを使用すると、BlendShapeに対するアニメーションが競合する {#blendshape-animations-conflict-when-using-aao-merge-skinned-mesh-component}

この問題は Avatar Optimizer 1.8.0 で修正されています。
Avatar Optimizer 1.8.0 以降にアップデートしてください。

## OSCギミックで使用されているPhysBone / Contact Receiverが動作していない {#physbones-contact-receivers-that-are-used-in-the-osc-based-gimmick-are-not-working}

`AAO Trace and Optimize`コンポーネントは、アバターの振る舞いを変えないように慎重に設計されています。
しかし、技術的な理由から、`AAO Trace and Optimize`コンポーネントはPhysBone / Contact ReceiverコンポーネントがOSCギミックで使用されているかどうかを判断することができません。

最近のアバターは、PhysBone / Contact Receiverコンポーネントを使用した独自のギミックを持っていることがあるため、これらのコンポーネントを削除し忘れることがよくあります。
そのため、`AAO Trace and Optimize`は、そのようなコンポーネントがOSCギミックで使用されていないと仮定して、それらが他の用途で使われていなければ削除します。

この仮定は正しいとは限らないため、PhysBone / Contact Receiverコンポーネントが(AnimatorやExpression Menuを使用せずに)OSCギミックで使用されている場合には、[Asset Description]ファイルを作成し、[Parameters Read By External Tools]を設定してください。

そのギミックが公開・販売されているような場合には、そのギミックとAAOの互換性を改善するために、作成した Asset Descriptionファイルを共有していただけると助かります。
そのAsset Descriptionを将来のAvatar Optimizerに組み込むことにより、そのギミックが正しく動作するようになるかもしれません。
共有していただける場合は、[GitHub]、[NDMF Discord]、[Fediverse (Misskey / Mastodon)][Fediverse]、[Twitter]などからご連絡ください。

## ビルド前のハードリミットチェックのせいでアバターをアップロードできない {#i-cannot-upload-the-avatar-because-of-pre-build-hard-limit-check}

これは、VRCSDKのコントロールパネルにあるアップロードボタンが、シーン上のアバターがハードリミットを超えている場合には押せないようになっていたためです。

しかし、VRChat SDK 3.8.1以降では、シーン上のアバターがハードリミットを超えていても、アバターのビルドとアップロードを開始できるようになりました。
そのため、この問題を解決するための推奨される方法は、VRCSDKを最新バージョンに更新することです。

シーン上のアバターがハードリミットを超えている場合でも、VRCSDKコントロールパネルを使用せずにアップロードプロセスを開始できる外部ツールを使用することもできます。

## Avatar Optimizerの開発を支援したい {#i-want-to-support-the-development-of-avatar-optimizer}

Avatar Optimizerの開発を支援したい場合、[GitHub Discussions]でのフィードバックや[GitHub Issues]でのバグ報告、機能追加の要望、またプルリクエストなどを歓迎しています。

[good first issue]が付いているissueは、比較的実装が簡単なissueです。初めてのプルリクエストにおすすめです。
また、[help wanted]が付いているissueは、開発者や情報などが不足しているissueです。議論や開発に参加していただけると助かります。

なお、[GitHub Sponsors]や[Booth]での金銭的な支援も受け付けています。

[Fediverse]: https://misskey.niri.la/@anatawa12
[GitHub Discussions]: https://github.com/anatawa12/AvatarOptimizer/discussions
[GitHub Issues]: https://github.com/anatawa12/AvatarOptimizer/issues/new/choose
[`AAO Merge Skinned Mesh`]: ../reference/merge-skinned-mesh/
[Modular Avatar]: https://modular-avatar.nadena.dev/
[`MA Mesh Settings`]: https://modular-avatar.nadena.dev/docs/reference/mesh-settings
[Twitter]: https://twitter.com/anatawa12_vrc
[GitHub Sponsors]: https://github.com/sponsors/anatawa12
[Booth]: https://anatawa12.booth.pm/items/4885109
[good first issue]: https://github.com/anatawa12/AvatarOptimizer/labels/good%20first%20issue
[help wanted]: https://github.com/anatawa12/AvatarOptimizer/labels/help%20wanted
[NDMF Discord]: https://discord.gg/dV4cVpewmM
[GitHub]: https://github.com/anatawa12/AvatarOptimizer/
[Asset Description]: ../developers/asset-description/
[Parameters Read By External Tools]: ../developers/asset-description/#parameters-read-by-external-tools

[^merged-mesh]: 統合先のメッシュとは、`AAO Merge Skinned Mesh`コンポーネントと一緒に付いているSkinned Mesh Rendererのことです。
