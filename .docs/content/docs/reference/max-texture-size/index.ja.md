---
title: Max Texture Size
weight: 1000
---

# Max Texture Size

特定のmipmapレベルを抽出してテクスチャサイズを縮小します。

このコンポーネントは、任意のGameObjectに追加でき、そのGameObjectとその子全てに適用されます。

mipmapを使用してテクスチャを縮小し、元のテクスチャフォーマットと設定を保持します。

複数の Max Texture Size コンポーネントがある場合には、最も近い親の設定が使用され、テクスチャが複数の設定に影響されている場合には、最小サイズが適用されます。

## 制限事項 {#limitations}

### Mipmapが必要 {#mipmaps-required}

テクスチャをリサイズするには、mipmapが有効になっている必要があります。mipmapのないテクスチャや、ターゲットサイズに到達するのに十分なmipmapレベルを持っていないテクスチャはスキップされ、警告が表示されます。

### Crunch 圧縮テクスチャ {#crunch-copmpressed-textures}

Crunch 圧縮を使用するテクスチャはリサイズできません。Crunch圧縮されたテクスチャが検出された場合、ビルドログに警告が表示されます。

## 設定 {#settings}

![component.png](component.png)

### Max Texture Size

最大テクスチャサイズを選択します。
