---
title: Merge ToonLit Material
weight: 25
---

# Merge ToonLit Material

手動でテクスチャを並び替えることにより、`VRChat/Mobile/Toon Lit`のマテリアルを1つのマテリアルに統合します。

このコンポーネントは、SkinnedMeshRendererコンポーネントのあるGameObjectに追加してください。(分類: [Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component))

現在、このコンポーネントは大きな需要があると見られる`VRChat/Mobile/Toon Lit`シェーダーのみをサポートしています。
他のシェーダーのサポートも追加する可能性があります。(サードパーティシェーダーでも対応します)
もし対応して欲しいシェーダーがあれば、[issue][issue]を書いてください。

このコンポーネントは新しいマテリアルを作成するため、このコンポーネントによって作成されたマテリアルは`AAO Merge Skinned Mesh`コンポーネントによって統合されません。
複数のレンダラーからマテリアルを統合したい場合は、`AAO Merge Skinned Mesh`コンポーネントと同じGameObjectに`Merge ToonLit Material`を追加する必要があります。

## 設定 {#settings}

`統合したマテリアルを追加`をクリックして、統合後のマテリアルを追加します。
それぞれのマテリアルについて、統合対象のマテリアルを複数選択することが出来ます。
`統合対象を追加`をクリックするか、ドロップダウンメニューからマテリアルを選択してください。
統合対象のマテリアルについて、テクスチャの配置場所を設定する必要があります。
X, Y, W, Hの値を調整してテクスチャをお好みの位置にを合わせてください。
`プレビューを生成`をクリックすると、統合後のテクスチャのプレビューがそれぞれ生成されます。

![component.png](component.png)

[issue]: https://github.com/anatawa12/AvatarOptimizer/issues/new/choose
