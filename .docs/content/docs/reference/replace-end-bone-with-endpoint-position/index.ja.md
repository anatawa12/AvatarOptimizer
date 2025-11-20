---
title: Replace End Bone With Endpoint Position
weight: 100
---

# Replace End Bone With Endpoint Position

このコンポーネントは、PhysBoneの末端のボーンであるEnd BoneをEndpoint Positionに置き換えます。

このコンポーネントはPhysBoneコンポーネントがあるGameObjectに追加してください。

このコンポーネントがアタッチされたGameObjectに複数のPhysBoneがある場合、その全てのPhysBoneに設定が適用されます。

## 利点 {#benefits}

End BoneをEndpoint Positionに置き換えることにより、VRChatのPerformance Rankシステムにおける、`PhysBone Affected Transforms`の数を減らすことができます。

## 設定 {#settings}

![component.png](component.png)

### Endpoint Positionの設定方法　{#endpoint-position-mode}

Endpoint Positionの値を決定する方法を選択します。

- `Average`（平均）\
  適用対象の各PhysBoneにおいて、End Boneのローカル座標の平均を計算して、その値をEndpoint Positionとして使用します。

- `Override`（オーバーライド）\
  手動で`Endpoint Position Override`に指定した値を、Endpoint Positionとして使用します。

### Endpoint Position Override {#endpoint-position-override}

このオプションは、`Endpoint Positionの設定方法`が`Override`の場合のみ利用できます。

ここでEndpoint Positionとして使用するローカル座標を直接入力できます。
