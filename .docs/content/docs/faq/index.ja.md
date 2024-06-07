---
title: よくある質問
weight: 2
---

# よくある質問 {#faq}

Avatar Optimizerに関するよくある質問のリストです。
他に質問がある場合は、[GitHub Discussions]または[Fediverse (Misskey / Mastodon)][Fediverse]でお気軽にお尋ねください。

## `AAO Trace and Optimize`コンポーネントを使用すると、アバターの振る舞いや見た目が変わる {#avatar-behavior-or-appearance-changed-when-using-aao-trace-and-optimize-component}

`AAO Trace and Optimize`コンポーネントを使用して、アバターの振る舞いや見た目が変わってしまった場合は、(アバターがAvatar Optimizerの他のコンポーネントのバグ挙動に依存していない限り、)全てバグです。
[GitHub Issues]や[Fediverse (Misskey / Mastodon)][Fediverse]、[Twitter]などから報告をお願いします。

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

この問題は既知のバグであり、現時点では競合してしまう仕様です。

BlendShapeに対してアニメーションされるメッシュを統合する際は、競合しないようご注意ください。
競合する場合は警告が表示されるので、そちらを確認してください。

この問題のissue: [#568](https://github.com/anatawa12/AvatarOptimizer/issues/568)

## OSCギミックで使用されているPhysBone / Contact Receiverが動作していない {#physbones-contact-receivers-that-are-used-in-the-osc-based-gimmick-are-not-working}

`AAO Trace and Optimize`コンポーネントは、アバターの振る舞いを変えないように慎重に設計されています。
しかし、技術的な理由から、`AAO Trace and Optimize`コンポーネントはPhysBone / Contact ReceiverコンポーネントがOSCギミックで使用されているかどうかを判断することができません。

最近のアバターは、PhysBone / Contact Receiverコンポーネントを使用した独自のギミックを持っていることがあるため、これらのコンポーネントを削除し忘れることがよくあります。
そのため、`AAO Trace and Optimize`は、そのようなコンポーネントがOSCギミックで使用されていないと仮定して、それらが他の用途で使われていなければ削除します。

もちろん、仮定が正しいとは限らないため、PhysBone / Contact ReceiverコンポーネントをOSCギミックに使用している場合には、それらが使用されていると分かるようにアバターを調整してもらわなければならないかもしれません。\
`AAO Trace and Optimize`は、アバター内のAnimatorにあるパラメーターと同じパラメーターが使用されているPhysBone / Contact Receiverコンポーネントを削除しないようになっています。
そのため、OSCギミックで使用されるパラメーターを、Animator Controllerのパラメーター一覧やExpression Parameterに追加することで、これらのコンポーネントが削除されないようになります。

また、改善のための議論の材料として、OSCギミックで使われているPhysBoneやContact Receiverが削除された場合には、それらに設定されているパラメータ名を教えていただけると助かります。
将来的に、有名なOSCギミックによって使用されるパラメーターの一覧を実装して、それらのパラメータが使用されている場合はコンポーネントを保持するような形を取るかもしれませんし、他の方法で対処するかもしれません。\
以下のissueや[Fediverse (Misskey / Mastodon)][Fediverse]、[Twitter]などでお気軽にお知らせください。

この問題のissue: [#1090](https://github.com/anatawa12/AvatarOptimizer/issues/1090)

## ビルド前のハードリミットチェックのせいでアバターをアップロードできない {#i-cannot-upload-the-avatar-because-of-pre-build-hard-limit-check}

Avatar Optimizerなどの非破壊的なアバター改変ツールを使うと、元々ハードリミットを超えているようなアバターでも、ビルド後にはハードリミットを超えないようになることがあります。
しかし、VRCSDKのコントロールパネルにあるアップロードボタンは、シーン上のアバターがハードリミットを超えている場合には押せないようになっています。\
そのような場合でも、以下のような方法を使って、ビルド前のハードリミットチェックを飛ばしてアップロードすることができます。
(ビルド後のハードリミットチェックも存在するため、ビルド後にハードリミットを超える場合は以下の方法でもアップロードに失敗します。)

- `Manual bake avatar`で生成したアバターをアップロードする。\

  アバターのGameObjectを右クリックして出てくるメニューの`NDM Framework`から`Manual bake avatar`をクリックすると、非破壊ツールによる処理を手動で適用することができます。
  `Manual bake avatar`は初めにアバターを複製し、その複製に対して非破壊ツールの処理を適用させるため、元のアバターは変更されないままになります。
- Sayamame-beansの[Upload without pre-check]を使用してアップロードする。

  [Upload without pre-check]は、ビルド前のハードリミットチェックをスキップしてアップロードするためのツールです。
- kurotuの[VRCQuestTools]を使用する

  [VRCQuestTools]は、アバターをAndroid / Quest対応アバターに変換するためのツールです。\
  このツールには、ビルド前チェックをスキップしてアバターをAndroid向けにビルドするための[VQT Avatar Builder]という機能が含まれています。

[Upload without pre-check]: https://github.com/Sayamame-beans/Upload-without-preCheck?tab=readme-ov-file#upload-without-pre-check
[VRCQuestTools]: https://kurotu.github.io/VRCQuestTools/
[VQT Avatar Builder]: https://kurotu.github.io/VRCQuestTools/docs/references/main-menu/show-avatar-builder

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

[^merged-mesh]: 統合先のメッシュとは、`AAO Merge Skinned Mesh`コンポーネントと一緒に付いているSkinned Mesh Rendererのことです。
