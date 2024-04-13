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

You'll see the list of material slots of the mesh.
Select the material slots you want to remove polygons with mask texture.

### Mask Texture

The mask texture to remove polygons.

### Remove Mode

Since the mask textures have different colors depending on the case, you need to select the corresponding mode.

When you use the mask texture which is designed to remove polygons if the color is (close to) black, select `Remove Black` mode.\
When you use the mask texture which is designed to remove polygons if the color is (close to) white, select `Remove White` mode.
