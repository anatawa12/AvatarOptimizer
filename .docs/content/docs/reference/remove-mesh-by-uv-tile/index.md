---
title: Remove Mesh By UV Tile
weight: 25
---

# Remove Mesh By UV Tile

Remove some polygons in any area specified by UV Tile.

This component should be added to a GameObject which has a Skinned Mesh Renderer or Mesh Renderer component. (Kind: [Modifying Edit Skinned Mesh Component with basic mesh support](../../component-kind/edit-skinned-mesh-components#modifying-component))

## Benefits

By removing polygons which are hidden by clothes or something, you can reduce rendering cost, BlendShape processing cost, etc. without affecting the appearance so much.

You can use this component to easily remove polygons of models designed to hide some portion with UV Tile Discard feature of Poiyomi or lilToon.\
This component works like UV Tile Discard with Vertex Mode.

Please read [documentation of Poiyomi's UV Tile Discard][UV Tile Discard] for more details about UV Tile Discard.

You also may use some other non-destructive tool to create UV Tiling for the model.

[UV Tile Discard]: https://www.poiyomi.com/special-fx/uv-tile-discard

## Settings

![component.png](component.png)

You'll see the list of material slots of the mesh.

You can select the UV Tile to remove polygons by clicking the tile.
Checked tiles will be removed.

You may also select UV Channel to use for UV Tile selection above.
Unlike UV Tile Discard of Poiyomi or lilToon, this component can use any UV Channel for UV Tile selection.
