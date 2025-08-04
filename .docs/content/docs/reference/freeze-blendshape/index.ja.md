---
title: Freeze BlendShape
weight: 25
---

# Freeze BlendShape

BlendShapeをメッシュに固定し、除去することが出来ます。

このコンポーネントは、SkinnedMeshRendererコンポーネントのあるGameObjectに追加してください。(分類: [Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component))

<blockquote class="book-hint info">

[Trace And Optimize](../trace-and-optimize)が自動で同様の処理を行うため、大抵の場合、このコンポーネントを使用する必要はありません。

</blockquote>

## 利点 {#benefits}

BlendShapeの固定・除去には以下の効果があります。

- BlendShapeの値が0以外のときは処理負荷が発生するため、値をアニメーション等で変更しないBlendShapeは固定すると負荷が軽くなります。
- 値が常に0である場合でも、固定することでアバターの容量を削減することができます。

## 備考 {#notes}

固定すると、アニメーションでの値変更は出来なくなります。

## 設定 {#settings}

![component.png](component.png)

BlendShapeの一覧が表示されるので、固定・除去対象のBlendShapeを選択してください。
