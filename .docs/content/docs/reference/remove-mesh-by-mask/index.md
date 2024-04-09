---
title: Remove Mesh By Mask
weight: 25
---

# Remove Mesh By Mask

Remove polygons by specified mask texture.

This component should be added to a GameObject which has a SkinnedMeshRenderer component. (Kind: [Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component))

## Benefits

By removing polygons which are hidden by clothes or something, you can reduce rendering cost, BlendShape processing cost, etc. without affecting the appearance so much.
You can use this component to easily remove polygons with alpha mask texture or mask texture for [MeshDeleterWithTexture by gatosyocora].

[MeshDeleterWithTexture by gatosyocora]: https://github.com/gatosyocora/MeshDeleterWithTexture

## Settings

![component.png](component.png)

You'll see the list of material slots of the mesh.
Select the material slots you want to remove polygons with mask texture.

The mask texture should be set in the `Mask Texture`.

If the mask texture is designed to remove polygons if the color is (close to) black, set Remove Mode to `Remove Black`. \
If the mask texture is designed to remove polygons if the color is (close to) white, set Remove Mode to `Remove White`.
