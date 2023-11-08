---
title: ツールにAvatar Optimizerとの互換性をもたせる
---

# ツールにAvatar Optimizerとの互換性をもたせる

このページでは以下の２つのことを説明します。

- ツールはどのような場合にAvatar Optimizerと非互換になるか
- どのように互換性を改善するか

もし質問があれば、お気軽に[fediverseで`@anatawa12@misskey.niri.la`][fediverse]にご連絡ください。

## ツールはどのような場合にAvatar Optimizerと非互換になるか {#when-incompatible}

ツールがコンポーネントを追加せず、ビルド時に何もしないのであれば、そのツールは既にAvatar Optimizerと互換性があります！

それに対し、ツールが何らかのコンポーネントをアバターに追加する場合、そのツールはAvatar Optimizerと互換性が無い可能性があります。

Avatar Optimizerはコンポーネント等に対するガベージコレクションシステムを実装しているため、最適化時にアバターに存在するすべてのコンポーネントのことを知る必要があります。

未知のコンポーネントによる問題を避けるため、Avatar Optimizerは未知のコンポーネントが以下のようなものであると仮定します。(この仮定は将来的に変更される可能性があります。)
- 副作用のあるコンポーネントである。
- コンポーネントが参照している全てのコンポーネントに依存している。

この仮定は正しくない可能性があるため、Avatar Optimizerは未知のコンポーネントを見つけた場合に以下のような警告を生成します。

![unknown-component-warning](unknown-component-warning.png)

また、ツールがNDMF[^NDMF]ベースでなく、Playモードに入るときに適用される非破壊ツールの場合、 Avatar
Optimizerがそのツールより前に処理する可能性があります。

## どのように互換性を改善するか {#improve-compatibility}

### NDMFを使用した非破壊ツールの場合 {#improve-compatibility-ndmf-based}

ツールがNDMF[^NDMF]を使用した非破壊ツールの場合、Avatar Optimizerが処理する前にそのツールのコンポーネントを削除してください。

Avatar OptimizerはOptimization phaseで殆どの処理を行うため、ツールがOptimization phaseで何もしないのであれば、特に問題はありません。\
ツールがOptimization phaseでコンポーネントを使って何らかの処理を行う場合、[`BeforePlugin`][ndmf-BeforePlugin]を用いてAvatar Optimizerの前に処理することを検討してください。
NDMFでのAvatar OptimizerのQualifiedNameは`com.anatawa12.avatar-optimizer`です。

Avatar Optimizerより後のOptimization phaseでコンポーネントを使って何らかの処理を行う必要がある場合、Avatar Optimizerに[コンポーネントを登録][register-component]してください。

### NDMFを使用していない非破壊ツールの場合 {#improve-compatibility-non-ndmf-based}

ツールがNDMF[^NDMF]を使用していない非破壊ツールの場合、NDMFを使用することを検討してください。

ツールがPlayモードに入るときに適用されるものの場合、Avatar Optimizerとの処理順を保証するためにNDMFを使用する必要があります。\
これに対し、アバターのビルド時にのみ処理を行う場合、NDMFを使用する必要はありません。

NDMFを使用したくない場合、Avatar Optimizerが処理する前にそのツールのコンポーネントを削除してください。
これを行うには、NDMFのOptimization phaseより前にツールに処理させる必要があります。\
現在のNDMFは、VRCSDKの`RemoveAvatarEditorOnly`の直前であるorder `-1025`でOptimization phaseを実行するので、
ツールの`IVRCSDKPreprocessAvatarCallback`はそれより小さい`callbackOrder`である必要があります。

Avatar Optimizer(NDMFのOptimization phase)よりも後でコンポーネントを使って何らかの処理を行う必要がある場合、Avatar Optimizerに[コンポーネントを登録][register-component]してください。

### データを保持するためだけにコンポーネントを持つツールの場合 {#improve-compatibility-destructive-tools}

ツールのコンポーネントがデータを保持する役割しかなく、ビルド時には意味を持っていない場合、
Avatar Optimizerが処理する前に`IVRCSDKPreprocessAvatarCallback`でコンポーネントを削除するか([この部分](#improve-compatibility-non-ndmf-based)を参照)、
Avatar Optimizerにコンポーネントを登録してください([この部分][register-component]を参照)。

### コンポーネントを登録する {#register-component}

ツールのコンポーネントをAvatar Optimizerの処理で削除したい、または残しておきたい場合、
Avatar Optimizerにコンポーネントの情報を登録できます。

まず、Avatar OptimizerのAPIを呼び出すために、assembly definitionファイル[^asmdef]を(存在しない場合)作成してください。

次に、asmdefファイルのアセンブリ参照に`com.anatawa12.avatar-optimizer.api.editor`を追加してください。\
ツールをAvatar Optimizerに依存させたくない場合、[Version Defines]を使用してください。
Avatar Optimizer 1.6.0より前にはAPIが無く、Avatar Optimizer 2.0.0ではAPIの互換性を破壊する可能性があるため、
バージョンの範囲を`[1.6,2.0)`(または、将来的に追加されたAPIを用いる必要がある場合、より厳密に `[1.7,2.0)`など)のように指定することを推奨します。

![version-defines.png](version-defines.png)

続いて、ツールのコンポーネントについての`ComponentInformation`を定義してください。

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

`CollectMutations`では、コンポーネントの処理によって変更される可能性があるプロパティを登録します。\
`CollectDependency`では、ビルド時や実行時でのコンポーネントの依存関係を登録します。\
詳しくはxmldocやメソッド名を参照してください。

コンポーネントがデータを保持するためだけのものであれば、どちらも空にします。

[fediverse]: https://misskey.niri.la/@anatawa12
[ndmf-BeforePlugin]: https://ndmf.nadena.dev/api/nadena.dev.ndmf.fluent.Sequence.html#nadena_dev_ndmf_fluent_Sequence_BeforePlugin_System_String_System_String_System_Int32_
[register-component]: #register-component

[^asmdef]: Assembly-CSharp以外のアセンブリを定義するためのファイル。[unity docs](https://docs.unity3d.com/2019.4/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html)を参照してください。
[^NDMF]: bdunderscoreさんが作成した[NDMF] (Non-Destructive Modular Framework)は、非破壊改変ツールのためのフレームワークです。
Avatar Optimizerは他の非破壊改変ツールとの互換性を確保するためにこのフレームワークを使用しています。

[NDMF]: https://ndmf.nadena.dev/
[modular-avatar]: https://modular-avatar.nadena.dev/
[Version Defines]: https://docs.unity3d.com/2019.4/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html#define-symbols
