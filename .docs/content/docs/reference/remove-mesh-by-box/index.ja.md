---
title: Remove Mesh By Box
weight: 25
aliases: 
  - /ja/docs/reference/remove-mesh-in-box/
---

# Remove Mesh By Box

箱で指定した範囲のポリゴンを削除します。

このコンポーネントは、SkinnedMeshRendererコンポーネントかMeshRendererコンポーネントのあるGameObjectに追加してください。(種類: [MeshRenderer対応Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component))

## 利点 {#benefits}

服で隠れていたりして見えないような部分のメッシュを削除すると、見た目に影響させずに描画負荷やBlendShapeの処理負荷などを減らして軽量化することができます。

## 設定 {#settings}

### 削除するポリゴン {#remove-polygons}

箱の内側のポリゴンを削除するか、箱の外側のポリゴンを削除するかを選択することが出来ます。

### 箱 {#boxes}

ポリゴンを削除するための箱の一覧が表示されます。
`Boxes`の右側の数値を大きくすることで、箱を追加することが出来ます。

それぞれの箱について、中心位置、大きさ、角度を変更することが出来ます。(ローカル座標で指定します)

![component.png](component.png)

`Edit This Box`をクリックすると下図のようなギズモを表示します。
こちらから箱の位置、大きさ、角度を調整することも出来ます。

<img src="gizmo.png" width="563">

## 例 {#Example}

上側の図にある箱の範囲内のメッシュが、下側の図のように削除されます。

<img src="before.png" width="403">
<img src="after.png" width="403">
