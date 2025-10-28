---
title: Edit Skinned Mesh Components
weight: 20
---

# Edit Skinned Mesh Components

Edit Skinned Mesh Component is a kind of component which should be added to
a GameObject which has a SkinnedMeshRenderer component, and applies some modification to the SkinnedMeshRenderer.

Since Avatar Optimizer 1.9.0, some components can be used with basic MeshRenderer component.
Those will be called "Edit Skinned Mesh component with basic mesh support".

There are two sub-kinds.

## Source Component

This kind of component will produce Mesh and some other properties of SkinnedMeshRenderer.\
Following components are Source Edit Skinned Mesh Component.

- [MergeSkinnedMesh](../../reference/merge-skinned-mesh)\
  This component will produce mesh from other (Skinned)MeshRenderers.

## Modifying Component

This kind of component will (duplicates and) modifies the existing Mesh and some other properties of SkinnedMeshRenderer.\
Following components are Modifying Edit Skinned Mesh Component.

- [Remove Mesh By BlendShape](../../reference/remove-mesh-by-blendshape)
- [Remove Mesh By Box](../../reference/remove-mesh-by-box)
- [Remove Mesh By UV Tile](../../remove-mesh-by-uv-tile/)
- [Freeze BlendShape](../../reference/freeze-blendshape)
- [Merge ToonLit Material](../../reference/merge-toonlit-material)
- [Rename BlendsShape](../../reference/rename-blendshape)

# Components with Basic Mesh Support

Since Avatar Optimizer 1.9.0, the following Edit Skinned Mesh Components can be used with basic MeshRenderer component as well.

- [Remove Mesh By Box](../../reference/remove-mesh-by-box)
- [Remove Mesh By Mask](../../reference/remove-mesh-by-mask)
- [Remove Mesh By UV Tile](../../reference/remove-mesh-by-uv-tile/)
