---
title: コンポーネントにAvatar Optimizerとの互換性をもたせる
---

# コンポーネントにAvatar Optimizerとの互換性をもたせる

このページでは以下の2つのことを説明します。

- コンポーネントはどのような場合にAvatar Optimizerと非互換になるか
- どのように互換性を改善するか

もし質問があれば、お気軽に[Fediverse (Misskey / Mastodon)][fediverse]や[NDMF Discord]でご連絡ください。

## コンポーネントはどのような場合にAvatar Optimizerと非互換になるか {#when-incompatible}

Avatar Optimizerが処理する時点でアバターにコンポーネントが存在している場合、そのコンポーネントはAvatar Optimizerと互換性が無い可能性があります。

Avatar Optimizerはビルド処理の最後の方で動作するように設計されているため、Avatar Optimizerにとって未知のコンポーネントについてはサポートしていません。

例えば、Avatar Optimizerはコンポーネント等に対するガベージコレクションシステムを実装しています。
使用されているコンポーネントを保持し、未使用のコンポーネントを適切に削除するためには、
最適化時にアバターに存在するすべてのコンポーネントのことを知っておく必要があります。

未知のコンポーネントによる問題を避けるため、Avatar Optimizerは未知のコンポーネントが以下のようなものであると仮定します。
- コンポーネントが有効かつアクティブになる可能性がある場合は保持される必要がある。
  - これは、未知のコンポーネントが実行時(VRChat上など)に動作するコンポーネントであると仮定しているためです。
- コンポーネントが参照している全てのコンポーネントに依存している。

(上記の仮定は将来的に変更される可能性があります。)

しかしながら、これらの仮定は正しくない可能性があるため、Avatar Optimizerは以下のような警告を生成します。

![unknown-component-warning](unknown-component-warning.png)

## どのように互換性を改善するか {#improve-compatibility}

以下の対応のいずれかを行うことで、互換性を改善することができます。

