---
title: Remove Zero Sized Polygon
weight: 100
---

# Remove Zero Sized Polygon

面積が0なポリゴンを削除します。

このコンポーネントは、SkinnedMeshRendererコンポーネントのあるGameObjectに追加してください。

{{< hint warning >}}

このコンポーネントはビルドプロセスの最後の方に実行されるため、このコンポーネントは[Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component) では**ありません**。

このコンポーネントを[Merge Skinned Mesh](../merge-skinned-mesh)のソースSkinnedMeshRendererに追加しても効果はありません。

{{< /hint >}}

## 利点 {#benefits}

面積が0なポリゴンを削除することで、描画負荷を減らすことができます。
面積が0なポリゴンは見た目にほとんど影響を与えません。

## 設定 {#settings}

現在のところ設定はありません。

![component.png](component.png)

## 備考 {#notes}

このコンポーネントは[Trace and Optimize](../trace-and-optimize)コンポーネントによって自動的に追加されます。
このコンポーネントを手動で追加するよりも、Trace and Optimizeを使うことをお勧めします。
