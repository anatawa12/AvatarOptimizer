---
title: Trace And Optimize
weight: 11
aliases:
  - /en/docs/reference/automatic-configuration/
---

# Trace And Optimize (T&O) {#trace-and-optimize}

<i>Previously known as Automatic Configuration</i>

This component will trace your avatar and optimize your avatar automatically.
You can enable/disable some automatic optimization features with checkboxes.

This component should be added to an avatar root. (Kind: [Avatar Global Component](../../component-kind/avatar-global-components))

{{< hint info >}}

Trace and Optimize is quite carefully designed under the premise that "**never let it affect the appearance**."\
So, if any problems occur, such as appearance is affected or some gimmick stops working, they are all caused by bugs in AAO, without exception.\
Therefore, if you encounter any problems with this component, please report it.\
We will fix it as much as we can.

{{< /hint >}}

Currently the following optimizations are applied automatically.
- `Optimize BlendShape`\
  <small>Previously known as `Freeze BlendShapes` but renamed to add more functionality.</small>\
  Optimizes BlendShapes and remove if BlendShapes are not used or unnecessary.
- `Remove unused Objects`\
  By scanning animation etc., automatically removes unused Objects (e.g. GameObjects, Components).\
  In addition, this will automatically toggle PhysBone Components if they are only used by toggled objects.
  - `Preserve EndBone`\
    Prevents removing end bones[^endbone] whose parent is not removed.
- `Optimize PhysBone Settings`\
  Optimizes PhysBone settings for better performance. This performs the following optimizations.
  - Merges PhysBone Colliders with the exactly same settings into one PhysBone Collider.
  - Unchecks `Is Animated` if it's not necessary.
- `Optimize Animator`\
  Optimizes Animator Controller. See [this section](#animator-optimizer) for more details.
- `Merge Skinned Meshes`\
  Merges skinned meshes which don't need to be separated.\
  Some meshes may not be automatically merged in some cases, so use [Merge Skinned Mesh](../merge-skinned-mesh) manually if necessary.
  - `Allow Shuffling Material Slots`\
    By shuffling material slots, you may reduce draw calls of the avatar.
    The order of material slots usually doesn't matter, but it may affect the drawing order in rare cases.

Also, You can adjust optimization with the following settings
- `MMD World Compatibility`\
  Optimize with considering compatibility with MMD Worlds. e.g. Not freezing BlendShapes used by MMD Worlds.

In addition, there are the following Advanced Optimizations.

- `Automatically Remove Zero Sized Polygons`\
  Removes polygons whose area are zero.
  This can break some shaders or animated scales, so use it carefully.

Also, there is `Debug Options` which is for workaround bugs but it's unstable & not well-tested.
See tooltips or implementation for more details.

![component.png](component.png)

[^endbone]: AAO currently assumes any bones whose name ends with `end` (ignoring case) are end bones.

## Animator Optimizer {#animator-optimizer}

This feature currently applies the following optimizations.

- Convert AnyState to Entry-Exit\
  This tries to convert Animator Controller layers of AnyState type to of Entry-Exit type as possible.
  With other optimizations, AnyState type layers may be converted to BlendTree.
- Convert Entry-Exit to BlendTree\
  This tries to convert Animator Controller layers of Entry-Exit type to BlendTree as possible.
- Merge BlendTree Layers\
  This merges multiple BlendTree layers to single Direct BlendTree layer as possible.
- Remove Meaningless Layers\
  This removes layers which have no state nor a transition.
