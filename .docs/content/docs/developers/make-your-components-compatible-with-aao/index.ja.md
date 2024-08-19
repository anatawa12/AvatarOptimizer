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


To improve the compatibility, you may implement one of the following methods.

1. Remove your components before Avatar Optimizer processes as much as possible.

   If your component is not working at runtime, (in other words, it's a build-time or edit mode only component),
   it's mostly better for your tool to process avatar before Avatar Optimizer processes,
   and remove your components before Avatar Optimizer processes.

   Please refer [section below](#remove-component) for more details.

   Avatar Optimizer internally uses this method for most Avatar Optimizer components,
   that will be processed before Trace and Optimize.

2. Register your components to Avatar Optimizer with API

   If your component is working at runtime, or your tool actually want to process avatar after Avatar Optimizer processes,
   you can register your components to Avatar Optimizer to tell about your components.

   Please refer [section below](#register-component) for more details.

   Avatar Optimizer internally uses this method to keep some components that are processed after Trace and Optimize,
   and components from Unity, VRCSDK, and other avatar platform components.

3. Register your components as no problems to remove with Asset Description.

   Since Avatar Optimizer v1.7.0, you can use [Asset Description] to register components only for preserving data
   for edit-mode tools, that doesn't effects on build or at runtime.
   Please refer [Asset Description] for more details.
   If your tool process nothing at build time or runtime, you can use this to register your components instead of
   removing your components before Avatar Optimizer processes.

   If your tool process something at build time, registering with Asset Description is not recommended.
   Using Asset Description for components that process something at build time may unexpectedly
   remove your components and disables your tool if the execution order is incorrect or unexpectedly changed.

   This method is internally used by Avatar Optimizer to keep compatibility with well-known edit-mode tools.


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

You can remove your components with [`DestroyImmediate`][DestroyImmediate] to remove your components.

There is several ways to process and remove your component from avatar before Avatar Optimizer processes on build.

If your tool is a non-destructive tool based on NDMF[^NDMF], you can remove your components before the phases
prior to the Optimizing phase of NDMF or before `com.anatawa12.avatar-optimizer` plugin
(with [`BeforePlugin`][ndmf-BeforePlugin]) in the Optimizing phase.
If your tool removes your components in Optimizing phase, it's highly recommended to specify [`BeforePlugin`][ndmf-BeforePlugin]
even if your default callback order is before `com.anatawa12.avatar-optimizer` plugin.

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


If your components is only for holding information for your edit mode tool and has no meaning on the build time,
you can remove your components in `IVRCSDKPreprocessAvatarCallback` as described above, or
you can somply use [Asset Description] to register your components to be removed.

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
