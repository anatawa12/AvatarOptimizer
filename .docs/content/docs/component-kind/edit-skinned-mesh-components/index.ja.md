---
title: Edit Skinned Mesh Components
weight: 20
---

# Edit Skinned Mesh Components

Edit Skinned Mesh ComponentはSkinnedMeshRendererコンポーネントのあるGameObjectに追加することで、そのSkinnedMeshRendererに作用するコンポーネントです。

Avatar Optimizer 1.9.0以降では、一部のコンポーネントがメッシュレンダラー(MeshRendererコンポーネント)にも対応しています。\
これらは「MeshRenderer対応Edit Skinned Mesh Component」と呼ばれます。

このコンポーネントには2つの小分類があります。

## Source Component

メッシュなどを生成するコンポーネントです。\
以下のコンポーネントがSource Edit Skinned Mesh Componentです。

- [MergeSkinnedMesh](../../reference/merge-skinned-mesh)\
  このコンポーネントは他の(Skinned)MeshRendererを基にメッシュを生成します。

## Modifying Component

既にあるメッシュなどを(複製して)編集するコンポーネントです。\
以下のコンポーネントがModifying Edit Skinned Mesh Componentです。

- [Remove Mesh By BlendShape](../../reference/remove-mesh-by-blendshape)
- [Remove Mesh By Box](../../reference/remove-mesh-by-box)
- [Remove Mesh By UV Tile](../../remove-mesh-by-uv-tile/)
- [Freeze BlendShape](../../reference/freeze-blendshape)
- [Merge ToonLit Material](../../reference/merge-toonlit-material)
- [Rename BlendsShape](../../reference/rename-blendshape)

# MeshRenderer対応コンポーネント {#components-with-basic-mesh-support}

Avatar Optimizer 1.9.0以降では、以下のEdit Skinned Mesh Componentがメッシュレンダラーにも対応しています。

- [Remove Mesh By Box](../../reference/remove-mesh-by-box)
- [Remove Mesh By Mask](../../reference/remove-mesh-by-mask)
- [Remove Mesh By UV Tile](../../reference/remove-mesh-by-uv-tile/
