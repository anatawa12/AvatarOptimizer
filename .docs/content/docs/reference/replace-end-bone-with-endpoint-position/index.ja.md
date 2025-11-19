---
title: Replace End Bone With Endpoint Position
weight: 100
---

# Replace End Bone With Endpoint Position

このコンポーネントは、PhysBoneの末端のボーンであるEnd BoneをEndpoint Positionに置き換えます。

このコンポーネントはPhysBoneコンポーネントがあるGameObjectに追加してください。

このコンポーネントがアタッチされたGameObjectに複数のPhysBoneがある場合、その全てのPhysBoneに設定が適用されます。

## 設定 {#settings}

![component.png](component.png)

### Endpoint Positionの設定方法　{#endpoint-position-mode}

Endpoint Positionの値を決定する方法を選択します。

- `Average`（平均）\
  すべてのEnd Boneのローカル座標の平均を計算して、その値をEndpoint Positionとして使用します。

- `Manual`（手動）\
  `Endpoint Positionに設定する値`で指定した値を、Endpoint Positionとして使用します。

### Endpoint Positionに設定する値 {#value-for-endpoint-position}

`Endpoint Positionの設定方法`が`Manual`の場合のみ有効です。

Endpoint Positionとして使用するローカル座標を手動で指定します。
