---
title: Merge Bone
weight: 100
---

# Merge Bone

If you add this component to some GameObject, this GameObject will be removed and merged to parent GameObject.

If the parent GameObject also have Merge Bone component, two GameObjects are merged to their further parent GameObject.

All children of GameObject where this component is applied will belong to parent of this GameObject.

## Settings

![component.png](component.png)

- `Avoid Name Conflict` Avoids animation problems with name conflict by renaming child GameObjects
