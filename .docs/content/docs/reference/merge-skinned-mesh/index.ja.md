---
title: Merge Skinned Mesh
weight: 21
---

# Merge Skinned Mesh (MergeSMR) {#merge-skinned-mesh}

1つ以上のSkinnedMeshRendererやMeshRendererを1つのSkinnedMeshRendererに統合することが出来ます。

このコンポーネントは、メッシュを指定していないSkinnedMeshRendererコンポーネントがある新規GameObjectに追加してください。(分類: [Source Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#source-component))

<blockquote class="book-hint info">

[Trace And Optimize](../trace-and-optimize)が自動で同様の処理を行うため、大抵の場合、このコンポーネントを使用する必要はありません。

</blockquote>

## 利点 {#benefits}

SkinnedMeshRendererを統合することでメッシュを変形させる処理の回数が減り、負荷が軽くなります。
また、同じマテリアルを使用しているマテリアルスロットも統合することができるので、描画負荷も減らす事ができます。

## 備考 {#notes}

アニメーションでメッシュのオン・オフを個別に切り替えたりすることはできなくなりますが、マテリアルに関するアニメーションは統合前のものがそのまま機能します。

このコンポーネントはメッシュ・マテリアル・BlendShape・Boundsを設定しますが、その他の設定については変更しません。
Anchor Override等の設定を行うには、MergeSkinnedMeshのあるGameObject上のSkinnedMeshRendererコンポーネントを編集してください。

<blockquote class="book-hint info">

[Modular Avatar]を使用している場合は、アバターのルートに[`MA Mesh Settings`]コンポーネントを追加して設定することにより、アバター全体のAnchor Override等をまとめて設定することができます。

</blockquote>

BlendShapeによる負荷を減らすために、体や服のメッシュのBlendShapeは固定・除去することを推奨します。\
[Freeze BlendShape](../freeze-blendshape)コンポーネントを統合対象・統合先のSkinnedMeshRendererコンポーネントのいずれか(または両方)に追加して、BlendShapeを固定・除去することが出来ます。
[Trace and Optimize](../trace-and-optimize)コンポーネントの`BlendShapeを最適化する`によっても同様の効果を得ることが出来ます。

以前のAvatar Optimizerは顔のメッシュを他のメッシュと統合することを推奨していませんでした。
これは、Unity 2019でBlendShapeの多いメッシュを統合するとメッシュの負荷が大幅に増加してしまうためです。\
Unity 2022ではBlendShapeの負荷が改善されているため、その記述は取り下げられました。

## 設定 {#settings}

![component.png](component.png)

### スキンメッシュレンダラー {#skinned-renderers}

統合対象のSkinnedMeshRendererの一覧です。

一番下の"None"と書いてある要素にドラッグ&ドロップすることにより対象を追加し、Noneに戻すことにより対象を一覧から取り除きます。

<div id="static-renderers"></div>

### 基本レンダラー {#basic-renderers}

統合対象のMeshRendererの一覧です。

静的メッシュの移動・回転・変形をボーンで再現します。

一番下の"None"と書いてある要素にドラッグ&ドロップすることにより対象を追加し、Noneに戻すことにより対象を一覧から取り除きます。

### 空のレンダラーオブジェクトを削除する {#remove-empty-renderer-gameobject}

統合対象のSkinnedMeshRendererが属しているGameObjectにSkinnedMeshRenderer以外のコンポーネントが無い場合、そのGameObjectをヒエラルキーから取り除くオプションです。

### 有効無効状態が統合先と異なるレンダラーを統合しない {#skip-enablement-mismatched-renderers}

統合先のSkinnedMeshRendererと有効無効の状態が異なる(Skinned)MeshRendererが統合対象の中に含まれている場合、それらをビルド時に統合対象から除外するオプションです。

### 有効無効状態に関するアニメーションをコピーする {#copy-enablement-animation}

統合対象の(Skinned)MeshRendererの有効無効状態に関するアニメーションを統合先のSkinnedMeshRendererにコピーするオプションです。

この機能は、統合先のSkinnedMeshRendererの`enabled`プロパティや、そのGameObjectや祖先のGameObjectの`activeSelf`プロパティのアニメーションをコピーします。
ただし、アニメーションされているプロパティは1種類しかコピーできないため、複数種類/階層のプロパティがアニメーションされている場合(`enabled`と`activeSelf`の両方がアニメーションされている場合や、自身と親の両方の`activeSelf`がアニメーションされている場合など)はエラーになります。

なお、統合先のSkinnedMeshRendererの`enabled`に対するアニメーションはこの機能によって上書きされるため、この機能を使用する時は統合先のSkinnedMeshRendererの`enabled`をアニメーションしてはいけません。

### BlendShapeモード {#blendshape-mode}

BlendShapeをどのように扱うかについてのオプションです。

- `BlendShape名を自動変更して重複を避ける`: 重複を避けるために、BlendShape名を自動で変更します。これはデフォルトの設定になっています。
- `同名のBlendShapeを統合する`: 同じ名前のBlendShapeを統合します。異なるSkinnedMeshRendererにある同じ名前のBlendShapeを統合する際に便利です。
- `v1.7.x互換モード`: v1.7.x以前のAvatar Optimizerとの互換性を維持するためのモードです。同じ名前のBlendShapeが統合されますが、Trace and Optimizeの判断では考慮されません。また、新しく追加したコンポーネントでこのモードを選択することはできません。

### マテリアルの統合 {#merge-materials}

複数の(Skinned)MeshRendererで使用されているマテリアルがある場合、ここに一覧で表示されます。

`統合する`にチェックを入れることで、それらのマテリアルスロットを1つに統合します。これはDrawCallを削減します。

[Modular Avatar]: https://modular-avatar.nadena.dev/ja
[`MA Mesh Settings`]: https://modular-avatar.nadena.dev/ja/docs/reference/mesh-settings
