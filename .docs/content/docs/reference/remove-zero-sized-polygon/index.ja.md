---
title: Remove Zero Sized Polygon
weight: 100
---

# Remove Zero Sized Polygon

面積がゼロのポリゴンを削除します。

このコンポーネントは、SkinnedMeshRendererコンポーネントのあるGameObjectに追加してください。

{{< hint warning >}}

このコンポーネントはビルドの最後の方で実行されるため、[Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component) では**ありません**。

このコンポーネントを[Merge Skinned Mesh](../merge-skinned-mesh)の統合対象となるSkinnedMeshRendererに追加しても効果がありません。

{{< /hint >}}

## 利点 {#benefits}

面積がゼロのポリゴンを削除することで、描画負荷を減らすことができます。
見た目に影響を与えることはほとんどありません。

## 備考 {#notes}

シェーダーによってはモデルファイルでのポリゴンの大きさが0でも実際にはなにかが描画されることがあるため、見た目に影響がある可能性があります。

## 設定 {#settings}

今のところ、このコンポーネントに設定項目はありません。

![component.png](component.png)

## 備考 {#notes}

このコンポーネントは[Trace and Optimize](../trace-and-optimize)コンポーネントによって自動的に追加されます。
このコンポーネントを手動で追加するよりも、Trace and Optimizeを使うことをお勧めします。
