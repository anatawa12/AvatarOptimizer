---
title: Asset Description
---

# Asset Description

Asset DescriptionはAvatar Optimizerにアセットの情報を提供するためのファイルです。

## なぜAsset Descriptionが必要なのか {#why-asset-description-is-needed}

アバター上の不要な要素を削除するために、Avatar Optimizerはアバターに存在するすべてのコンポーネントのことを知る必要があります。\
Avatar Optimizer v1.6.0で[コンポーネントにAAOとの互換性をもたせるためのドキュメント][make-component-compatible]とAPIが追加されましたが、
非破壊ツールでなく、ビルド時に処理を行わないようなツールでは、`IVRCSDKPreprocessAvatarCallback`でコンポーネントを削除するのは少し面倒だろうと考えました。\
そのため、Avatar Optimizerに無視してほしい、ビルド時やランタイムで処理を行わないコンポーネントを指定するためのシンプルな仕組みとして、Asset Descriptionがv1.7.0で追加されました。

なお、非破壊ツールの場合については、正しくない実行順で処理が行われた場合に、Avatar Optimizerがコンポーネントを誤って削除してしまわないように、従来通り`IVRCSDKPreprocessAvatarCallback`でコンポーネントを削除することを推奨します。

[make-component-compatible]: ../make-your-components-compatible-with-aao

## Asset Descriptionの作成 {#create-asset-description}

Asset Descriptionを作成するには、Projectウィンドウの右クリックメニューから`Create/Avatar Optimizer/Asset Description`を選択してください。\
Avatar Optimizerはすべてのファイルの中からファイル検索を行うため、Asset Descriptionの名前、場所は自由です。

## Asset Descriptionの編集 {#edit-asset-description}

![asset-description-inspector](asset-description-inspector.png)

### Comment {#comment}

コメント欄はメモを書くためにご自由にお使いください。
Avatar Optimizerはコメントを無視します。

### Meaningless Components {#meaningless-components}

Meaningless ComponentsはAvatar Optimizerに無視してほしいコンポーネントの型の一覧です。
コンポーネントのScript Assetを指定してください。
指定されたScript Assetの型のコンポーネントと、そのサブクラスのコンポーネントはAvatar Optimizerに無視されます。

Asset Descriptionでは実際のScene上のコンポーネントと同様に、Script AssetがguidとfileIDの形で保持されています。
そのため、クラス名を変更したとしても、シーン上のコンポーネントが壊れていない限り、Asset Descriptionでの指定も問題なく機能します。
