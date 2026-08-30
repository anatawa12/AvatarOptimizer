---
title: OSCギミックにAvatar Optimizerとの互換性をもたせる
---

# OSCギミックにAvatar Optimizerとの互換性をもたせる

このページでは、OSCツールを使用して実行時にPhysBoneやContact Receiverのパラメーターを読み書きするアバターギミックの作者向けに、互換性を確保するために必要な設定について説明します。

## なぜOSCギミックがAvatar Optimizerと非互換になる場合があるか {#why}

### OSCツールによるパラメーターの読み取り {#why-read}

最近のアバターはPhysBoneやContact Receiverコンポーネントを使用した独自のギミックが含まれている場合があり、ユーザーがギミックを削除する際には、これらのコンポーネントを削除し忘れる可能性が高いです。\
そのため、Avatar Optimizerは、Animator ParameterやExpression Parameterを通じてアバターを変化させることがないコンポーネントについては削除するように設計されています。[^fake-usage]

しかしながら、PhysBoneやContact Receiverは、OSCツールから直接読み取ることができるパラメーターを公開しており、
Avatar Optimizerは、そのようなパラメーターが本当に使用されていないのか、OSCツールによって読み取られることが意図されているのかを判別することができません。\
そのため、OSCツールによってパラメーターを読み取ることが意図されていた場合も、コンポーネントを削除してしまう可能性があります。

### OSCツールによるパラメーターの変更 {#why-change}

Avatar Optimizerでは、実行時に変更されないパラメーターを分析してAnimator Controllerを最適化する機能の実装も計画しています。\
しかし、Avatar Optimizerが定数だと判断したパラメーターが、実際にはOSCツールやVRCParameterDriverによって変更されるものであった場合、この最適化によってギミックが動作しなくなる可能性があります。

Avatar Optimizerはまだこの最適化を実装していませんが、将来この最適化が実装された場合にギミックとの互換性を維持できるよう、パラメーターを宣言するようお願いします。

## 何をすればよいか {#what-to-do}

Avatar Optimizerがギミックに誤った最適化を適用してしまうことを防ぐために、[Asset Description]を作成し、OSCツールが読み取る、または変更するすべてのパラメーターを、それぞれ`Parameters Read By External Tools`または`Parameters Changed By External Tools`リストに宣言していただくようお願いします。

その後、作成したAsset Descriptionをアバター、またはギミックと一緒に配布してください。\
Avatar OptimizerがインストールされていないプロジェクトにAsset Descriptionファイルが存在した場合でも特に問題は発生しないため、ギミックと一緒にAsset Descriptionを配布しても、ギミックがAvatar Optimizerに依存することにはなりません。[^missing-asset]

### 手順 {#step-by-step}

1. OSCツールが読み取る、または変更するすべてのパラメーターを確認してください。

   ツールが実行時に読み取る、または変更するすべてのパラメーター名を列挙してください。\
   Asset Descriptionは正規表現に対応しているため、必ずしも正確な名前を指定する必要はありません。\
   例えば、`FooHaptics/OSC/`のような接頭辞(プレフィックス)を持つすべてのパラメーターに一致するパターンを指定することが可能です。

2. Asset Descriptionを作成してください。

   UnityのProjectウィンドウで右クリックし、`Create > Avatar Optimizer > Asset Description`を選択してください。\
   ファイルの名前や保存場所は自由です。

3. パラメーターを`Parameters Read By External Tools`および`Parameters Changed By External Tools`に追加してください。

   作成したAsset DescriptionをInspectorで開き、手順1で確認した各パラメーターを対応するリストに追加してください。\
   なお、この仕組みはアバターにギミックが導入されているかどうかを判別できないため、設定内容がプロジェクト内のすべてのアバターに適用されることにご注意ください。

   正規表現を使用する場合は、パラメーターをできるだけ具体的に定義してください。

4. Asset Descriptionをギミックと一緒に配布してください。

   ギミックを導入したユーザーに自動で正しい設定が適用されるように、作成したAsset Descriptionファイルを製品のパッケージに同梱してください。\
   Asset Descriptionをギミックに同梱して配布しても、ギミックがAvatar Optimizerに依存することはありません。ユーザーがAvatar Optimizerをインストールする必要はありません。

Asset Descriptionの詳細については、[Asset Description]のページを参照してください。

[Asset Description]: ../asset-description/
[asset-description-read]: ../asset-description/#parameters-read-by-external-tools

[^fake-usage]: 何らかの意味があるように見えても、実際にはそうではないアニメーション(ダミーのGameObjectを操作するなど)を追加したとしても、将来的にはそのGameObjectやアニメーションが削除される可能性があります。Avatar Optimizerの処理を騙そうとしないでください。

[^missing-asset]: MissingになっているScriptable Objectを正しく処理できないような、実装が不適切なツールがプロジェクトに存在した場合にのみ問題が発生します。私たちの知る限りでは、現時点でそのようなツールは存在していません。互換性に関する問題が発生した場合は報告をお願いします。
