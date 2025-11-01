---
title: "システムの前提条件"
weight: 50
---

# システムの前提条件と制約

Avatar Optimizerは、システムの動作、プラットフォームの機能、データ構造に関するさまざまな前提条件に依存しています。これらの前提条件を理解することは、以下の点で重要です:

- 互換性のある外部ツールの開発
- Avatar Optimizerへの貢献
- 最適化動作の理解
- 問題のデバッグ

## ドキュメントの場所

システムの前提条件の包括的なリストは、リポジトリのルートで管理されています:

**[ASSUMPTIONS.md](https://github.com/anatawa12/AvatarOptimizer/blob/master/ASSUMPTIONS.md)**

このドキュメントは、コードベース全体の明示的および暗黙的な前提条件をカテゴリ別に整理しています:

- **プラットフォームと環境**: Unityバージョン、VRChat SDK、NDMF依存関係
- **フレームワーク統合**: NDMFビルドフェーズ、実行順序
- **データ構造**: Transformの一意性、不変性制約、インスタンスIDの安定性
- **APIコントラクト**: コンポーネント登録、シェーダー情報、メッシュプロバイダー要件
- **VRChat固有**: PhysBoneの動作、パラメータドライバー、マルチパスレンダリング
- **アニメーターシステム**: ステートマシンの制限、遷移要件
- **メッシュ処理**: 頂点インデックス、テクスチャ処理、ブレンドシェイプ
- **スレッドモデル**: シングルスレッド実行の前提

## 外部ツール開発者向け

Avatar Optimizerと統合するツールを開発している場合は、ASSUMPTIONS.mdドキュメントを確認して以下を理解してください:

1. **API要件**: ComponentInformationまたはShaderInformation実装が満たすべき条件
2. **ビルド時の動作**: Avatar OptimizerがNDMFパイプラインでいつどのように実行されるか
3. **データ制約**: AAOがメッシュデータ、コンポーネント、Transformについて行う前提条件
4. **VRChat依存関係**: プラットフォーム固有の最適化と動作

## コントリビューター向け

Avatar Optimizerに貢献する際は:

1. コードを変更する前に**関連する前提条件を確認**
2. **新しい前提条件をドキュメント化**し、ASSUMPTIONS.mdに明確な場所参照を含める
3. **制約を検証**して、変更がドキュメント化された前提条件に違反しないことを確認
4. 前提条件が変更された場合は**ドキュメントを更新**

## 前提条件の例

ドキュメントからのいくつかの重要な例:

### Transformインスタンスの一意性
> 同じTransformインスタンスが複数の異なる目的に使用されない
> 
> *場所: `Editor/Processors/OriginalState.cs:10`*

これは、OriginalStateが各Transformが正確に1つの使用コンテキストにマッピングされることを前提として、元のTransform行列を保存することを意味します。

### ComponentInformation登録
> クラスは`ComponentInformation<TComponent>`を継承し、パラメータなしのコンストラクタを持つ必要があります
>
> *場所: `API-Editor/ComponentInformation.cs:13-18`*

外部コンポーネント用のComponentInformationを実装する場合、これらの要件を満たす必要があります。

### BlendShapeフレームの順序
> BlendShapeフレームは重みでソートされている必要があります
>
> *場所: `Internal/MeshInfo2/MeshInfo2.cs:1426`*

コードは、正しいブレンドシェイプ補間のためにこの順序に依存しています。

## 最新情報の確認

ASSUMPTIONS.mdドキュメントは、コードベースの進化に伴って更新されます。常にリポジトリの最新バージョンを参照してください:

https://github.com/anatawa12/AvatarOptimizer/blob/master/ASSUMPTIONS.md

特定の前提条件に関する質問や、欠落している前提条件を報告するには、GitHubでIssueを開いてください。
