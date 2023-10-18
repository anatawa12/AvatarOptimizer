---
title: Merge PhysBone
weight: 100
---

# Merge PhysBone

このコンポーネントは、複数のPhysBoneコンポーネントを1つに統合します。
MultiChildTypeはIgnoreになります。

このコンポーネントは新規GameObjectに追加してください。

## 備考 {#notes}

統合対象は同じGameObjectの子である必要があります。
代わりに`このGameObjectの子にする`オプションを使用することも出来ます。

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

コライダーについては、`Merge`を選択して統合対象のコライダー一覧を統合することも出来ます。

Endpoint Positionについては、`Clear`を選択して[Clear Endpoint Position](../clear-endpoint-position)を使用することもできます。
