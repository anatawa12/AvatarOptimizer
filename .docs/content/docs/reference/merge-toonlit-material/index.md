---
title: Merge ToonLit Material
weight: 25
---

# Merge ToonLit Material

Merge `VRChat/Mobile/Toon Lit` materials to one material by packing texture manually.

This component is [Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component) thus this component should be added onto GameObject with SkinnedMeshRenderer.

This component currently only supports `VRChat/Mobile/Toon Lit` because I believe there are big demands
but I may add support for other shaders. (also for third-party shaders)
If you want other shader support, please write a [issue][issue]

## Settings

Click `Add Merged Material` to add merged material.
For each merged material, you can set multiple source materials from materials.
Click `Add Source` or select your material from dropdown menu.
For each source material, you must set where to the texture will placed to.
Please change X, Y, W, H to fit to where you want.
Click `Generate Preview` to generate each texture and see preview.

![component.png](component.png)

[issue]: https://github.com/anatawa12/AvatarOptimizer/issues/new/choose
