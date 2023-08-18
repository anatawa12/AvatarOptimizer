---
title: Remove Mesh By BlendShape
weight: 25
---

# Remove Mesh By BlendShape

Remove vertices transformed by specified BlendShape and their polygons.

This component is [Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component) thus this component should be added onto GameObject with SkinnedMeshRenderer.

## Settings

![component.png](component.png)

You'll see list of blend shapes and check to select blendShapes.
If some vertices in your mesh is moved more than `Tolerance` by selected BlendShape, this component will remove the vertices. 
