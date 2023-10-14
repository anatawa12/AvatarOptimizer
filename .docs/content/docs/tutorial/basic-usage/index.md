---
title: Basic Usage
---

Basic Usage
===

Use Automatic Optimization {#trace-and-optimize}
---

There are several optimizations which can be performed automatically for Avatars.

- Removing Unused BlendShapes(Shape Keys)[^blend-shape]
  - For BlendShapes with non-zero weight, freezing BlendShapes will reduce processing cost.
  - Even if the weight is zero, removing BlendShapes will reduce the size of avatars.
- Removing unused Behaviours such as PhysBones
  - If there are enabled PhysBones which do not need to be swayed (e.g., the swaying target mesh is always disabled), extra computational cost is caused.
- Merging bones which is not animated or swayed with PhysBones
  - If the clothes bones are nested into the body bones, there will be many bones which are never moved locally. Such bones cause extra cost.

With AvatarOptimizer, these optimizations can be performed automatically by simply adding a `Trace And Optimize` to the Avatar Root!

![add-trace-and-optimize.png](add-trace-and-optimize.png)

[^blend-shape]: BlendShape is the name of Shape Keys in Unity. Unity and Maya call them as Blend Shape, Blender calls them as Shape Key, Metasequoia and MMD call them as Morph.

Merge Meshes to reduce # of Skinned Renderers {#merge-skinned-mesh}
--

You can easily merge Skinned Mesh with Avatar Optimizer!
Merging Skinned Mesh will not allow you to turn them on and off individually, but merging them will reduce rendering cost!

{{< hint info >}}

**Why will we merge Skinned Mesh?**

Merging Skinned Mesh will reduce number of deforming mesh (skinning).
Also, Merging with MergeSkinnedMesh can reduce material slots so we can reduce number of drawing. 

{{< /hint >}}

This time, I'll optimize Anon-chan as a simplest case.

![start.png](./start.png)

First, create GameObject for merged mesh[^mesh].
Right-click avatar GameObject and click `Create Empty` to create new GameObject.
Then, rename to understandable name. In this document, I call it as `Anon_Merged`.

![create-empty.png](./create-empty.png)

Then, Add `Merge Skinned Mesh` to `Anon_Merged`.

![add-merge-skinned-mesh.png](./add-merge-skinned-mesh.png)

This adds `Merge Skinned Mesh` and `Skinned Mesh Renderer` (which will be the merged mesh).

To make it easy to specifying merge target meshes to `Merge Skinned Mesh`, lock the inspector with `Anon_Merged` selected.
This allow us to drag & drop multiple meshes at once.[^tip-lock-inspector]

![lock-inspector.png](./lock-inspector.png)

Then, select meshes except for Body, which is the face mesh, and drag & drop to Skinned Renderers of `Merge Skinned Mesh`!

![drag-and-drop.png](./drag-and-drop.png)

{{< hint info >}}

**Why don't we merge face meshes?**

BlendShape (Shape Keys) is a feature became heavier in proportion to the count of vertices and BlendShapes.
Therefore, merging face mesh, which has many BlendShapes, and body mesh, which has many vertices, can make your avatar heavier than before
so I recommend not to merge face mesh.

{{< /hint >}}

Next, configure `Anon_Merged`!

Because of many reasons[^merge-skinned-mesh], `Merge Skinned Mesh` doesn't configure anything except of bones, meshes, materials, BlendShapes and bounds.
So, please configure Anchor Override, Root Bone and so on yourself on the merged mesh (`Skinned Mesh Renderer` which is added at the same time as the `Merge Skinned Mesh`).
I think specifying what is configured in your avatar's body as Anchor Override, and setting `Hips` as the Root Bone will work well.

{{< hint info >}}

### Checking performance rank without uploading avatar {#performance-rank-without-upload}

Because Avatar Optimizer is a non-destructive avatar modification tool,
Performance Rank on the VRCSDK Control Panel is no loner be relied upon.

Instead, you can check Performance Rank in Play Mode with Actual Performance Window of anatawa12's Gist Pack.
Please check [basic usages of anatawa12's Gist Pack][gists-basic-usage] and [documentation of Actual Performance Window][Actual Performance Window] for more details。

[gists-basic-usage]: https://vpm.anatawa12.com/gists/ja/docs/basic-usage/
[Actual Performance Window]: https://vpm.anatawa12.com/gists/ja/docs/reference/actual-performance-window/

{{< /hint >}}

[^tip-lock-inspector]: It is useful to keep in mind that it can be used in many other places such as specifying multiple colliders for PhysBone.
[^merge-skinned-mesh]: Root Bone and Anchor Override are impossible to merge automatically I think. If you know any good algorithm, please tel me that.
[^mesh]: In this document mesh means SkinnedMeshRenderer, not the Mesh asset in Unity.

Reduce polygons with BlendShapes which shrink parts of the body {#remove-mesh-by-blendshape}
---

By removing polygons which are hidden by clothes or something, you can reduce rendering cost, BlendShape processing cost, etc. without affecting the appearance so much.
To make this easier, AvatarOptimizer allows you to remove polygons with BlendShapes for shrinking parts of the body, which many avatars have!

Let's add `Remove Mesh By BlendShape` to Body Mesh!

Enable `Automatically set BlendShape weight for preview when toggled` to make sure that unintended parts of the body are not removed, and
select BlendShapes from the BlendShapes list below that shrink the parts of the body you want to remove!

![remove mesh by BlendShape](./remove-mesh-by-blendshape.png)
