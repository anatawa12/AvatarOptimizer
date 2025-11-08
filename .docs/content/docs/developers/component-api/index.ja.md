---
title: Component Scripting API
---

# Component Scripting API

Avatar Optimizer v1.7.0以降、アバターにAvatar Optimizerのコンポーネントを追加・設定するためのComponent APIを提供しています。
このAPIを使用することで、Avatar Optimizerのコンポーネントを追加するツールやコンポーネントを作成することができます。

## Component APIが利用可能なコンポーネント {#supported-components}

現時点では、すべてのコンポーネントがComponent APIで利用出来るわけではありません。
Component APIが利用可能なコンポーネントの一覧は以下の通りです。

- `RemoveMeshInBox` - コンポーネントの追加と追加時の設定変更がサポートされています
- `RemoveMeshByBlendShape` - コンポーネントの追加と追加時の設定変更がサポートされています
- `RemoveMeshByMask` - コンポーネントの追加と追加時の設定変更がサポートされています
- `TraceAndOptimize` - デフォルト設定での追加はサポートされていますが、設定変更はサポートされていません

将来のバージョンで追加されるデフォルトで有効な機能との互換性を保つために、コンポーネントの設定変更を行う場合には注意が必要です。
詳細については以下のドキュメントを参照してください。

## はじめに {#getting-started}

Component APIを使用するには、assembly definitionファイルで`com.anatawa12.avatar-optimizer.runtime`を参照する必要があります。
Avatar Optimizerはランタイムで動作しないため、ランタイムビルド向けで`com.anatawa12.avatar-optimizer.runtime`に依存してはいけません。\
`com.anatawa12.avatar-optimizer.runtime`にあるいくつかのクラスは、将来のバージョンでランタイム向けビルドから除外される可能性があります。
言い換えると、ランタイム向けのアセンブリで`com.anatawa12.avatar-optimizer.runtime`を使用するのは避けることをお勧めします。エディタ向けのアセンブリでのみ使用するようにしてください。

次に、コンポーネントの設定を変更する場合は、将来のバージョンで追加される機能との互換性を確保するために`void Initialize(int version)`メソッドを呼び出す必要があります。
([動作の安定性](../../basic-concept/#behavior-stability)で説明されているように、)デフォルト設定は変更される可能性があります。\
デフォルト設定は、`GameObject.AddComponent<T>()`メソッドで追加されるコンポーネントに影響します。
従って、Avatar Optimizerの将来のバージョンとの互換性を保つためには、使用するデフォルト設定のバージョンを指定して`Initialize`メソッドを呼び出す必要があります。
デフォルト設定のバージョンは、`Initialize`メソッドのドキュメントに記載されているはずです。

<blockquote class="book-hint warning">

`Initialize`メソッドを呼び出さなかった場合、コンポーネントが予期しない動作をしたり、将来的にエラーが発生したりする可能性があります。

</blockquote>

<blockquote class="book-hint info">

コンポーネントの設定変更はコンポーネントを追加した直後に行う場合のみサポートされており、既にGameObject上に存在しているコンポーネントに対する設定変更はサポートされていません。
これは、将来のバージョンで追加された機能が、既存のコンポーネントの設定内容と互換性がない可能性があるためです。

例えば、v1.8.0で追加された`AAO Remove Mesh By Box`コンポーネントの反転オプションを有効にすると、設定される箱による効果が変わってしまい、v1.7以前のみを想定して作成されているツールと互換性がなくなってしまいます。

</blockquote>
