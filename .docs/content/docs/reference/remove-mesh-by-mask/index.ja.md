---
title: Remove Mesh By Mask
weight: 25
---

# Remove Mesh By Mask

マスクテクスチャで指定した範囲のポリゴンを削除します。

このコンポーネントは、SkinnedMeshRendererコンポーネントのあるGameObjectに追加してください。(分類: [Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component))

## 利点 {#benefits}

服で隠れていたりして見えないような部分のメッシュを削除すると、見た目に影響させずに描画負荷やBlendShapeの処理負荷などを減らして軽量化することができます。

このコンポーネントを使用すると、アルファマスク用のテクスチャや、gatosyocoraさんの[MeshDeleterWithTexture]用のマスクテクスチャを利用して簡単にメッシュを削除することができます。

[MeshDeleterWithTexture]: https://github.com/gatosyocora/MeshDeleterWithTexture

## 設定 {#settings}

![component.png](component.png)

メッシュのマテリアルスロットの一覧が表示されます。
マスクテクスチャによるポリゴンの削除を行う対象のマテリアルスロットを選択してください。

### マスクテクスチャ {#mask-texture}

ポリゴンの削除に利用するマスクテクスチャです。
「編集」ボタンをクリックすると、マスクテクスチャエディターが開きます。

### 削除モード {#remove-mode}

マスクテクスチャは物によって色が異なるため、対応するモードを選択する必要があります。

黒(に近い色)の場合にポリゴンを削除するように設計されているマスクテクスチャを利用する場合は、`Remove Black`に設定してください。\
白(に近い色)の場合にポリゴンを削除するように設計されているマスクテクスチャを利用する場合は、`Remove White`に設定してください。

## マスクテクスチャエディター {#mask-texture-editor}

![mask-editor.png](mask-editor.png)

このウィンドウでマスクテクスチャを編集することができます。

描画テクスチャに関する情報が描画ウィンドウの上に表示されます。\
中央にはブラシサイズとビューのコントロールが表示されます。\
下の描画ウィンドウには、元のテクスチャ、マスクテクスチャ、メッシュのUVが表示されます。

マスクを描画するには左ドラッグ、ビューを移動するには右ドラッグまたはshift + 左ドラッグを使用します。\
ズームイン/アウトするにはスクロール、ブラシサイズを変更するにはshift + スクロールを使用します。
