---
title: Replace End Bone With Endpoint Position
weight: 100
---

# Replace End Bone With Endpoint Position

This component replaces the End Bone, which is the tip bone in a PhysBone, with an Endpoint Position.

Add this component to the GameObject that has the PhysBone component.

If multiple PhysBone components are attached to the GameObject with this component, the settings will be applied to all of them.

## Benefits {#benefits}

By replacing the End Bone with an Endpoint Position, you can reduce the number of `PhysBone Affected Transforms` counted by the VRChat Performance Rank system.

## Settings {#settings}

![component.png](component.png)

### Endpoint Position Mode {#endpoint-position-mode}

Select how the value for the Endpoint Position is determined.

- `Average`\
  For each applicable PhysBone, calculates the average of the local positions of its End Bones and uses that value as the Endpoint Position.

- `Manual`\
  Uses the value specified in `Value for Endpoint Position` as the Endpoint Position.

### Endpoint Position Override {#endpoint-position-override}

This option is available only when `Endpoint Position Mode` is set to `Manual`.

Here, you can directly enter the local position that will be used as the Endpoint Position.