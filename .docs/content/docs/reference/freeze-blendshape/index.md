---
title: Freeze BlendShape
weight: 25
---

# Freeze BlendShape

Freeze & remove BlendShape from the mesh.

This component should be added to a GameObject which has a SkinnedMeshRenderer component. (Kind: [Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component))

<blockquote class="book-hint info">

[Trace And Optimize](../trace-and-optimize) will automatically do the same process, so in most cases you do not need to use this component.

</blockquote>


## Benefits

Freezing & removing BlendShapes has the following benefits.

- For BlendShapes with non-zero weight, freezing BlendShapes will reduce processing cost.
- Even if the weight is zero, removing BlendShapes will reduce the size of avatars.

## Notes

By freezing BlendShape, the weights cannot be changed on Animation.

## Settings

![component.png](component.png)

You'll see the list of BlendShapes and check to freeze BlendShape.
