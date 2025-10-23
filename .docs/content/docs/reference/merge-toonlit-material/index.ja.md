---
title: Merge ToonLit Material
weight: 200
---

# Merge ToonLit Material

<blockquote class="book-hint warning">

このコンポーネントは非推奨です。
代わりに、多くのシェーダーをサポートし、保存形式も改善された[Merge Material](../merge-material/)コンポーネントを使用してください。
このコンポーネントの動作が改善されることはありません。

</blockquote>

手動でテクスチャを並び替えることにより、`VRChat/Mobile/Toon Lit`のマテリアルを1つのマテリアルに統合します。

このコンポーネントは、SkinnedMeshRendererコンポーネントのあるGameObjectに追加してください。(分類: [Modifying Edit Skinned Mesh Component](../../component-kind/edit-skinned-mesh-components#modifying-component))

現在、このコンポーネントは大きな需要があると見られる`VRChat/Mobile/Toon Lit`シェーダーのみをサポートしています。
他のシェーダーのサポートも追加する可能性があります。(サードパーティシェーダーでも対応します)
もし対応して欲しいシェーダーがあれば、[issue][issue]を書いてください。

このコンポーネントは新しいマテリアルを作成するため、このコンポーネントで統合されたマテリアルを使用しているマテリアルスロットは`AAO Merge Skinned Mesh`コンポーネントで統合されません。
別々のメッシュで使用されているマテリアルを纏めて統合したい場合は、`AAO Merge Skinned Mesh`コンポーネントのあるGameObjectと同じGameObjectに`Merge ToonLit Material`コンポーネントを追加する必要があります。

## 設定 {#settings}

`統合したマテリアルを追加`をクリックして、統合後のマテリアルを追加します。
それぞれのマテリアルについて、統合対象のマテリアルを複数選択することが出来ます。
`統合対象を追加`をクリックするか、ドロップダウンメニューからマテリアルを選択してください。
統合対象のマテリアルについて、テクスチャの配置場所を設定する必要があります。
X, Y, W, Hの値を調整してテクスチャをお好みの位置にを合わせてください。
`プレビューを生成`をクリックすると、統合後のテクスチャのプレビューがそれぞれ生成されます。

![component.png](component.png)

[issue]: https://github.com/anatawa12/AvatarOptimizer/issues/new/choose
