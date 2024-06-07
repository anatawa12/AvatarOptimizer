---
title: Component Scripting API
---

# Component Scripting API

Avatar Optimizer v1.7.0以降、AvatarにAvatar Optimizerのコンポーネントを追加・設定するためのComponent APIを提供します。
このAPIを使用することで、Avatar Optimizerのコンポーネントを追加するツールやコンポーネントを作成することができます。

## Component APIがサポートするコンポーネント {#supported-components}

現在、すべてのコンポーネントがComponent APIでサポートされているわけではありません。
Component APIでサポートされているコンポーネントのリストは以下の通りです。

- `RemoveMeshInBox` - コンポーネントの追加と設定がサポートされています
- `RemoveMeshByBlendShape` - コンポーネントの追加と設定がサポートされています
- `TraceAndOptimize` - デフォルトの設定での追加はサポートされていますが、設定はサポートされていません

設定がサポートされているコンポーネントについては、将来の機能でデフォルトで有効になる機能との互換性を保つために、設定には注意が必要です。
詳細については以下のドキュメントを参照してください。

## はじめに {#getting-started}

Component APIを使用するには、アセンブリ定義ファイルで`com.anatawa12.avatar-optimizer.runtime`アセンブリを参照する必要があります。
Avatar Optimizerはランタイムで動作しないため、ランタイムビルドに`com.anatawa12.avatar-optimizer.runtime`アセンブリを依存させてはいけません。
将来、`com.anatawa12.avatar-optimizer.runtime`からいくつかのクラスをランタイムビルドで削除するかもしれません。
言い換えると、ランタイムアセンブリで`com.anatawa12.avatar-optimizer.runtime`を使用するのは避けることが推奨されます。エディタアセンブリでのみ使用するようにしてください。

次に、コンポーネントを設定する場合は、将来の機能との互換性を確保するために`void Initialize(int version)`メソッドを呼び出す必要があります。
([動作の安定性](../../basic-concept/#behavior-stability)で説明されているように、)将来、デフォルトの設定が変更される可能性があります。
デフォルトの設定は、`GameObject.AddComponent<T>()`メソッドで追加されたコンポーネントに影響します。
したがって、将来のバージョンとの動作の互換性を保つためには、使用するデフォルト設定のバージョンを指定して`Initialize`メソッドを呼び出す必要があります。
デフォルトの設定バージョンは、`Initialize`メソッドのドキュメントに記載されているはずです。
`Initialize`メソッドを呼び出さないと、コンポーネントは予期しない動作をするか、将来エラーが発生する可能性があります。
