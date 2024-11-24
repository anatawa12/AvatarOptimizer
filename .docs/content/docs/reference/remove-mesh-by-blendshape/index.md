---
title: Remove Mesh By BlendShape
weight: 25
---

# Remove Mesh By BlendShape

Remove vertices transformed by specified BlendShape and their polygons.

This component should be added to a GameObject which has a SkinnedMeshRenderer component. (Kind: [Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component))

## Benefits

By removing polygons which are hidden by clothes or something, you can reduce rendering cost, BlendShape processing cost, etc. without affecting the appearance so much.

You can use this component to easily remove polygons with BlendShapes for shrinking parts of the body, which many avatars have.

## Settings

![component.png](component.png)

You'll see the list of BlendShapes and check to select blendShapes.
If some vertices in your mesh is moved more than `Tolerance` by selected BlendShape, this component will remove the vertices.

In case polygons you want to remove are not removed, increase the `Tolerance` value.
In case polygons you do not want to remove are removed, decrease the `Tolerance` value.

If you enabled `Automatically set BlendShape weight for preview when toggled`, when you toggle specifying BlendShapes, their weight will be automatically set to 100 or 0.
