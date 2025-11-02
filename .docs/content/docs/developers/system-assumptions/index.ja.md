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

## Transformの前提条件

### Transformインスタンスの一意性
**前提条件**: 同じTransformインスタンスが複数の異なる目的に使用されない。

**場所**: `Editor/Processors/OriginalState.cs:10`

**影響**: OriginalStateは各Transformが1つの使用コンテキストにマッピングされることを前提として、元のTransform行列を保存します。Transformを操作するツールを作成する場合、各Transformが単一の明確な目的を持つことを確認してください。

---

## メッシュ処理の前提条件

### 頂点インデックスの使用
**前提条件**: 頂点インデックスは、ShaderInformationで明示的に使用登録されていない限り、シェーダーやAvatar Optimizer後に実行される他のプラグインで使用されないものとします。

**場所**: `Editor/Processors/TraceAndOptimize/AutoMergeSkinnedMesh.cs:104-108`

**影響**: Avatar Optimizerは最適化中にメッシュをマージし、頂点インデックスを変更する可能性があります。シェーダーやツールで頂点インデックスを使用する場合は、ShaderInformation APIを介してこの使用を登録し、不適切な最適化を防ぐ必要があります。

---

## パラメータ変更の前提条件

### 外部パラメータの変更
**前提条件**: パラメータは、AssetDescriptionで外部（OSCなど）によって変更されると明示的に宣言されていない限り、外部から変更されないものと仮定します。

**場所**: `Editor/AssetDescription.cs:43`

**影響**: Avatar Optimizerは、マークされていない限りパラメータ値が変更されないという前提に基づいて最適化を行います。ツールが実行時にパラメータを変更する場合は、AssetDescriptionを介して外部パラメータとして適切に登録してください。
