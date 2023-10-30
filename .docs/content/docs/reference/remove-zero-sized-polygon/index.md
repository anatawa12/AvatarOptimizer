---
title: Remove Zero Sized Polygon
weight: 100
---

# Remove Zero Sized Polygon

Remove polygons with area of zero. 

This component should be added to a GameObject which has a SkinnedMeshRenderer component.

{{< hint warning >}}

Since this component works very late in the build process, this component is **NOT** [Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component).

Putting this component on the source SkinnedMeshRenderers of [Merge Skinned Mesh](../merge-skinned-mesh) has no effect.

{{< /hint >}}

## Benefits

By removing polygons which has zero size, you can reduce rendering cost.
Polygons with zero size will have almost zero effect on the appearance.

## Settings

Currently no settings.

![component.png](component.png)

## Notes

This component will be attached by [Trace and Optimize](../trace-and-optimize) component.
I recommend you to use Trace and Optimize instead of attaching this component manually.
