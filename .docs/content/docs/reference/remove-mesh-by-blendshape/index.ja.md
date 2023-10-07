---
title: Remove Mesh By BlendShape
weight: 25
---

# Remove Mesh By BlendShape

指定されたBlendShapeによって動かされる頂点とそのポリゴンを削除します。

このコンポーネントは[Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component)であるため、SkinnedMeshRendererコンポーネントのあるGameObjectに追加してください。

服で隠れていたりして見えないような部分のメッシュを削除すると、見た目に影響させずに描画負荷やBlendShapeの処理負荷などを減らして軽量化することができます。
このコンポーネントを使用すると、多くの素体に含まれている貫通防止用のBlendShapeを利用して簡単にメッシュを削除することができます。

## 設定 {#settings}

![component.png](component.png)

BlendShapeの一覧が表示されるので、BlendShapeを選択してください。
もし選択されたBlendShapeが頂点を`許容差`より大きく動かしていたら、その頂点を削除します。
