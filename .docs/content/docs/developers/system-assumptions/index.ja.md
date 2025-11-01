---
title: "システムの前提条件"
weight: 50
---

# 外部ツール開発者向けシステムの前提条件

このドキュメントは、Avatar Optimizerがデータ構造、コンポーネント、動作について行う前提条件を列挙しています。Avatar Optimizerと統合するツールを開発している場合、互換性を確保するためにこれらの前提条件を理解する必要があります。

## TransformとGameObjectの前提条件

### Transformインスタンスの一意性
**前提条件**: 同じTransformインスタンスが複数の異なる目的に使用されない。

**場所**: `Editor/Processors/OriginalState.cs:10`

**影響**: OriginalStateは各Transformが1つの使用コンテキストにマッピングされることを前提として、元のTransform行列を保存します。Transformを操作するツールを作成する場合、各Transformが単一の明確な目的を持つことを確認してください。

### コンポーネントインスタンスIDの安定性
**前提条件**: UnityのインスタンスIDはビルドプロセス中に安定している。

**場所**: `Editor/ObjectMapping/ObjectMapping.cs:24-48`

**影響**: インスタンスIDはオブジェクトマッピング全体で辞書のキーとして使用されます。外部ツールは、ビルドプロセス中にUnityがインスタンスIDを再割り当てする原因となってはいけません。

## APIコントラクトの要件

### ComponentInformation登録

**要件**:
- クラスは`ComponentInformation<TComponent>`を継承する**必要があります**
- クラスはパラメータなしのコンストラクタを持つ**必要があります**
- 型パラメータは`TargetType`から割り当て可能である**必要があります**
- 各コンポーネント型に対して**1つ**のComponentInformationのみ存在できます
- 外部コンポーネント用のComponentInformationを宣言**すべきではありません**（競合を避けるため）

**場所**: `API-Editor/ComponentInformation.cs:13-21`

**メソッド呼び出し制約**: `AddDependency`を別の時に呼び出した後、`IComponentDependencyInfo`のメソッドを呼び出しては**いけません**。前の呼び出しコンテキストが無効になるためです。

**場所**: `API-Editor/ComponentInformation.cs:214`

### ShaderInformation登録

**登録タイミング**: シェーダー情報は`InitializeOnLoad`、`RuntimeInitializeOnLoadMethod`、またはシェーダーコンストラクタで登録すべきです。

**場所**: `API-Editor/ShaderInformation.cs:15`

**アセットベースのシェーダー**: アセットとして保存されているシェーダーの場合、`RegisterShaderInformation`ではなく`RegisterShaderInformationWithGUID`を使用してください。`InitializeOnLoadAttribute`時にアセットをロードできない可能性があるためです。

**場所**: `API-Editor/ShaderInformation.cs:64`

**デフォルトの前提**: シェーダー情報を提供しない場合、Avatar Optimizerは頂点インデックスがシェーダーによって使用**されていない**と仮定します。

**場所**: `API-Editor/ShaderInformation.cs:184`

**UV Tile Discard**: シェーダーがUV座標の整数部分のみを使用する場合（UV Tile Discardなど）、UV使用を「その他のUV使用」として登録すべきではありません。

**場所**: `API-Editor/ShaderInformation.cs:250`

### MeshRemovalProviderの制約

**メッシュデータの不変性**: MeshRemovalProviderを作成した後、`UVUsageCompabilityAPI`で登録された退避UV チャンネルを除いて、メッシュデータを変更すべきではありません。

**場所**: `API-Editor/MeshRemovalProvider.cs:18`

**廃棄要件**: MeshRemovalProviderが不要になったら`Dispose()`を呼び出すべきです。

**場所**: `API-Editor/MeshRemovalProvider.cs:68`

### UVUsageCompabilityAPIの使用制限

**ビルド時のみ**: このAPIはビルド時の非破壊ツール（`IVRCSDKPreprocessAvatar`内）での使用を意図しています。インプレース編集モードツールから使用しては**いけません**。

