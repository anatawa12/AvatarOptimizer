---
title: Merge Skinned Mesh
weight: 21
---

# Merge Skinned Mesh

1つ以上のSkinnedMeshRendererやMeshRendererを1つのSkinnedMeshRendererに統合することが出来ます。

このコンポーネントは、メッシュを指定していないSkinnedMeshRendererコンポーネントがある新規GameObjectに追加してください。(分類: [Source Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#source-component))

## 利点 {#benefits}

SkinnedMeshRendererを統合することでメッシュを変形させる処理の回数が減り、負荷が軽くなります。
また、同じマテリアルを使用しているマテリアルスロットも統合することができるので、描画負荷も減らす事ができます。

## 備考 {#notes}
アニメーションでメッシュのオン・オフを個別に切り替えたりすることはできなくなりますが、マテリアルに関するアニメーションは統合前のものがそのまま機能します。

このコンポーネントはメッシュ・マテリアル・BlendShape・Boundsを設定しますが、その他の設定については変更しません。
AnchorOverride等の設定を行うには、MergeSkinnedMeshのあるGameObject上のSkinnedMeshRendererを編集してください。

また、このコンポーネントは、服のメッシュや体のメッシュを統合するのには適していますが、顔のメッシュを統合するのには適していません。  
BlendShapeは、頂点とBlendShapeの数に比例して負荷が大きくなる機能です。
顔のメッシュは一般的に多くのBlendShapeを持っているため、統合対象に含めると頂点数の増加により負荷が大きくなってしまいます。

同様に、体や服のメッシュのBlendShapeは固定・除去することを推奨します。
[Freeze BlendShape](../freeze-blendshape)コンポーネントを統合対象・統合先のSkinnedMeshRendererのいずれか(または両方)に追加して、BlendShapeを固定・除去することが出来ます。
[Trace and Optimize](../trace-and-optimize)コンポーネントの`BlendShapeを自動的に固定・削除する`によっても同様の効果を得ることが出来ます。

## 設定 {#settings}

![component.png](component.png)

### スキンメッシュレンダラー {#skinned-renderers}

統合対象のSkinnedMeshRendererの一覧です。

一番下の"None"と書いてある要素にドラッグ&ドロップすることにより対象を追加し、Noneに戻すことにより対象を一覧から取り除きます。

### 静的レンダラー {#static-renderers}

統合対象のMeshRendererの一覧です。

静的メッシュの移動・回転・変形をボーンで再現します。

一番下の"None"と書いてある要素にドラッグ&ドロップすることにより対象を追加し、Noneに戻すことにより対象を一覧から取り除きます。

### 空のレンダラーオブジェクトを削除する {#remove-empty-renderer-gameobject}

統合対象のSkinnedMeshRendererが属しているGameObjectにSkinnedMeshRenderer以外のコンポーネントが無い場合、そのGameObjectをヒエラルキーから取り除くオプションです。

### 有効無効が統合先と等しくないレンダラーを統合しない {#skip-initially-disabled-renderers}

このオプションが有効なばあい、有効無効が統合先と等しくないSkinnedMeshRendererやMeshRendererがビルド時に無効になっている場合、統合対象から除外されます。

### マテリアルの統合 {#merge-materials}

複数の(Skinned)MeshRendererで使用されているマテリアルがある場合、ここに一覧で表示されます。

`統合する`にチェックを入れることで、それらのマテリアルスロットを1つに統合します。これはDrawCallを削減します。
