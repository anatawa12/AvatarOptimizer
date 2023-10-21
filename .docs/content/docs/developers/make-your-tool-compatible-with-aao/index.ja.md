---
title: ツールをAvatar Optimizerとの互換性をもたせる
---

# ツールをAvatar Optimizerとの互換性をもたせる

このページでは以下の２つのことを説明します。

- どのようなときにツールがAvatar Optimizerと非互換になるか
- どのように互換性を改善するか

もし質問があれば[fediverseで`@anatawa12@misskey.niri.la`][fediverse]に気軽に聞いてください。

## どのようなときにツールがAvatar Optimizerと非互換になるか {#when-incompatible}

もしあなたのツールがコンポーネントを追加せず、ビルド時に何もしないのであれば、すでにAvatarOptimizerと互換性があります！

もしあなたのツールがコンポーネントをアバターに追加する場合、そのツールはAvatar Optimizerと非互換である可能性があります

Avatar Optimizerはコンポーネントに対するガベージコレクションを実装しているため、Avatar Optimizerはアバターに存在する
すべてのコンポーネントについて知る必要があります。

未知なコンポーネントの問題を避けるため、Avatar Optimizerは道のコンポーネントが以下のようなものであると仮定します。(この仮定は将来的に変更される可能性があります。)
- 副作用があるコンポーネントである
- コンポーネントによって参照されてるコンポーネントに依存している

しかし、この仮定は正しくない可能性があるので、Avatar Optimizerは未知のコンポーネントを見つけた場合、以下のような警告を生成します。

![unknown-component-warning](unknown-component-warning.png)

もしあなたのツールがNDMF[^NDMF]をベースにしていない非破壊ツールで、Play modeに入るときに適用されるツールの場合、 Avatar
Optimizerがそのツールより前に適用される可能性があるため、非互換です。

## どのように互換性を改善するか {#improve-compatibility}

### NDMFを使用した非破壊ツールの場合 {#improve-compatibility-ndmf-based}

もしあなたのツールがNDMF[^NDMF]を使用した非破壊ツールの場合、Avatar Optimizerが実行される前にそのツールのコンポーネントを削除してください。

Avatar OptimizerはOptimization phaseに処理を実行するため、もしあなたのツールがOptimization phaseに何もしないのであれば、特に問題がありません。
もしあなたのツールがOptimization phaseになにか実行する場合、[`BeforePlugin`][ndmf-BeforePlugin]を用いてAvatar Optimizerの前に実行するようにしてください。
Avatar OptimizerのNDMFのQualifiedNameは`com.anatawa12.avatar-optimizer`です。

もしあなたのツールがAvatar Optimizerよりあとにあなたのコンポーネントを用いて実行する必要がある場合、 Avatar Optimizerに[コンポーネントを登録][register-component]してください.

### NDMFを使用していない非破壊ツールの場合 {#improve-compatibility-non-ndmf-based}

もしあなたのツールがNDMF[^NDMF]を使用していない非破壊ツールの場合、NDMFを使用するのを検討してください。

もしあなたのツールがPlayモードに入るときに適用される場合、Avatar Optimizerとの処理順を保証するためにはNDMFを使用する必要があります。
もしあなたのツールがPlayモードに入るときに適用されない場合、NDMFを使用しなくても問題ないことは多いです。

もしあなたのツールでNDMFを使用したくない場合、Avatar Optimizerが実行される前にそのツールのコンポーネントを削除してください。
これを達成するためにはNDMFのOptimization phaseの実行より前にあなたのツールを実行するようにしてください。
現在、NDMFはOptimization phaseをVRCSDKの`RemoveAvatarEditorOnly`の直前のorder `-1025`で実行するので、
あなたのツールの`IVRCSDKPreprocessAvatarCallback`をそれより小さい`callbackOrder`で登録してください。

もしあなたのツールがAvatarOptimizerの実行後(NDMFのOptimization phaseのあと)に処理したい場合、Avatar Optimizerに[コンポーネントを登録][register-component]してください.

### データを保持するだけのコンポーネントを持つツールの場合 {#improve-compatibility-destructive-tools}

もしあなたのツールのコンポーネントがビルド時に意味を持たず、情報保持のための場合、
AvatarOptimizerの処理の前に`IVRCSDKPreprocessAvatarCallback`でコンポーネントを削除するか、Avatar Optimizerにコンポーネントを登録してください。

もし`IVRCSDKPreprocessAvatarCallback`でコンポーネントを削除する場合、[この部分](#improve-compatibility-non-ndmf-based)を参照してください。

もしAvatar Optimizerにコンポーネントを登録する場合、[この部分][register-component]を参照してください。

## コンポーネントを登録する {#register-component}

もしあなたのツールのコンポーネントをAvatar Optimizerの処理後まで保持したり、またはAvatar Optimizerによって削除されたい場合、
Avatar Optimizerにコンポーネントの情報を登録できます。

Avatar OptimizerのAPIを呼び出すため、まず初めにassembly definition file[^asmdef]を存在しない場合作成してください。

次に、asmdefファイルのアセンブリ参照に`com.anatawa12.avatar-optimizer.api.editor`を追加してください。
もしあなたのツールがAvatar Optimizerに依存したくない場合、[Version Defines]を使用してください。
Avatar Optimizer 1.6.0より前では公開APIがなく、またAvatar Optimizer v2.0.0でAPIを破壊する可能性があるため、
バージョンの範囲を`[1.6,2.0)`(やもっと厳しい `[1.7,2.0)`など)のように指定するのを推奨します。

![version-defines.png](version-defines.png)

次に、あなたのコンポーネントに関する`CompoinentInformation`を定義してください。

```csharp
#if AVATAR_OPTIMIZER && UNITY_EDITOR

[ComponentInformation(typeof(YourComponent))]
internal class YourComponentInformation : ComponentInformation<YourComponent>
{
    protected override void CollectMutations(YourComponent component, ComponentMutationsCollector collector)
    {
        // call methods on the collector to tell about the component
    }

    protected override void CollectDependency(YourComponent component, ComponentDependencyCollector collector)
    {
        // call methods on the collector to tell about the component
    }
}

#endif
```

`CollectDependency`ではビルド時、または実行時のあなたのコンポーネントの依存関係を登録してください。
`CollectMutations`ではあなたのコンポーネントが実行時に変更する可能性があるプロパティを登録してください。
詳しくはxmldocやメソッド名を参照してください。

もしあなたのコンポーネントがエディタ上のツールのためのデータを保持するだけの場合、どちらも空にします。

[fediverse]: https://misskey.niri.la/@anatawa12
[ndmf-BeforePlugin]: https://ndmf.nadena.dev/api/nadena.dev.ndmf.fluent.Sequence.html#nadena_dev_ndmf_fluent_Sequence_BeforePlugin_System_String_System_String_System_Int32_
[register-component]: #register-component

[^asmdef]: Assembly-CSharp以外のアセンブリを定義するためのファイル。[unity docs](https://docs.unity3d.com/2019.4/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html)を参照してください。.
[^NDMF]: [NDMF] (Non-Destructive Modular Framework)は非破壊改変ツールのためのbdunderscoreさんによるフレームワークです。 
Avatar Optimizerはこのフレームワークを他の非破壊改変ツールとの互換性のために使用しています。

[NDMF]: https://ndmf.nadena.dev/
[modular-avatar]: https://modular-avatar.nadena.dev/
[Version Defines]: https://docs.unity3d.com/2019.4/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html#define-symbols
