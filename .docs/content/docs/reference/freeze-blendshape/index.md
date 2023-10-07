---
title: Freeze BlendShape
weight: 25
---

# Freeze BlendShape

Freeze & remove BlendShape from the mesh.

This component should be added to a GameObject which has a SkinnedMeshRenderer component. (Kind: [Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component))

## Benefits {#benefits}

Freezing & removing BlendShapes has the following benefits.

- For BlendShapes with non-zero weight, freezing BlendShapes will reduce processing cost.
- Even if the weight is zero, removing BlendShapes will reduce the size of avatars.

## Notes {#notes}

By freezing BlendShape, the weights cannot be changed on Animation.

## Settings

![component.png](component.png)

You'll see list of blend shapes and check to freeze blend shape.
