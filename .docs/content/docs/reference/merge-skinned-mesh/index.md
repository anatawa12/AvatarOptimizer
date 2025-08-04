---
title: Merge Skinned Mesh
weight: 21
---

# Merge Skinned Mesh (MergeSMR) {#merge-skinned-mesh}

Merges one or more SkinnedMeshRenderers and MeshRenderers into one SkinnedMeshRenderer.

This component should be added to a new GameObject which has a SkinnedMeshRenderer component without Mesh specified. (Kind: [Source Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#source-component))

<blockquote class="book-hint info">

[Trace And Optimize](../trace-and-optimize) will automatically do the same process, so in most cases you do not need to use this component.

</blockquote>

## Benefits

Merging SkinnedMeshRenderer will reduce number of deforming mesh (skinning).
Also, it can reduce material slots using the same material, so we can reduce rendering cost.

## Notes

This component makes it impossible to turn meshes on and off individually on animations, but material-related animations will work without modification.

This component will configure Meshes, Materials, BlendShapes, and Bounds but other settings will not be modified.
Please edit SkinnedMeshRenderer component attached to same GameObject as MergeSkinnedMesh to set Anchor Override or else.

<blockquote class="book-hint info">

If you are using [Modular Avatar], you can add [`MA Mesh Settings`] component to the root of the avatar to set the Anchor Override or else for the whole avatar.

</blockquote>

It's better to freeze & remove unchanging BlendShapes for body / cloth meshes to reduce BlendShape load.\
You can freeze & remove BlendShape using [Freeze BlendShape](../freeze-blendshape) component.
Add this component to both/either merge source SkinnedMeshRenderer and/or merged SkinnedMeshRenderer to freeze & remove BlendShapes.
Also, you can use `Optimize BlendShapes` of [Trace and Optimize](../trace-and-optimize) component to get the same benefits.

In previous versions of Avatar Optimizer, we recommended not merging face meshes due to merging BlendShape-heavy mesh will increase load on BlendShape much in Unity 2019.\
However, in Unity 2022, we no longer recommends not merging face meshes because the BlendShape load has been improved.

## Settings

![component.png](component.png)

### Skinned Renderers

The list of SkinnedMeshRenderers to be merged.

Drop to None element at the bottom to add renderer and reset to None to remove from the list.

### Static Renderers

The list of MeshRenderers (without mesh transform).

Those meshes are transformed to polygons weighted to one bone, the GameObject that MeshRenderer belongs to.

Drop to None element at the bottom to add renderer and reset to None to remove from the list.

### Remove Empty Renderer GameObject

If this checkbox is checked and the GameObject where SkinnedMeshRenderer belongs does not have
any other components than SkinnedMeshRenderer, the GameObject will be removed from Hierarchy.

### Skip Enablement Mismatched Renderers

If this checkbox is checked, renderers whose enablement is different than target renderer on the build time will not be merged.

### Copy Enablement Animation

If this checkbox is checked, the activeness / enablement animation of merge target renderers will be copied to the merged renderer.

This feature may copy animation animating `enabled` of the merge target renderers or `activeSelf` of the GameObjects or ancestor GameObjects.
This feature supports copying only one animated property so if there are multiple animated properties (e.g., both `enabled` and `activeSelf` are animated, or both one `activeSelf` and parents' `activeSelf` are), it will be error.

In addition, if this is enabled, you must not animate `enabled` of the merged renderer since it will be overwritten by the copied animation.

### BlendShape Mode

The mode of BlendShape handling.

- `Rename BlendShape to avoid conflict`: Renames BlendShapes to avoid conflicts. This is the default behavior.
- `Merge BlendShapes with same name`: Merges BlendShapes with the same name. This is useful when you want to merge BlendShapes of the same name from different SkinnedMeshRenderers.
- `v1.7.x or older compability mode`: This mode is for compatibility with v1.7.x or older versions. It will merge BlendShapes, but this merge is not considered by Trace and Optimize. This mode is not available for new components.


### Merge Materials

If this component found some Materials used in multiple renderers, the Materials will be listed here.

Check `Merge` to merge those MaterialSlots (SubMeshes) into one MaterialSlot. This reduces DrawCalls.

[Modular Avatar]: https://modular-avatar.nadena.dev
[`MA Mesh Settings`]: https://modular-avatar.nadena.dev/docs/reference/mesh-settings
