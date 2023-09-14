---
title: Merge Bone
weight: 100
---

# Merge Bone

このコンポーネントがGameObjectに付いている場合、このGameObjectに対応するボーンは親ボーンに統合され、GameObjectが取り除かれます。

もし親のGameObjectにもMerge Boneコンポーネントが付いていた場合、2つのボーンはその更に親のボーンに統合されます。

このコンポーネントが付いているGameObjectの全ての子GameObjectは、その親GameObjectの子になります。

## 設定 {#settings}

![component.png](component.png)

- `名前の競合を避ける` 統合時に子GameObjectの名前を変更することで、名前の重複によってアニメーションが正しく動かなくなる問題を回避します。
