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

<blockquote class="book-hint info">

Trace and Optimize is quite carefully designed under the premise that "**never let it affect the appearance**."\
So, if any problems occur, such as appearance is affected or some gimmick stops working, they are all caused by bugs in AAO, without exception.\
Therefore, if you encounter any problems with this component, please report it.\
We will fix it as much as we can.

</blockquote>

Currently the following optimizations are applied automatically.
- `Optimize BlendShape`\
  <small>Previously known as `Freeze BlendShapes` but renamed to add more functionality.</small>\
  By scanning animation etc., remove, freeze, or merge BlendShapes automatically to reduce the number of BlendShapes.
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
- `Optimize Texture`\
  Optimizes textures without affecting the appearance.\
  Currently, UV Packing and reducing texture size is performed only for materials with supported shaders.

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

[^endbone]: Trace And Optimize currently treats any bones whose name ends with `end` (ignoring case) as end bones.

## Animator Optimizer {#animator-optimizer}

This feature currently applies the following optimizations.

(Details of optimization can be changed in the future.)

- Convert AnyState to Entry-Exit\
  This tries to convert Animator Controller layers of AnyState type to Diamond-style Entry-Exit type as possible.
  With other optimizations, AnyState type layers may be converted to BlendTree.

  ```mermaid
  ---
  title: AnyState type layer
  ---
  graph LR;
        AnyState(AnyState);
        Entry(Entry) --> State1;
        AnyState --> State1(State1);
        AnyState --> State2(State2);
        AnyState --> State3(State3);
  
  classDef default fill:#ab8211
  classDef node stroke-width:0px,color:#ffffff
  classDef state fill:#878787
  style AnyState fill:#29a0cc
  style Entry fill:#15910f
  class Entry,State1,State2,Exit node
  class State1 default
  class State2,State3 state
  ```

- Convert Complete Graph to Entry-Exit\
  This tries to convert Animator Controller layers with complete graph structure to Diamond-style Entry-Exit type as possible.
  With other optimizations, such layers may be converted to BlendTree.

  ```mermaid
  ---
  title: Complete Graph layer
  ---
  graph LR;
        Entry(Entry) --> State1;
        State1(State1) --> State2(State2);
        State1 --> State3(State3);
        State2 --> State1;
        State2 --> State3;
        State3 --> State1;
        State3 --> State2;
  
  classDef default fill:#ab8211
  classDef node stroke-width:0px,color:#ffffff
  classDef state fill:#878787
  %%style AnyState fill:#29a0cc
  style Entry fill:#15910f
  class Entry,State1,State2,Exit node
  class State1 default
  class State2,State3 state
  ```

- Convert Entry-Exit to BlendTree\
  This tries to convert Animator Controller layers of Entry-Exit type to BlendTree as possible.\
  Currently, this is applied to Diamond-style and Linear-style Entry-Exit layers.

  ```mermaid
  ---
  title: Diamond-style Entry-Exit type layer
  ---
  graph LR;
        Entry(Entry);
        Entry --> State1(State1);
        Entry --> State2(State2);
        Entry --> State3(State3);
        State1 --> Exit(Exit);
        State2 --> Exit;
        State3 --> Exit;
  
  classDef default fill:#ab8211
  classDef node stroke-width:0px,color:#ffffff
  style Exit fill:#ba202f
  style Entry fill:#15910f
  classDef state fill:#878787
  class Entry,State1,State2,Exit node
  class State1 default
  class State2,State3 state
  ```

  ```mermaid
  ---
  title: Linear-style Entry-Exit type layer
  ---
  flowchart LR;
        Entry(Entry) --> State1(State1);
        State1 --> State2(State2);
        State2 --> Exit(Exit);

  classDef node stroke-width:0px,color:#ffffff
  style Exit fill:#ba202f
  style Entry fill:#15910f
  classDef default fill:#ab8211
  classDef state fill:#878787
  class Entry,State1,State2,Exit node
  class State1 default
  class State2 state
  ```

- Merge BlendTree Layers\
  This merges multiple BlendTree layers to single Direct BlendTree layer as possible.
- Remove Meaningless Layers\
  This removes layers which have no state nor a transition.
