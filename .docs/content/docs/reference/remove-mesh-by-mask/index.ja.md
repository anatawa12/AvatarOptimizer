---
title: Remove Mesh By Mask
weight: 25
---

# Remove Mesh By Mask

指定されたマスクテクスチャーによってポリゴンを削除します。

このコンポーネントは、SkinnedMeshRendererコンポーネントのあるGameObjectに追加してください。(分類: [Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component))

## 利点 {#benefits}

服で隠れていたりして見えないような部分のメッシュを削除すると、見た目に影響させずに描画負荷やBlendShapeの処理負荷などを減らして軽量化することができます。
このコンポーネントを使用すると、アルファマスク用のテクスチャや、[gatosyocoraさんのMeshDeleterWithTexture]用のマスクテクスチャーを簡単に利用することができます。

[gatosyocoraさんのMeshDeleterWithTexture]: https://github.com/gatosyocora/MeshDeleterWithTexture

## 設定 {#settings}

![component.png](component.png)

メッシュのマテリアルスロットの一覧が表示されます。
マスクテクスチャーを使用してポリゴンを削除したいマテリアルスロットを選択してください。

マスクテクスチャーは`マスクテクスチャー`に設定してください。

マスクテクスチャーが、黒(に近い色)の場合にポリゴンを削除するように設計されている場合は、`削除モード`を`Remove Black`に設定してください。\
マスクテクスチャーが、白(に近い色)の場合にポリゴンを削除するように設計されている場合は、`削除モード`を`Remove White`に設定してください。
