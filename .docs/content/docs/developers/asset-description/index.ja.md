---
title: Asset Description
---

# Asset Description

Asset DescriptionはAvatar Optimizerにアセットの情報を提供するためのファイルです。

## Asset Descriptionはなぜ必要なのか

Avatar Optimizerはアバターのいらない要素削除するため、アバターにあるすべてのコンポーネントについての知識が必要があります。\
Avatar Optimizer v1.6.0で[コンポーネントの互換性を持たせるためのドキュメント][make-component-compatible]とAPIが追加されましたが、
非破壊ツールではなく、ビルド時に処理を行わないツールにとってはコンポーネントを `IVRCSDKPreprocessAvatarCallback`で削除するのは少し煩雑であろうと考えました。\
そのため、ビルド時に意味のない、Avatar Optimizerに無視してほしいコンポーネントを指定するためのシンプルな仕組みとしてAsset Descriptionが1.7.0で追加されました。

非破壊ツールについては実行順が正しくなかったときにAvatarOptimizerによってコンポーネントを誤って削除されてしまうのを防ぐため、今まで通り`IVRCSDKPreprocessAvatarCallback`で削除することを推奨します。

[make-component-compatible]: ../make-your-components-compatible-with-aao

## Asset Descriptionの作成 {#create-asset-description}

Asset Descriptionを作成するには Project ウィンドウの右クリックメニューから `Create/Avatar Optimizer/Asset Description` を選択してください。\
Avatar Optimizerはすべてのファイルから検索するため、Asset Descriptionの名前、場所は自由です。

## Asset Descriptionの編集 {#edit-asset-description}

![asset-description-inspector](asset-description-inspector.png)

### Comment {#comment}

コメント欄はメモを書くためにご自由にお使いください。
Avatar Optimizerはコメントを無視します。

### Meaningless Components {#meaningless-components}

Meaningless ComponentsにはAvatar Optimizerに無視してほしいコンポーネントの型を列挙するものです。
コンポーネントのScript Assetを指定してください。
指定されたScript Assetの型もしくはそのサブクラスのコンポーネントはAvatar Optimizerに無視されます。

Asset Descriptionでは実際のScene上のコンポーネントと同様に、Script AssetのguidとfileIDの形で保持されています。
そのため、クラス名を変更してもシーン上のコンポーネントが壊れない限りはAsset Descriptionでの指定も問題なく機能します。
