---
title: Merge Skinned Mesh
weight: 1
---

# Merge Skinned Mesh

1つ以上のSkinnedMeshRendererやMeshRendererを1つのSkinnedMeshRendererに統合(マージ)することが出来ます。

MergeSkinnedMeshは、メッシュを指定していないSkinnedMeshRendererを持つ新しいGameObjectに追加するべきです。

このコンポーネントはメッシュ・ボーン・ブレンドシェイプを統合しますが、その他の設定については変更しないため、AnchorOverride等の設定を行うには、MergeSkinnedMeshのあるGameObject上のSkinnedMeshRendererを編集してください。

ブレンドシェイプは頂点の数に比例して重くなる機能です。
SkinedMeshの統合は頂点数を増加させるため、値を動的に変化させないブレンドシェイプは統合の前(または後)に固定・除去することを推奨します。
この操作を行うために、[Freeze BlendShape](../freeze-blendshape)コンポーネントがあります。統合対象・統合先のSkinnedMeshRendererのいずれか(または両方)にFreeze BlendShapeコンポーネントを追加する事が出来ます。
Animation等により値を動的に変化させるブレンドシェイプがある場合はSkinnedMeshを統合し過ぎない方が良いでしょう。
例えば、顔と身体のメッシュが別れている場合、顔のブレンドシェイプは表情を変更するために使用されますが、身体のブレンドシェイプは使用されないため、顔と身体のメッシュを統合するべきではありません。
このコンポーネントは、服と体をマージするのに適しています。

## 設定 {#settings}

![component.png](component.png)

### スキンメッシュレンダラー {#skinned-renderers}

統合対象のSkinnedMeshRendererの一覧です。

一番下の"None"と書いてある要素にドラッグ&ドロップすることにより対象を追加し、Noneに戻すことにより対象を一覧から取り除きます。

## 静的レンダラー {#static-renderers}

統合対象のMeshRendererの一覧です。

静的メッシュの移動・回転・変形をボーンで再現します。

一番下の"None"と書いてある要素にドラッグ&ドロップすることにより対象を追加し、Noneに戻すことにより対象を一覧から取り除きます。

## 空のレンダラーオブジェクトを削除する {#remove-empty-renderer-gameobject}

統合対象のSkinnedMeshRendererが属しているGameObjectにSkinnedMeshRenderer以外のコンポーネントが無い場合、そのGameObjectをヒエラルキーから取り除くオプションです。

## マテリアルの統合 {#merge-materials}

複数の(Skinned)MeshRendererで使用されているマテリアルがある場合、ここに一覧で表示されます。

`統合する`にチェックを入れることで、それらのマテリアルスロットを1つに統合します。これはDrawCallを削減します。
