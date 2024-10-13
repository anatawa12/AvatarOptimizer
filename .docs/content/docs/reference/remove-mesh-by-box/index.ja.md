---
title: Remove Mesh By Box
weight: 25
aliases: 
  - /ja/docs/reference/remove-mesh-in-box/
---

# Remove Mesh By Box

箱で指定した範囲内のポリゴンを削除します。

このコンポーネントは、SkinnedMeshRendererコンポーネントのあるGameObjectに追加してください。(分類: [Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component))

## 利点 {#benefits}

服で隠れていたりして見えないような部分のメッシュを削除すると、見た目に影響させずに描画負荷やBlendShapeの処理負荷などを減らして軽量化することができます。

## 設定 {#settings}

数値を調整して箱を追加します。
それぞれの箱について、中心位置、大きさ、角度を変更することが出来ます。(ローカル座標で指定します)

![component.png](component.png)

`Edit This Box`をクリックして下図のようなギズモを表示します。箱の大きさ、位置、角度を調整することが出来ます。

<img src="gizmo.png" width="563">

## 例 {#Example}

上側の図にある箱の範囲内のメッシュが、下側の図のように削除されます。

<img src="before.png" width="403">
<img src="after.png" width="403">
