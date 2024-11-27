---
title: Remove Mesh By Mask
weight: 25
---

# Remove Mesh By Mask

Remove some polygons in any area specified by mask textures.

This component should be added to a GameObject which has a SkinnedMeshRenderer component. (Kind: [Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component))

## Benefits

By removing polygons which are hidden by clothes or something, you can reduce rendering cost, BlendShape processing cost, etc. without affecting the appearance so much.

You can use this component to easily remove polygons with alpha mask texture or mask texture for [MeshDeleterWithTexture] by gatosyocora.

[MeshDeleterWithTexture]: https://github.com/gatosyocora/MeshDeleterWithTexture

## Settings

![component.png](component.png)

You'll see the list of material slots of the mesh.\
Select the material slots you want to remove polygons with mask texture.

### Mask Texture

The mask texture to remove polygons.\
If nothing is set, click "Create" button to create a new texture.

Click "Edit" button to open the Mask Texture Editor.

### Remove Mode

Since the mask textures have different colors depending on the case, you need to select the corresponding mode.

When you use the mask texture which is designed to remove polygons if the color is (close to) black, select `Remove Black` mode.\
When you use the mask texture which is designed to remove polygons if the color is (close to) white, select `Remove White` mode.

## Mask Texture Editor

![mask-editor.png](mask-editor.png)

With this window, you can edit the mask texture.

At the top of the window, information related to the mask texture being edited is shown.\
At the center, there are controls for view and brush size.\
The bottom of the window shows the original texture, mask texture and UVs of the mesh.

You can left-drag to paint the mask, and right-drag or shift + left-drag to move the view.\
You can scroll to zoom in/out, and shift + scroll to change the brush size.
