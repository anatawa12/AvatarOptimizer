---
title: Remove Mesh By BlendShape
weight: 25
---

# Remove Mesh By BlendShape

指定されたブレンドシェイプによって動かされる頂点とそのポリゴンを削除します

このコンポーネントは[Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component)であるため、SkinnedMeshRendererコンポーネントのあるGameObjectに追加してください。

## 設定 {#settings}

![component.png](component.png)

ブレンドシェイプの一覧が表示されるので、ブレンドシェイプを選択してください。
もし選択されたブレンドシェイプが頂点を`許容差`より大きく動かしていたら、その頂点を削除します。
