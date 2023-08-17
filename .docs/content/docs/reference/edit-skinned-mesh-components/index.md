---
title: Edit Skinned Mesh Components
weight: 20
---

# Edit Skinned Mesh Components

Edit Skinned Mesh Component is a kind of component which should be added to 
GameObject with SkinnedMeshRender and applies some modification to the SkinnedMeshRenderer.

There are two sub-kind for this kind.

## Source Component

This kind of component will produce Mesh and some other properties of SkinnedMeshRenderer.
Following components are Source Edit Skinned Mesh Component.

- [MergeSkinnedMesh](../merge-skinned-mesh)
  
  This component will produce mesh from other (Skinned)MeshRenderers.

## Modifying Component

This kind of component will (duplicates and) modifies the existing Mesh and some other properties of SkinnedMeshRenderer.
Following components are Modifying Edit Skinned Mesh Component.

- [Remove Mesh By BlendShape](../remove-mesh-by-blendshape)
- [Remove Mesh in Box](../remove-mesh-by-blendshape)
- [Freeze BlendShape](../freeze-blendshape)
- [Merge ToonLit Material](../merge-toonlit-material)
