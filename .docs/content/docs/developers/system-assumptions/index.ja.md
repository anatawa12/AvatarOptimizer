---
title: "システムの前提条件"
weight: 50
---

# 外部ツール開発者向けシステムの前提条件

このドキュメントは、Avatar Optimizerがデータ構造と動作について行う主要な前提条件を列挙しています。Avatar Optimizerと統合するツールを開発している場合、互換性を確保するためにこれらの前提条件を理解してください。

**注記**: APIコントラクト要件（ComponentInformation、ShaderInformationなど）については、APIソースファイルのdocコメントを参照してください:
- `API-Editor/ComponentInformation.cs`
- `API-Editor/ShaderInformation.cs`
- `API-Editor/MeshRemovalProvider.cs`
- `API-Editor/UVUsageCompabilityAPI.cs`

---

## TransformとGameObjectの前提条件

### Transformインスタンスの一意性
**前提条件**: 同じTransformインスタンスが複数の異なる目的に使用されない。

**場所**: `Editor/Processors/OriginalState.cs:10`

**影響**: OriginalStateは各Transformが1つの使用コンテキストにマッピングされることを前提として、元のTransform行列を保存します。Transformを操作するツールを作成する場合、各Transformが単一の明確な目的を持つことを確認してください。

### コンポーネントインスタンスIDの安定性
**前提条件**: UnityのインスタンスIDはビルドプロセス中に安定している。

**場所**: `Editor/ObjectMapping/ObjectMapping.cs:24-48`

**影響**: インスタンスIDはオブジェクトマッピング全体で辞書のキーとして使用されます。外部ツールは、ビルドプロセス中にUnityがインスタンスIDを再割り当てする原因となってはいけません。

---

## メッシュとBlendShapeの前提条件

### BlendShapeフレームの順序
**前提条件**: BlendShapeフレームは重みでソートされている必要があります。

**場所**: `Internal/MeshInfo2/MeshInfo2.cs:1426`

**影響**: コードは正しいブレンドシェイプ補間のためにこの順序に依存しています。ブレンドシェイプを作成または変更するツールの場合、フレームが適切にソートされていることを確認してください。

### 頂点インデックスの不安定性に関する警告
**警告**: Avatar Optimizerがメッシュをマージすると、頂点インデックスが変更される可能性があります。Avatar Optimizerをアップグレードすると、特定の頂点インデックスに依存するアバターが壊れる可能性があります。

**場所**: `Editor/Processors/TraceAndOptimize/AutoMergeSkinnedMesh.cs:104-108`

**影響**: ユーザーとツールは、最適化を通じて頂点インデックス値が安定したままであることに依存してはいけません。

---

## 外部パラメータ

### AssetDescription外部パラメータ
**前提条件**: Avatar Optimizerは、AssetDescriptionでマークされたパラメータが外部ツールによって変更される可能性があると仮定します。

**場所**: `Editor/AssetDescription.cs:43`

**影響**: これは特定のプロパティの最適化決定に影響します。ツールがパラメータを変更する場合、それらが外部パラメータとして適切に登録されていることを確認してください。
