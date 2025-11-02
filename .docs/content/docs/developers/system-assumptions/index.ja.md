---
title: "システムの前提条件"
weight: 50
---

# 外部ツールに求める前提条件

## Transformインスタンスの一意性 {#transform-instance-uniqueness}

同じTransformインスタンスが複数の異なる目的に使用されない。たとえば、Transformはremove mesh in boxの計算に使用されます。

Transformを操作するツールを作成する場合、各Transformが単一の明確な目的を持つことを確認してください。

## 頂点インデックスの使用 {#vertex-index-usage}

頂点インデックスは、ShaderInformationで明示的に使用登録されていない限り、シェーダーやAvatar Optimizer後に実行される他のプラグインで使用されないものとします。

Avatar Optimizerは最適化中にメッシュをマージし、頂点インデックスを変更する可能性があります。シェーダーやツールで頂点インデックスを使用する場合は、ShaderInformation APIを介してこの使用を登録し、不適切な最適化を防ぐ必要があります。

## 外部パラメータの変更 {#external-parameter-modifications}

パラメータは、AssetDescriptionで外部（OSCなど）によって変更されると明示的に宣言されていない限り、外部から変更されないものと仮定します。

Avatar Optimizerは、マークされていない限りパラメータ値が変更されないという前提に基づいて最適化を行います。ツールが実行時にパラメータを変更する場合は、AssetDescriptionを介して外部パラメータとして適切に登録してください。
