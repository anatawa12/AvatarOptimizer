---
title: Merge Skinned Mesh
weight: 1
---

# Merge Skinned Mesh

Merges one or more SkinnedMeshRenderer and MeshRenderers into one SkinnedMeshRenderer.

You should add MergeSkinnedMesh onto new GameObject with SkinnedMeshRenderer without specified Mesh.

This component will merge mesh, bones, and BlendShapes but other settings will not be modified.
Please edit SkinnedMeshRenderer component attached to same GameObject as MergeSkinnedMesh to set AnchorOverride or else.

BlendShapes is a heavy feature proportion to the count of vertices.
Merging SkinedMesh increases vertices so It's better to freeze & remove static BlendShape before or after merging SkinnedMesh.
There's component for this operation [Freeze BlendShape](../freeze-blendshape). You can add Freeze BlendShape to either / both merge source SkinnedMeshRenderer or / and merged SkinnedMeshRenderer.
If you have variable blend shapes, you should not merge too many SkinnedMeshes.
For example, if you have separated mesh between body and face, you should not merge body and face Meshes because blendshape of face will be used to change facial expression but body mesh not.
This component is good for Merging your clothes and body.

## Settings

![component.png](component.png)

### Skinned Renderers

The list of SkinnedMeshRenderers to be merged.

Drop to None element at the bottom to add renderer and reset to None to remove from the list.

## Static Renderers

The list of MeshRenderers (without mesh transform).

Those meshes are transformed to polygons weighted to one bone, the GameObject that MeshRenderer belongs to.

Drop to None element at the bottom to add renderer and reset to None to remove from the list.

## Remove Empty Renderer GameObject

If this checkbox is checked and the GameObject SkinnedMeshRenderer belongs to does not have
any other components than SkinnedMeshRenderer, the GameObject will be removed from Hierarchy.

## Merge Materials

If MergeSkinnedMesh component found some Materials used in multiple renderers, the Materials will be listed here.

Check `Merge` to merge those MaterialSlots (SubMeshes) into one MaterialSlot. This reduces DrawCalls.
