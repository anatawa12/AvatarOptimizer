---
title: Basic Usage
---

Basic Usage
===

Use Automatic Optimization {#trace-and-optimize}
---

There are several optimization which can perform automatically. for Avatars

- Removing Unused BlendShapes(Shape Keys)[^blend-shape]
  - For BlendShapes with non-zero weight, there are processing load so freezing BlendShape will reduce processing load.
  - Even if the weight is zero, removing the BlendShapes will reduce size of avatars.
- Removing unused Behaviours such as PhysBones
  - If a PhysBone that does not need to be swayed is enabled, for example a PhysBone where the mesh that exists as the target of the shaking is always disabled, an extra computational load is incurred.
- Merging bones being animated nor swayed with PhysBones
  - When clothing bones are nested inside the bones of the body, there will be many bones that do not move on their own. Such bones create extra load.

With Avatar Optimizer, You can those optimization with adding `Trace And Optimize` to the Avatar Root!

![add-trace-and-optimize.png](add-trace-and-optimize.png)

[^blend-shape]: BlendShapeはUnity上のシェイプキーの名前です。UnityやMayaではBlendShape、BlenderではShape Key、MetasequoiaやMMDではモーフと呼ばれます。

Merge Meshes to reduce # of Skinned Renderers {#merge-skinned-mesh}
--

You can easily merge Skinned Mesh with Avatar Optimizer!
Merging Skinned Mesh will not allow you to turn them on and off individually, but combining them will save some rendering weight!

{{< hint info >}}

**Why will we merge Skinned Mesh?**

Merging Skinned Mesh will reduce number of deforming mesh (skinning).
Also, Merging with MergeSkinnedMesh can reduce material slots so we can reduce number of drawing. 

{{< /hint >}}

This time, I'll optimize Anon-chan as a simplest case.

![start.png](./start.png)

First, create GameObject for merged mesh.
Right-click avatar GameObject and click `Create Empty` to create new GameObject.
Then, rename to understandable name. In this document, I call it as `Anon_Merged`.

![create-empty.png](./create-empty.png)

Then, Add `Merge Skinned Mesh` to `Anon_Merged`.

![add-merge-skinned-mesh.png](./add-merge-skinned-mesh.png)

This adds `Merge Skinned Mesh` and `Skinned Mesh Renderer`.

The `Merge Skinned Mesh` will merge specified meshes[^mesh] into the attached mesh. 
To make merge working, let's specify meshes to be merged onto `Merge Skinned Mesh`!

To make it easy to specifying meshes, lock the inspector with `Anon_Merged` selected. 
This allow us to drag & drop multiple meshes at once.[^tip-lock-inspector]

![lock-inspector.png](./lock-inspector.png)

Then, select meshes except for Body, which is the face mesh, and drag & drop to Skinned Renderers!

![drag-and-drop.png](./drag-and-drop.png)

{{< hint info >}}

**Why don't we merge face meshes?**

BlendShape (Shape Keys) is a feature became heavier in proportion to the count of vertices and BlendShapes.
Therefore, merging face mesh, which has many BlendShapes, and body mesh, which has many vertices, can make your avatar heavier than before
so I recommend not to merge face mesh.

{{< /hint >}}

Next, configure `Anon_Merged`!

Because of many reasons[^merge-skinned-mesh], `Merge Skinned Mesh` doesn't configure anything except of bones, meshes, materials, BlendShapes and bounds.
So, please configure Root Bone, Anchor Override and else yourself.
I think specifying Anchor Override of your body and setting Hips as the Root Bone will work well.

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

Reduce polygons with shrinking which shrinks parts of body
---

By deleting polygons that are hidden by clothing or otherwise, you can reduce rendering load, BlendShape processing load, etc., without affecting the appearance much.
To easily achieve this, AvatarOptimizer can remove meshes using the BlendShapes which shrinks parts of body included in many avatars!

Let's add `Remove Mesh By BlendShape` to Body Mesh!

Enable `Automatically set BlendShape weight for preview when toggled` to make sure that unexpected parts of the body are not removed,
Select the BlendShapes which shrinks parts of body which you want to remove from the list of BlendShapes below!

TODO: 写真
