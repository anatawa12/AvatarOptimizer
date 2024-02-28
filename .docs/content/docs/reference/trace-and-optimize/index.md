---
title: Trace And Optimize
weight: 11
aliases:
  - /en/docs/reference/automatic-configuration/
---

# Trace And Optimize

<i>Previously known as Automatic Configuration</i>

This component will trace your avatar and optimize your avatar automatically.
You can enable/disable some automatic optimization features with checkboxes.

This component should be added to an avatar root. (Kind: [Avatar Global Component](../../component-kind/avatar-global-components))

Currently the following optimizations are applied automatically.
- `Automatically Freeze Blend Shape`  
  Automatically freezes unused BlendShapes in animation or else.
- `Remove unused Objects`  
  By scanning animation etc., automatically removes unused Objects (e.g. GameObjects, Components).
  - `Preserve EndBone`  
    Prevents removing end bones[^endbone] whose parent is not removed.
- `Automatically Remove Zero Sized Polygons`\
  Removes polygons whose area are zero.
- `Optimize PhysBone Settings`\
  Optimizes PhysBone settings for better performance. This performs the following optimizations.
  - Merges PhysBone Colliders with the exactly same settings into one PhysBone Collider.
  - Unchecks `Is Animated` if it's not necessary.

Also, You can adjust optimization with the following settings
- `MMD World Compatibility`  
  Optimize with considering compatibility with MMD Worlds. e.g. Not freezing BlendShapes used by MMD Worlds.

In addition, there is `Advanced Settings` which is for workaround bugs but it's unstable & not well-tested.
See tooltips or implementation for more details.

![component.png](component.png)

[^endbone]: AAO currently assumes any bones whose name ends with `end` (ignoring case) are end bones.
