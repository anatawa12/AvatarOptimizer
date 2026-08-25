---
title: OSCギミックにAvatar Optimizerとの互換性をもたせる
---

# OSCギミックにAvatar Optimizerとの互換性をもたせる

このページでは、OSCツールを使用して実行時にPhysBoneやContact Receiverのパラメータを読み書きするアバターギミックの作者向けに、互換性を確保するために必要な設定について説明します。

## OSCギミックがなぜ場合に Avatar Optimizer と非互換になるか {#why}

### OSCツールによって読み取られるパラメータ {#why-read}

PhysBoneおよびContact Receiverコンポーネントは、OSCツールから直接読み取ることのできるパラメータを公開しています。
これらのパラメータは、アバターのAnimator ControllerやExpression Parametersに宣言されていない場合でも外部ツールからアクセスできます。
また、Animator ControllerやExpression Parameterに宣言されていても、実際に使用されていない場合には削除される場合があります。[^fake-usage]

このため、Avatar Optimizerは、そのようなパラメータが本当に使用されていないのか、それともOSCツールによって意図的に読み取られているのかを判別できません。

最近のアバターには、PhysBoneやContact Receiverコンポーネントを利用したギミックが含まれていることがあります。
ギミックを削除した後に、ユーザーがこれらのコンポーネントを削除し忘れることもあります。
そのため、Avatar Optimizerは、Animator Controllerで使用されておらず、かつSynced Expression Parametersとして定義されていないパラメータを未使用と判断し、それに対応するPhysBoneやContact Receiverコンポーネントを削除しようとします。

そのため、OSCツールによるこれらのパラメータの読み取りに依存しているギミックでは、コンポーネントが気付かないうちに削除され、ギミックが動作しなくなる可能性があります。

### OSCツールによって変更されるパラメータ {#why-change}

Avatar Optimizerでは、実行時に変更されることのないパラメータを分析してAnimator Controllerを最適化する機能も計画しています。

しかし、OSCツールやVRCParameterDriverによって、Avatar Optimizerが定数だと判断したパラメータが変更される場合、この最適化によってギミックが動作しなくなる可能性があります。

Avatar Optimizerはまだこの最適化を実装していませんが、将来この最適化が実装された際にもギミックの互換性を維持できるよう、そのようなパラメータを宣言してください。

## 何をすればよいか {#what-to-do}

Avatar Optimizerによってギミックが誤って最適化されるのを防ぐため、[Asset Description]を作成し、OSCツールが読み取る、または変更するすべてのパラメータを、それぞれ`Parameters Read By External Tools`または`Parameters Changed By External Tools`リストに宣言していただくようお願いします。

Asset Descriptionはアバターまたはギミックと一緒に配布してください。
Avatar OptimizerがインストールされていないプロジェクトにAsset Descriptionが存在しても問題はないため、ギミックと一緒にAsset Descriptionを配布しても、ギミックがAvatar Optimizerに依存することにはなりません。[^missing-asset]

### 手順 {#step-by-step}

1. OSCツールが読み取る、または変更するすべてのパラメータを確認してください。

   外部ツールが実行時に読み取る、または変更するすべてのAvatar Parameter名を列挙してください。

   Asset Descriptionでは正規表現を使用できるため、正確な名前を指定する必要はありません。
   例えば、`FooHaptics/OSC/`のようなプレフィックスを持つすべてのパラメータに一致するパターンを指定できます。

2. Asset Descriptionを作成してください。

   UnityのProjectウィンドウで右クリックし、`Create > Avatar Optimizer > Asset Description`を選択してください。

   ファイルの名前や保存場所は自由です。

3. パラメータを`Parameters Read By External Tools`および`Parameters Changed By External Tools`に追加してください。

   Asset DescriptionをInspectorで開き、手順1で確認した各パラメータを対応するリストに追加してください。

   この仕組みでは、ギミックがインストールされているかどうかを判別できないため、設定はプロジェクト内のすべてのアバターに適用されます。

   正規表現を使用する場合は、パラメータの定義をできるだけ具体的にしてください。

4. Asset Descriptionをギミックと一緒に配布してください。

   ギミックをインストールしたユーザーが正しい設定を自動的に取得できるよう、Asset Descriptionファイルを製品のパッケージに含めてください。

   Asset Descriptionをギミックと一緒に配布しても、ギミックがAvatar Optimizerに依存することにはならないため、ユーザーがAvatar Optimizerをインストールする必要はありません。

Asset Descriptionの詳細については、[Asset Description]のページを参照してください。

[Asset Description]: ../asset-description/
[asset-description-read]: ../asset-description/#parameters-read-by-external-tools

[^fake-usage]: 一見エフェクトがあるように見えても、実際にはそうではないアニメーション（ダミーのGameObjectをアニメーションさせるなど）を追加しようとしても、将来的にそのGameObjectやアニメーションが削除される可能性があります。そのため、Avatar Optimizerを誤解させようとしないでください。

[^missing-asset]: Asset Descriptionを正しく処理できない、設計の悪いツールによって使用された場合にのみ問題となります。私たちが知る限り、現在そのようなツールは存在しません。互換性に関する問題が発生した場合は、お知らせください。
