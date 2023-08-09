---
title: Trace And Optimize
weight: 1
aliases:
  - /ja/docs/reference/automatic-configuration/
---

# Trace And Optimize

<i>昔のバージョンではAutomatic Configurationという名前でした</i>

このコンポーネントは、アバターを走査して可能な限り自動的に最適化を行います。
チェックボックスで自動的にする最適化を選択することが出来ます。

現在、以下の自動的な最適化が行われます
- [FreezeBlendShape](../freeze-blendshape)
  アニメーションなどで使われていないBlendShapeを自動的に固定・除去します。
- `使われていないObjectを自動的に削除する`
  アニメーションなどを走査して、使われていないObjectを自動的に削除します。

また、以下の設定で自動設定を調節できます。
- `MMDワールドとの互換性`
  MMDワールドで使われるBlendShapeを固定しないなど、MMDワールドとの互換性を考慮した軽量化を行います。

![component.png](component.png)
