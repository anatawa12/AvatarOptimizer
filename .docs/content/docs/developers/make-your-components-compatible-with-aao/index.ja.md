---
title: コンポーネントにAvatar Optimizerとの互換性をもたせる
---

# コンポーネントにAvatar Optimizerとの互換性をもたせる

このページでは以下の2つのことを説明します。

- コンポーネントはどのような場合にAvatar Optimizerと非互換になるか
- どのように互換性を改善するか

もし質問があれば、お気軽に[fediverseで`@anatawa12@misskey.niri.la`][fediverse]にご連絡ください。

## コンポーネントはどのような場合にAvatar Optimizerと非互換になるか {#when-incompatible}

Avatar Optimizerが処理する時点でアバターにコンポーネントが存在している場合、そのコンポーネントはAvatar Optimizerと互換性が無い可能性があります。

Avatar Optimizerはビルドプロセスの最後の方に処理するようにデザイされており、Avatar Optimizer にとって未知のコンポーネントはサポートされていません。

例えば、Avatar Optimizerはコンポーネント等に対するガベージコレクションシステムを実装しています。
未使用のコンポーネントを正しく削除し、使用されているコンポーネントを正しく保持するためには、
最適化時にアバターに存在するすべてのコンポーネントのことを知る必要があります。

未知のコンポーネントによる問題を避けるため、Avatar Optimizerは未知のコンポーネントが以下のようなものであると仮定します。
- コンポーネントが実行時に有効でアクティブになる可能性がある場合は保持する必要がある。
  - これは、Avatar Optimizerが未知のコンポーネントを実行時コンポーネントと仮定しているためです。
- コンポーネントが参照している全てのコンポーネントに依存している。

(上記の仮定は将来変更される可能性があります。)

しかしながら、この仮定は正しくない可能性があるため、Avatar Optimizerは以下のような警告を生成します。

![unknown-component-warning](unknown-component-warning.png)

## どのように互換性を改善するか {#improve-compatibility}

互換性を改善するためには、以下のいずれかの方法を実装できます。

1. Avatar Optimizerが処理する前にコンポーネントを削除する

   あなたのコンポーネントが実行時には動作しない場合(つまり、ビルド時や編集モードのみのコンポーネントである場合)、
   あなたのツールがAvatar Optimizerの処理より前にアバターを処理し、コンポーネントを削除するのが殆どの場合で最善です。

   詳細は[以下のセクション](#remove-component)を参照してください。

   Avatar Optimizerは、TraceとOptimizeの前に処理されるAvatar Optimizerコンポーネントに対してこの方法を内部で使用しています。

2. APIを使ってAvatar Optimizerにコンポーネントを登録する

    コンポーネントが実行時に動作する場合、またはあなたのツールがAvatar Optimizerの処理後にアバターを処理したい場合、
    コンポーネントをAvatar Optimizerに登録して、コンポーネントについて知らせることができます。
    
     詳細は[以下のセクション](#register-component)を参照してください。
    
     Avatar Optimizerは、TraceとOptimizeの後に処理される一部のコンポーネントや、Unity、VRCSDK、などアバターのプラットフォームのコンポーネントを保持するためにこの方法を内部で使用しています。

3. Asset Descriptionで削除しても問題がないものとしてコンポーネントを登録する

   Avatar Optimizer v1.7.0以降では、ビルド時やランタイムで処理を行わないコンポーネント向けに[Asset Description]が追加されています。
   ツールがビルド時やランタイムで何も処理しない場合、Avatar Optimizerがコ��ポーネントを削除する代わりに、
   この方法を使用してコンポーネントを登録することができます。
   詳細は[Asset Description]を参照してください。

   ツールがビルド時に何かしらの処理を行う場合、Asset Descriptionでコンポーネントを登録することは推奨されません。
   Asset Descriptionを使用してビルド時に何かしらの処理を行うコンポーネントを登録すると、
   実行順序が正しくないか、予期しない変更があった場合に、コンポーネントが予期せず削除され、ツールが無効になる可能性があります。

   この方法は、Avatar Optimizerがよく知られた編集モードツールとの互換性を保つために内部で使用されています。

### コンポーネントを削除する {#remove-component}

[`DestroyImmediate`]を使用してコンポーネントを削除できます。

ビルド時にAvatar Optimizerが処理する前に処理し、コンポーネントを削除する方法はいくつかあります。

ツールがNDMF[^NDMF]を使用した非破壊ツールの場合は、NDMFのOptimizing phaseより前のPhaseか、
Optimizing phaseの中で([`BeforePlugin`][ndmf-BeforePlugin]を用いて)`com.anatawa12.avatar-optimizer` plugin
より前にコンポーネントを削除することを推奨します。
もし、ツールがOptimizing phaseでコンポーネントを削除する場合、デフォルトのコールバック順序が`com.anatawa12.avatar-optimizer` pluginより前であっても、
[`BeforePlugin`][ndmf-BeforePlugin]を指定することを強く推奨します。

ツールがNDMF[^NDMF]を使用していない非破壊ツールの場合は、NDMFのOptimizing phaseより前にコンポーネントを削除することを推奨します。
この場合、現在のNDMFはVRCSDKの`RemoveAvatarEditorOnly`の直前であるorder `-1025`でOptimizing phaseを実行するので、
それより小さい`callbackOrder`を指定した`IVRCSDKPreprocessAvatarCallback`でコンポーネントを削除してください。

もし、ツールのコンポーネントがデータを保持する役割しかなく、ビルド時には意味を持っていない場合、
上記のように`IVRCSDKPreprocessAvatarCallback`でコンポーネントを削除するか、
[Asset Description]を使用してコンポーネントを削除できるコンポーネントとして登録することができます。

[DestroyImmediate]: https://docs.unity3d.com/ScriptReference/Object.DestroyImmediate.html

### コンポーネントを登録する {#register-component}

ツールのコンポーネントをAvatar Optimizerの処理より後に残しておきたい場合、
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
詳しくはそれぞれのメソッドのxmldocを参照してください。

[fediverse]: https://misskey.niri.la/@anatawa12
[ndmf-BeforePlugin]: https://ndmf.nadena.dev/api/nadena.dev.ndmf.fluent.Sequence.html#nadena_dev_ndmf_fluent_Sequence_BeforePlugin_System_String_System_String_System_Int32_
[register-component]: #register-component

[^asmdef]: Assembly-CSharp以外のアセンブリを定義するためのファイル。[unity docs](https://docs.unity3d.com/2019.4/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html)を参照してください。
[^NDMF]: bdunderscoreさんが作成した[NDMF] (Non-Destructive Modular Framework)は、非破壊改変ツールのためのフレームワークです。
Avatar Optimizerは他の非破壊改変ツールとの互換性を確保するためにこのフレームワークを使用しています。

[NDMF]: https://ndmf.nadena.dev/
[modular-avatar]: https://modular-avatar.nadena.dev/
[Version Defines]: https://docs.unity3d.com/2019.4/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html#define-symbols
