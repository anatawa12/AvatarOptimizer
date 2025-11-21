---
title: Merge PhysBone
weight: 100
---

# Merge PhysBone (MergePB) {#merge-physbone}

このコンポーネントは、複数のPhysBoneコンポーネントを1つに統合します。
MultiChildTypeはIgnoreになります。

このコンポーネントは新規GameObjectに追加してください。

## 備考 {#notes}

統合対象は同じGameObjectの子である必要があります。
代わりに`このGameObjectの子にする`オプションを使用することも出来ます。

このコンポーネントは、PhysBoneのルートとなるGameObjectを新たに作成し、統合対象のPhysBoneによって揺らされるボーンを、作成したGameObjectの子にします。\
なお、ルートとなっているGameObjectも、PhysBoneによって影響を受けるボーンの1つとみなされるため、各Merge PhysBoneごとに`PhysBone Affected Transforms`の数が1つ増えてしまいます。
このコンポーネントによって追加されるGameObjectがPhysBoneによって揺らされることはないため、これはVRChatのPerformance Rankシステムのバグである可能性があります。

### Grabの動作について {#about-grab-behavior}

複数のPhysBoneを統合すると、Grabの動作が少し変わります。
統合前は、それぞれのPhysBoneを個別に掴むことができましたが、統合後は全てのボーンが1つのPhysBoneシステムとして扱われます。
これは、統合されたPhysBoneの`MultiChildType`が`Ignore`に設定されるためです。
ほとんどの場合、これによる影響は小さいですが、複数のPhysBoneを個別に操作する必要がある場合は、統合しないことを検討してください。

## 設定 {#settings}

![component.png](component.png)

### このGameObjectの子にする {#make-children-of-me}

チェックされている場合、統合対象のPhysBoneが揺らすボーンがこのGameObjectの子になるようにします。

### コンポーネント {#components}

統合対象のPhysBoneコンポーネントの一覧です。

一番下の"None"と書いてある要素にドラッグ&ドロップすることにより対象を追加し、Noneに戻すことにより対象を一覧から取り除きます。

### オーバーライド(上書き) {#overrides}

上記の設定項目の下は、PhysBoneの設定項目のようになっています。
それぞれの項目について、統合対象の項目から値をコピーする場合は`Copy`(すべての統合対象で値が同じ場合のみ有効)、
代わりに新しい値を設定する場合は`Override`を選択してください。

コライダーについては、`Merge`を選択して統合対象のコライダー一覧を統合することができます。

Endpoint Positionについては、`Clear`を選択して[Clear Endpoint Position](../clear-endpoint-position)を使用することができます。

角度制限では、`Fix`を選択することで、ボーンに対する捻るような回転(Roll)の値を自動で揃えられます。
これにより、Rollの値だけが異なっているような場合に角度制限を纏めて適用することができます。