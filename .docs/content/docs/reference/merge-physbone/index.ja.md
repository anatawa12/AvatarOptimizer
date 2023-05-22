---
title: Merge PhysBone
weight: 1
---

# Merge PhysBone

このコンポーネントは、複数のPhysBoneコンポーネントを1つに統合します。
MultiChildTypeはIgnoreになります。

加えて、Endpoint Positionの値は0に置換され、`_EndPhysBone`GameObjectが追加されます。
これは[Clear Endpoint Position](../clear-endpoint-position)の動作と同じです。

## 設定 {#Settings}

![component.png](component.png)

### コンポーネント {#components}

統合対象のPhysBoneコンポーネントの一覧です。

一番下の"None"と書いてある要素にドラッグ&ドロップすることにより対象を追加し、Noneに戻すことにより対象を一覧から取り除きます。

### オーバーライド(上書き) {#overrides}

上記の設定項目の下は、PhysBoneの設定項目のようになっています。
それぞれの項目について、統合対象の項目から値をコピーする場合は`Copy`(すべての統合対象で値が同じ場合のみ有効)、
代わりに新しい値を設定する場合は`Override`を選択してください。

コライダーについては、`Merge`を選択して統合対象のコライダー一覧を統合することも出来ます。