1. Avatar Optimizerが処理する前にコンポーネントを削除する

   コンポーネントが実行時では動作しないものである場合、(すなわち、ビルド時や編集モードでのみ動作するコンポーネントである場合、)
   Avatar Optimizerより前にアバターを処理し、コンポーネントを削除することが殆どの場合で最善です。

   詳細は[下のセクション](#remove-component)を参照してください。

   Avatar Optimizerは、Trace and Optimizeの前に処理を行う殆どのAvatar Optimizerコンポーネントにおいて、内部的にこの方法を使用しています。

2. APIを使用してAvatar Optimizerにコンポーネントを登録する

    実行時に動作するコンポーネントや、Avatar Optimizerより後にアバターを処理するために残しておく必要があるコンポーネントの場合、
    そのコンポーネントの情報をAvatar Optimizerに登録することができます。
    
     詳細は[下のセクション](#register-component)を参照してください。
    
     Avatar Optimizerは、Trace and Optimizeの後に処理を行う一部のコンポーネントや、Unity純正のコンポーネント、VRCSDKのコンポーネントなどを保持するために、内部的にこの方法を使用しています。

3. Asset Descriptionを使用して、削除しても問題のないコンポーネントとしてAvatar Optimizerに登録する

   Avatar Optimizer v1.7.0以降では、実行時やビルド時で処理を行わないコンポーネント向けに[Asset Description]が追加されています。
   ツールが実行時やビルド時に何も行わない場合は、「Avatar Optimizerが処理する前にコンポーネントを削除する」代わりにこの方法を使用してコンポーネントを登録することができます。

   詳細は[Asset Description]を参照してください。

   なお、ツールが実行時やビルド時に何らかの処理を行う場合は、そのコンポーネントをAsset Descriptionで登録することは非推奨です。
   実行時やビルド時に何らかの処理を行うコンポーネントをAsset Descriptionで登録してしまうと、
   実行順に予期しない変更があったり、指定が正しくなかったりした場合に、コンポーネントがAvatar Optimizerに意図せず削除されてツールが正常に動作できなくなる可能性があります。

   Avatar Optimizerは、編集モードでのみ動作するよく知られているツールとの互換性を保つために、内部的にこの方法を使用しています。

### コンポーネントを削除する {#remove-component}


ビルド時において、Avatar Optimizerより前にアバターを処理し、コンポーネントを削除するための方法はいくつかあります。削除には[`DestroyImmediate`]を用います。

ツールがNDMF[^NDMF]を使用した非破壊ツールの場合は、NDMFのOptimizing phaseより前のPhaseか、
Optimizing phaseの中で([`BeforePlugin`][ndmf-BeforePlugin]を用いて)`com.anatawa12.avatar-optimizer` plugin
より前にコンポーネントを削除することを推奨します。
Optimizing phaseの中でコンポーネントを削除する場合は、デフォルトのコールバック順序が`com.anatawa12.avatar-optimizer` pluginより前であっても、
[`BeforePlugin`][ndmf-BeforePlugin]を指定しておくことを強く推奨します。

ツールがNDMF[^NDMF]を使用していない非破壊ツールの場合は、NDMFのOptimizing phaseより前にコンポーネントを削除することを推奨します。
この場合、現在のNDMFはVRCSDKの`RemoveAvatarEditorOnly`の直前であるorder `-1025`でOptimizing phaseを実行するので、
`-1025`より小さい`callbackOrder`を指定した`IVRCSDKPreprocessAvatarCallback`でコンポーネントを削除してください。

ツールのコンポーネントにデータを保持する役割しかなく、実行時やビルド時では処理を行わない場合、
上記のように`IVRCSDKPreprocessAvatarCallback`でコンポーネントを削除するか、
[Asset Description]を使用し、削除しても問題のないコンポーネントとして登録することができます。

[DestroyImmediate]: https://docs.unity3d.com/ScriptReference/Object.DestroyImmediate.html

### コンポーネントを登録する {#register-component}

ツールのコンポーネントをAvatar Optimizerの処理より後に残しておきたい場合、
Avatar Optimizerにコンポーネントの情報を登録できます。

まず、Avatar OptimizerのAPIを呼び出すために、assembly definitionファイル[^asmdef]を(存在しない場合)作成してください。

次に、asmdefファイルのアセンブリ参照に`com.anatawa12.avatar-optimizer.api.editor`を追加してください。\
ツールをAvatar Optimizerに依存させたくない場合、[Version Defines]を使用してください。
Avatar Optimizer 1.6.0より前にはAPIが無く、Avatar Optimizer 2.0.0ではAPIの互換性を破壊する可能性があるため、
バージョンの範囲を`[1.6,2.0)`(または、将来的に追加されたAPIを用いる必要がある場合、より厳密に `[1.7,2.0)`など)のように指定することを推奨します。

![version-defines.png](../version-defines.png)

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
詳しくはそれぞれのメソッドのxmldocを参照してください。

### Asset Descriptionを作成する {#asset-description}

別ページにある[Asset Description]を参照してください

[fediverse]: https://misskey.niri.la/@anatawa12
[ndmf-BeforePlugin]: https://ndmf.nadena.dev/api/nadena.dev.ndmf.fluent.Sequence.html#nadena_dev_ndmf_fluent_Sequence_BeforePlugin_System_String_System_String_System_Int32_
[register-component]: #register-component

[^asmdef]: Assembly-CSharp以外のアセンブリを定義するためのファイル。[unity docs](https://docs.unity3d.com/2019.4/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html)を参照してください。
[^NDMF]: bdunderscoreさんが作成した[NDMF] (Non-Destructive Modular Framework)は、非破壊改変ツールのためのフレームワークです。
Avatar Optimizerは他の非破壊改変ツールとの互換性を確保するためにこのフレームワークを使用しています。

[NDMF]: https://ndmf.nadena.dev/
[modular-avatar]: https://modular-avatar.nadena.dev/
[Version Defines]: https://docs.unity3d.com/2019.4/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html#define-symbols
[Asset Description]: ../asset-description
[NDMF Discord]: https://discord.gg/dV4cVpewmM