**場所**: `API-Editor/UVUsageCompabilityAPI.cs:18-19`

**UVチャンネルの範囲**: UVチャンネルパラメータは0-7（包含）である必要があります。

**場所**: `API-Editor/UVUsageCompabilityAPI.cs:56`

## メッシュとBlendShapeの前提条件

### BlendShapeフレームの順序
**前提条件**: BlendShapeフレームは重みでソートされている必要があります。

**場所**: `Internal/MeshInfo2/MeshInfo2.cs:1426`

**影響**: コードは正しいブレンドシェイプ補間のためにこの順序に依存しています。ブレンドシェイプを作成または変更するツールの場合、フレームが適切にソートされていることを確認してください。

### BlendShapeBufferの不変性
**前提条件**: BlendShapeBufferはブレンドシェイプの削除を除いて一般的に不変です。

**場所**: `Internal/MeshInfo2/MeshInfo2.cs:1292`

**理由**: データを追加するには新しい配列を作成する必要があり、これはコストがかかります。

### 頂点インデックスの不安定性に関する警告
**警告**: Avatar Optimizerがメッシュをマージすると、頂点インデックスが変更される可能性があります。Avatar Optimizerをアップグレードすると、特定の頂点インデックスに依存するアバターが壊れる可能性があります。

**場所**: `Editor/Processors/TraceAndOptimize/AutoMergeSkinnedMesh.cs:104-108`

**影響**: ユーザーとツールは、最適化を通じて頂点インデックス値が安定したままであることに依存してはいけません。

## コンポーネント依存関係のセマンティクス

### 常に必要な依存関係
`IComponentDependencyInfo.AlwaysRequired()`でマークされた依存関係は、依存コンポーネントが無効になっている場合でも必要です。

**場所**: `API-Editor/ComponentInformation.cs:312`

### 条件付き依存関係
`IComponentDependencyInfo.AsSerializedReference()`でマークされた依存関係は、依存コンポーネントが無効になっている場合は必要ありません。

**場所**: `API-Editor/ComponentInformation.cs:334`

### 依存関係の前提
コンポーネントが有効にでき、依存関係が存在する場合、依存関係は必要と見なされます。

**場所**: `API-Editor/ComponentInformation.cs:210`

## 後方互換性の要件

### 新しい抽象メソッドの禁止
パブリックAPIクラス（`ComponentInformation`、`ShaderInformation`）に抽象メソッドを追加しては**いけません**。既存の外部実装が壊れるためです。

**場所**: 
- `API-Editor/ComponentInformation.cs:102`
- `API-Editor/ShaderInformation.cs:159`

## コンポーネントのプロパティマッピング

### TryMapPropertyの使用
コンポーネントに密接に関連するコンポーネント上のプロパティについては、`TryMapProperty`を使用してオブジェクト置換時にプロパティを適切にマッピングします。

**場所**: `API-Editor/ComponentInformation.cs:417`

**要件**: `CollectMutations`で`ModifyProperties`を呼び出して、プロパティを変更されたプロパティとして登録する必要があります。

**場所**: `API-Editor/ComponentInformation.cs:428`

## 外部パラメータとツール統合

### AssetDescription外部パラメータ
**前提条件**: Avatar Optimizerは、AssetDescriptionでマークされたパラメータが外部ツールによって変更される可能性があると仮定します。

**場所**: `Editor/AssetDescription.cs:43`

**影響**: これは特定のプロパティの最適化決定に影響します。ツールがパラメータを変更する場合、それらが外部パラメータとして適切に登録されていることを確認してください。

## 注記

内部実装の詳細を含むすべてのシステム前提条件の完全なリストについては、リポジトリの[ASSUMPTIONS.md](https://github.com/anatawa12/AvatarOptimizer/blob/master/ASSUMPTIONS.md)を参照してください。
