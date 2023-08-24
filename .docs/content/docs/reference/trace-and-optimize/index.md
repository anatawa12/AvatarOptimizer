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

This component is [Avatar Global Component](../../component-kind/avatar-global-components), so this should be added to an avatar root.

Currently the following optimizations are applied automatically.
- [FreezeBlendShape](../freeze-blendshape)
  Automatically freezes unused BlendShapes in animation or else.
- `Remove unused Objects`
  By scanning animation, etc., Automatically removes unused Objects.

Also, You can adjust optimization with the following settings
- `MMD World Compatibility`
  Optimize with considering compatibility with MMD Worlds. e.g. Not freezing BlendShapes used by MMD Worlds.
- `Use Advanced Animator Parser`
  To determine where AAO can optimize, AAO parses AnimatorControllers.
  If this option is enabled, AAO will use Advanced Animator Parser
  which parses layer structure of AnimatorController and BlendTree structure. 
  If this option is disabled, AAO will use Fallback Animator Parser which collects all AnimationClips and
  parses AnimationClips. Fallback Animator Parser is an legacy process, so it will parse Direct BlendTree incorrectly.
  Normally, you do not need to disable this option.

![component.png](component.png)
