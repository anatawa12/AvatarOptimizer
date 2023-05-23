---
title: Merge Skinned Mesh
weight: 1
---

# Merge Skinned Mesh

1つ以上のSkinnedMeshRendererやMeshRendererを1つのSkinnedMeshRendererに統合(マージ)することが出来ます。

MergeSkinnedMeshは、メッシュを指定していないSkinnedMeshRendererを持つ新しいGameObjectに追加するべきです。

このコンポーネントはメッシュ・ボーン・ブレンドシェイプを統合しますが、その他の設定については変更しないため、AnchorOverride等の設定を行うには、MergeSkinnedMeshのあるGameObject上のSkinnedMeshRendererを編集してください。

このコンポーネントは、服のメッシュや体のメッシュのマージには適していますが、顔のメッシュにはブレンドシェイプが原因となって重くなるため、あまり適していません。
ブレンドシェイプは、頂点とブレンドシェイプの数に比例して負荷が大きくなります。
SkinnedMeshの統合は頂点数を増加させ、また顔のメッシュは一般的に多くのブレンドシェイプを持っています。
そのため、顔のメッシュは統合するべきではありません。

また、同じ理由で、体/服のメッシュのブレンドシェイプを固定・除去することを推奨します。
ブレンドシェイプの固定・除去には、[Freeze BlendShape](../freeze-blendshape)コンポーネントを使用できます。
このコンポーネントを統合対象・統合先のSkinnedMeshRendererのいずれか(または両方)に追加すると、ブレンドシェイプを固定・除去できます。

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
