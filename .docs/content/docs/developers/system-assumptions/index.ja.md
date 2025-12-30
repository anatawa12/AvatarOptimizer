---
title: "システムの前提条件"
weight: 50
---

# 外部ツールに求める前提条件

このページでは、Avatar Optimizerが外部ツールやデータ構造に対して仮定している条件について説明します。\
Avatar Optimizerと連携するツールを開発している場合は、これらの条件を尊重していただけると幸いです。

## Transformインスタンスの使用方法 {#transform-instance-usage}

Transformインスタンスが同一であるか否かは、Avatar Optimizer内で同じボーンであるか否かを判定するために使用されます。

Avatar Optimizerは、Resolving Phaseで取得したTransform行列を使用して、Remove Mesh By Boxにより削除対象となる頂点を判断する場合があります。

Transformを操作するツールを作成する場合は、Avatar Optimizerの最適化処理内において各Transformインスタンスが一貫して単一のボーンや目的を表すことを確認してください。

## 頂点インデックスの使用 {#vertex-index-usage}

ShaderInformationで明示的に登録されていない限り、頂点インデックスは、シェーダーやAvatar Optimizerの後に実行される他のプラグインなどにおいて使用されないものと仮定して扱われます。

Avatar Optimizerは、その最適化処理においてメッシュを統合し、頂点インデックスを変更する可能性があります。\
シェーダーやツールで頂点インデックスを使用する場合は、[ShaderInformation API][ShaderInformation]を介してその利用状況を登録し、不適切な最適化処理を防止する必要があります。

[ShaderInformation]: ../shader-information

## 外部ツールによるAnimatorパラメーターの使用 {#external-parameter-usage}

Avatar Optimizerは、Animatorパラメーターの使用方法やその値の範囲を用いて、不要なアニメーションなどを最適化することがあります。\
しかし、外部ツールによって使用されるパラメーターについては、アバターのデータから適切に推測することが不可能であるため、AssetDescriptionで明示的に宣言されていない限り、外部からはパラメーターが使用されていないものと仮定して扱われます。

ツールがOSCなどでパラメーターを変更したり読み取ったりする場合は、[AssetDescription]を介して外部ツールから使用されるパラメーターとして登録してください。

[AssetDescription]: ../asset-description
