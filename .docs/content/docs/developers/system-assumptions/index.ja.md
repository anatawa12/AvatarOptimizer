---
title: "システムの前提条件"
weight: 50
---

# 外部ツールに求める前提条件

このページは、Avatar Optimizerが外部ツールとデータ構造について行う前提条件を説明しています。Avatar Optimizerと統合するツールを開発している場合は、これらの前提条件を尊重していただけると幸いです。

## Transformインスタンスの使用方法 {#transform-instance-usage}

同じTransformインスタンスは、Avatar Optimizer内で同じボーンを表現するために使用されます。

Avatar Optimizerは、resolving phaseからのTransform行列を使用して、remove mesh in boxで削除される頂点を判断する場合があります。

Transformを操作するツールを作成する場合は、各Transformインスタンスが最適化プロセス全体を通じて単一の一貫したボーンまたは目的を表すことを確認してください。

## 頂点インデックスの使用 {#vertex-index-usage}

頂点インデックスは、ShaderInformationで明示的に登録されていない限り、シェーダーやAvatar Optimizer後に実行される他のプラグインで使用されないものとします。

Avatar Optimizerは最適化中にメッシュをマージし、頂点インデックスを変更する可能性があります。シェーダーやツールで頂点インデックスを使用する場合は、ShaderInformation APIを介してこの使用を登録し、不適切な最適化を防ぐ必要があります。

## 外部によるパラメータの変更 {#external-parameter-modifications}

パラメータは、AssetDescriptionで外部（OSCなど）によって変更されると明示的に宣言されていない限り、外部から変更されないものと仮定します。

Avatar Optimizerは、マークされていない限りパラメータ値が変更されないという前提に基づいて最適化を行います。ツールが実行時にパラメータを変更する場合は、AssetDescriptionを介して外部パラメータとして適切に登録してください。
