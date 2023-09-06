---
title: Trace And Optimize
weight: 11
aliases:
  - /ja/docs/reference/automatic-configuration/
---

# Trace And Optimize

<i>昔のバージョンではAutomatic Configurationという名前でした</i>

このコンポーネントは、アバターを走査して自動的にできる限りの最適化を行います。
チェックボックスで自動的に行う最適化を選択することが出来ます。

このコンポーネントは[Avatar Global Component](../../component-kind/avatar-global-components)であるため、アバターのルートに追加してください。

現在、以下の機能を使った自動最適化が可能です。
- [FreezeBlendShape](../freeze-blendshape)
  アニメーションなどで使われていないBlendShapeを自動的に固定・除去します。
- `使われていないObjectを自動的に削除する`
  アニメーションなどを走査して、使われていないObjectを自動的に削除します。
  - `endボーンを残す`
    チェックされている場合、親が削除されていないendボーン[^endbone]を削除しません。

また、以下の設定で自動設定を調節できます。
- `MMDワールドとの互換性`
  MMDワールドで使われるBlendShapeを固定しないなど、MMDワールドとの互換性を考慮した軽量化を行います。

他に、バグの回避などに使用するための高度な設定がありますが、それらは不安定であり、不用意に変更するべきではありません。
それらの機能については英語のTooltipやソースコード、または開発者の指示を参考にしてください。

![component.png](component.png)

[^endbone]: AAOは名前が`end`で終わる(大文字小文字区別なし)ボーンをendボーンだとみなします。
