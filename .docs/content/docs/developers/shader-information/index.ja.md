---
title: Shader Information API
---

# Shader Information API

Avatar Optimizer v1.8.0以降では、カスタムシェーダーが使用されているマテリアルの最適化を支援するためのShader Information APIを提供しています。\
APIを通じてシェーダーの情報を登録することで、Avatar Optimizerはテクスチャのアトラス化やUVパッキングなどの高度な最適化を実行できるようになります。

## Shader Informationとは？ {#what-is-shader-information}

Shader Informationは、テクスチャ、UVチャンネル、その他のマテリアルプロパティをシェーダーがどのように使用しているかをAvatar Optimizerに伝えるための方法です。

現在のAvatar Optimizerはこの情報を用いて、アバターに以下のような最適化を行います。
(将来的に、さらに多くの最適化が追加される可能性があります。[^optimization-note])\
なお、すべての最適化がTrace and Optimizeで自動的に実行されるとは限りません。

- 複数のテクスチャをアトラス化し、パッキングする (`AAO Merge Material`などのコンポーネントを使用)
- シェーダーの機能としては存在するが、マテリアル設定で無効化されていて使用されていないテクスチャの削除

Shaderの情報がない場合、Avatar Optimizerはシェーダーを慎重に取り扱うため、これらの最適化の一部を実行することができません。

[^optimization-note]: 例えば、現時点ではUVチャンネルの最適化は実装されていませんが、将来のバージョンで追加される可能性はあります。

## コンセプト {#core-concepts}

### 主要なクラス {#main-classes}

Shader Information APIは3つの主要なクラスで構成されています。

- `ShaderInformation`: シェーダーに関する情報を提供するために継承する基底クラス。\
  `GetMaterialInformation`メソッドをオーバーライドして、そのシェーダーを使用するマテリアルがテクスチャとUVをどのように使用するかを登録します。
- `ShaderInformationRegistry`: エディターの初期化処理中に`ShaderInformation`の実装をAvatar Optimizerに[登録](#registration-methods)するために使用する静的クラス。
- `MaterialInformationCallback`: マテリアルプロパティを読み取ってテクスチャとUVの使用方法を伝えるためのメソッドを提供します。これは`GetMaterialInformation`に渡されます。

### Null値 {#null-values}

Shader Information API全体を通して、`null`値は**不明な値**または**アニメーションで操作される(静的に決定することのできない)値**を表します。\
マテリアルプロパティがアニメーションで操作されているなど、ビルド時にその値を決定できない場合、APIはその不確実性を示すために`null`を返します。\
値を静的に決定できない場合は、パラメータに`null`を渡す必要があります。

## 使い方 {#getting-started}

Shader Informationを提供するには、以下の手順に従ってください。

### 1. Assembly Definitionを作成する {#create-asmdef}

シェーダーのパッケージにエディター向けのAssembly Definition[^asmdef]がない場合は、作成してください。\
Shader Informationはビルド時にのみ使用され、Shader Information APIもエディター上でのみ利用可能であるため、エディター向けのアセンブリが必要です。

### 2. Assembly Referenceを追加する {#add-reference}

asmdefファイルのアセンブリ参照に`com.anatawa12.avatar-optimizer.api.editor`を追加してください。

Avatar Optimizerを必須の依存関係にしたくない場合は、`AVATAR_OPTIMIZER`のようなシンボルで[Version Defines]を使用して、Avatar Optimizerがインストールされているか、および、AAOのバージョンが特定のバージョンより新しいかを検出することができます。

![version-defines.png](../version-defines.png)

ここでは、`[1.8,2.0)`のようなバージョン範囲を指定することを推奨します。(v1.8.0以降をサポートしますが、v2.0.0以降には更新が必要になることを意味します)\
一部のAPIは後のバージョンから追加される可能性があるため、使用するAPIに基づいてバージョン範囲を調整しなければならない可能性があることにご注意ください。

### 3. Shader Informationクラスを作成 {#create-class}

`ShaderInformation`を継承するクラスを作成し、`ShaderInformationRegistry`に登録してください。\
Avatar Optimizerがマテリアルを処理する前に登録されることを保証するため、登録には`[InitializeOnLoad]`属性とstaticコンストラクタを使用してください。

```csharp
#if AVATAR_OPTIMIZER && UNITY_EDITOR

using Anatawa12.AvatarOptimizer.API;
using UnityEditor;
using UnityEngine;

namespace YourNamespace
{
    [InitializeOnLoad]
    internal class YourShaderInformation : ShaderInformation
    {
        static YourShaderInformation()
        {
            // シェーダーのGUIDで登録 (シェーダーアセットに推奨)
            string shaderGuid = "your-shader-guid-here";
            ShaderInformationRegistry.RegisterShaderInformationWithGUID(
                shaderGuid, 
                new YourShaderInformation()
            );
        }

        public override ShaderInformationKind SupportedInformationKind =>
            ShaderInformationKind.TextureAndUVUsage;

        public override void GetMaterialInformation(MaterialInformationCallback matInfo)
        {
            // ここでテクスチャとUVの使用状況を登録 (次に示す例を参照)
        }
    }
}

#endif
```

## ShaderInformationKindフラグ {#information-kinds}

`SupportedInformationKind`プロパティは、Shader Informationから提供する情報の種類をAvatar Optimizerに伝えます。

現在は以下の2種類があります。

- `TextureAndUVUsage`: シェーダーが使用するテクスチャや、各テクスチャがサンプリングを行うUVチャンネル、UVの変換行列、サンプラー状態に関する情報を提供していることを示します。\
  詳細は[テクスチャ使用状況の登録](#registering-textures)を参照してください。
- `VertexIndexUsage`: シェーダーが頂点インデックス(例: `SV_VertexID`)を使用するという情報を提供していることを示します。\
  このフラグを提供しない場合、Avatar Optimizerは頂点インデックスは使用されていないものと仮定し、最適化中に頂点を入れ替える可能性があります。\
  詳細は[頂点インデックス使用状況の登録](#register-vertex-index)を参照してください。

これはフラグ用Enum型であるため、`|`演算子で複数の値を組み合わせることができます。

```csharp
public override ShaderInformationKind SupportedInformationKind =>
    ShaderInformationKind.TextureAndUVUsage | ShaderInformationKind.VertexIndexUsage;
```

## マテリアル情報の登録 {#registering-information}

`GetMaterialInformation`メソッドは、シェーダーを使用する各マテリアルに対して呼び出されます。\
`MaterialInformationCallback`を使用して、テクスチャとUVの使用状況を登録してください。

各メソッドの詳細については、APIドキュメントのコメントを参照してください。

### マテリアルプロパティの読み取り {#reading-properties}

`MaterialInformationCallback`は、シェーダー上のマテリアルプロパティを読み取るメソッドを提供します。

```csharp
// floatプロパティの読み取り
float? value = matInfo.GetFloat("_PropertyName");

// intプロパティの読み取り
int? value = matInfo.GetInt("_PropertyName");

// Vector4プロパティの読み取り (_MainTex_STなど)
Vector4? value = matInfo.GetVector("_MainTex_ST");

// シェーダーキーワードが有効かどうかの確認
bool? enabled = matInfo.IsShaderKeywordEnabled("KEYWORD_NAME");
```

これらのメソッドは、プロパティが存在しないか値が不明な場合に`null`を返します。

### テクスチャ使用状況の登録 {#registering-textures}

`RegisterTextureUVUsage`メソッドを使用して、各2Dテクスチャに関する情報をAvatar Optimizerに伝えます。\
パラメータの詳細については、APIドキュメントのコメントを参照してください。

```csharp
public override void GetMaterialInformation(MaterialInformationCallback matInfo)
{
    // UV変換(スケールやオフセット)を取得
    var mainTexST = matInfo.GetVector("_MainTex_ST");
    Matrix2x3? uvMatrix = mainTexST is { } st
        ? Matrix2x3.NewScaleOffset(st)
        : null;

    // テクスチャを登録
    matInfo.RegisterTextureUVUsage(
        textureMaterialPropertyName: "_MainTex",
        samplerState: "_MainTex",  // _MainTexプロパティのサンプラーを使用
        uvChannels: UsingUVChannels.UV0,
        uvMatrix: uvMatrix
    );
}
```

#### サンプラー状態 {#sampler-states}

サンプラー状態はテクスチャのラッピングとフィルタリングを定義します。\
ほとんどのシェーダーはマテリアルプロパティにあるサンプラーを使用するため、その場合はプロパティ名を使用してください。
(文字列は暗黙的に`SamplerStateInformation`に変換されます)

```csharp
matInfo.RegisterTextureUVUsage(
    "_MainTex",
    samplerState: "_MainTex",  // 文字列は暗黙的に変換されます
    UsingUVChannels.UV0,
    uvMatrix
);
```

シェーダーがインラインサンプラー(例: `SamplerState linearClampSampler`)を使用している場合は、`SamplerStateInformation.LinearRepeatSampler`のような定義済みの定数を使用してください。

サンプラーを決定できない場合は、`SamplerStateInformation.Unknown`を使用してください。

#### UVチャンネル {#uv-channels}

`UsingUVChannels`メソッドを使用して、テクスチャがサンプリングを行うUVチャンネルを指定します。\
メッシュUVを使用しないテクスチャ(スクリーンスペース、MatCap、ビュー方向が基準など)の場合は、`UsingUVChannels.NonMesh`を使用してください。

```csharp
matInfo.RegisterTextureUVUsage(
    "_MatCapTexture",
    "_MatCapTexture",
    UsingUVChannels.NonMesh,  // メッシュUVを使用しない
    null  // UV変換なし
);
```

UVチャンネルがマテリアルプロパティに依存する場合は、以下のようにします。

```csharp
var uvChannel = matInfo.GetFloat("_UVChannel") switch
{
    0 => UsingUVChannels.UV0,
    1 => UsingUVChannels.UV1,
    _ => UsingUVChannels.UV0 | UsingUVChannels.UV1  // 不明、どちらにもなる可能性あり
};

matInfo.RegisterTextureUVUsage("_DetailTex", "_DetailTex", uvChannel, uvMatrix);
```

#### UV変換行列 {#uv-transform-matrices}

UV変換行列では、UVがどのようにスケール調整およびオフセットされるかを記述します。(例: `_MainTex_ST`)\
ほとんどのUnityシェーダーは`(scaleX, scaleY, offsetX, offsetY)`のVector4を使用しています。\
スケール調整とオフセットは`Matrix2x3.NewScaleOffset`メソッドを使用して`Matrix2x3`に変換できます。

```csharp
var texST = matInfo.GetVector("_MainTex_ST");
Matrix2x3? uvMatrix = texST is { } st 
    ? Matrix2x3.NewScaleOffset(st)
    : null;
```

必要に応じてこれを手動で構築することもできます。\
UV変換がアニメーションで操作可能であるか、動的に計算されるような場合には、`null`を使用してください。

### 頂点インデックス使用状況の登録 {#register-vertex-index}

シェーダーが頂点インデックス使用状況を登録すると、Avatar Optimizerは元のメッシュから頂点インデックスを保持しようとします。\
これは現在、Trace and Optimizeの自動メッシュ統合を無効にしますが、将来的にはより多くの機能が影響を受ける可能性があります。

この方法は頂点インデックスを保持するためのものです。\
シェーダーがランダムなシーケンスを生成する目的でのみ頂点インデックスを使用している場合は登録する必要がないため、`RegisterVertexIndexUsage`メソッドを呼び出すべきではありません。

```csharp
public override void GetMaterialInformation(MaterialInformationCallback matInfo)
{
    // ... テクスチャを登録 ...

    // 頂点インデックスを使用する機能が有効かチェック
    if (matInfo.GetFloat("_UseVertexIdEffect") != 0)
    {
        // このシェーダーがAvatar Optimizerに頂点インデックスを保持することを望んでいることを伝える
        matInfo.RegisterVertexIndexUsage();
    }
}
```

## Shader Informationの登録 {#registration-methods}

シェーダーと情報を紐付けるには、`ShaderInformation`の実装を登録する必要があります。\
Shader Informationを登録する方法には、以下の2つがあります。

### GUIDで登録する (推奨) {#register-by-guid}

シェーダーアセットの場合、シェーダーのGUIDを使用して登録できます。\
GUIDは通常変更されず、重複もせず、AssetDatabaseへのアクセスを必要としないため、この方法の使用を推奨します。\
(InitializeOnLoadなメソッドでのAssetDatabaseへのアクセスは無効であるため、シェーダーインスタンスによる登録は動作しない可能性があります)

```csharp
ShaderInformationRegistry.RegisterShaderInformationWithGUID(
    "your-shader-asset-guid",
    new YourShaderInformation()
);
```

### シェーダーインスタンスで登録する {#register-by-instance}

ビルド時に動的に作成されるシェーダー、またはシェーダーインスタンスがある場合は、シェーダーインスタンスを登録することができます。

```csharp
Shader shader = Shader.Find("Your/Shader/Name");
ShaderInformationRegistry.RegisterShaderInformation(
    shader,
    new YourShaderInformation()
);
```

## ベストプラクティス {#best-practices}

### InitializeOnLoadを使用する {#use-initializeonload}

`[InitializeOnLoad]`を使用してstaticコンストラクタでShader Informationを登録し、'Apply on Play'用ビルド処理の前に登録してください。

```csharp
[InitializeOnLoad]
internal class YourShaderInformation : ShaderInformation
{
    static YourShaderInformation()
    {
        // Unityがロードされると自動的に登録されます
        Register();
    }
    
    private static void Register()
    {
        ShaderInformationRegistry.RegisterShaderInformationWithGUID(
            "guid", new YourShaderInformation()
        );
    }
}
```

### nullの取り扱い {#handle-unknown-values}

マテリアルプロパティはアニメーションで操作されたり、不明であったりする可能性があります。\
`null`値を想定して処理してください。シェーダー自身はアニメーションでのプロパティ操作をサポートしていない場合でも、Avatar Optimizerが一度に複数のマテリアルを処理することにより`null`が渡される可能性があります。

```csharp
// パターンマッチングを使用
var st = matInfo.GetVector("_MainTex_ST");
Matrix2x3? uvMatrix = st is { } st2 ? Matrix2x3.NewScaleOffset(st2) : null;

// nullの場合も考慮してUVチャンネルを決定
var uvChannel = matInfo.GetFloat("_UVChannel") switch
{
    0 => UsingUVChannels.UV0,
    1 => UsingUVChannels.UV1,
    null => UsingUVChannels.UV0 | UsingUVChannels.UV1,  // 不明
    _ => UsingUVChannels.UV0 | UsingUVChannels.UV1
};
```

### キーワードとプロパティを確認する {#check-keywords-properties}

実際に使用されているテクスチャのみを登録してください。

```csharp
if (matInfo.IsShaderKeywordEnabled("_NORMALMAP") != false)
{
    // キーワードが有効な可能性、法線マップを登録
}

if (matInfo.GetFloat("_UseEmission") != 0)
{
    // エミッションが有効、エミッションマップを登録
}
```

<blockquote class="book-hint info">

`!= false`は、値が`true`または`null`(不明)かどうかをチェックするのに用います。\
この保守的なアプローチで、不明な場合に機能が有効であると仮定することができます。

</blockquote>

### 正確な情報を提供する {#provide-accurate-information}

- 頂点インデックスが本当に重要な場合にのみ`VertexIndexUsage`を設定する
- 正しいサンプラー状態を使用する (アトラス化のテクスチャフィルタリングに影響します)
- UV行列が動的に変化するか、アニメーションで操作される場合は`null`に設定
- スクリーンスペースのUVには`UsingUVChannels.NonMesh`を使用する

### Shader Informationクラスに`internal class`を使用する {#use-internal-class}

Shader Informationクラスをアセンブリの公開APIとして晒してしまわないために、`internal class` として宣言することを推奨します。\
これによりコードベースがクリーンになり、内部の実装詳細が誤って外部から使用されるのを防ぐことができます。

エディタ用スクリプト側に公開APIがない場合は、asmdefファイルのAuto Referenceをfalseに設定することで、クラスが`Assembly-CSharp`に対して使用可能になるのを防ぐことができます。

## 記述例 {#examples}

シンプルなシェーダーのためのShaderInformationの記述例です。

より複雑な例については、[GitHub][shader-information-impl]にあるAvatar Optimizerの組み込みShaderInformation実装を参照してください。

[shader-information-impl]: https://github.com/anatawa12/AvatarOptimizer/tree/master/Editor/APIInternal/

### メインテクスチャを持つシンプルなシェーダー {#example-simple}

```csharp
[InitializeOnLoad]
internal class SimpleShaderInformation : ShaderInformation
{
    static SimpleShaderInformation()
    {
        ShaderInformationRegistry.RegisterShaderInformationWithGUID(
            "your-shader-guid",
            new SimpleShaderInformation()
        );
    }

    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? uvMatrix = mainTexST is { } st 
            ? Matrix2x3.NewScaleOffset(st) 
            : null;

        matInfo.RegisterTextureUVUsage(
            "_MainTex",
            "_MainTex",
            UsingUVChannels.UV0,
            uvMatrix
        );
    }
}
```

### 条件付きで機能が有効になるシェーダー {#example-conditional}

```csharp
[InitializeOnLoad]
internal class FeatureShaderInformation : ShaderInformation
{
    static FeatureShaderInformation()
    {
        ShaderInformationRegistry.RegisterShaderInformationWithGUID(
            "your-shader-guid",
            new FeatureShaderInformation()
        );
    }

    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        // メインテクスチャ (常に存在)
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? mainUVMatrix = mainTexST is { } st 
            ? Matrix2x3.NewScaleOffset(st) 
            : null;

        matInfo.RegisterTextureUVUsage(
            "_MainTex", "_MainTex", UsingUVChannels.UV0, mainUVMatrix
        );

        // 法線マップ (キーワードに依存)
        if (matInfo.IsShaderKeywordEnabled("_NORMALMAP") != false)
        {
            matInfo.RegisterTextureUVUsage(
                "_BumpMap", "_BumpMap", UsingUVChannels.UV0, mainUVMatrix
            );
        }

        // 詳細テクスチャ (プロパティに依存)
        if (matInfo.GetFloat("_UseDetail") != 0)
        {
            var detailST = matInfo.GetVector("_DetailTex_ST");
            Matrix2x3? detailUVMatrix = detailST is { } st2 
                ? Matrix2x3.NewScaleOffset(st2) 
                : null;

            var detailUV = matInfo.GetFloat("_DetailUV") switch
            {
                0 => UsingUVChannels.UV0,
                1 => UsingUVChannels.UV1,
                _ => UsingUVChannels.UV0 | UsingUVChannels.UV1
            };

            matInfo.RegisterTextureUVUsage(
                "_DetailTex", "_DetailTex", detailUV, detailUVMatrix
            );
        }

        // MatCap (スクリーンスペース、UV変換なし)
        if (matInfo.IsShaderKeywordEnabled("_MATCAP") != false)
        {
            matInfo.RegisterTextureUVUsage(
                "_MatCap",
                SamplerStateInformation.LinearClampSampler,
                UsingUVChannels.NonMesh,
                null
            );
        }
    }
}
```

### 頂点インデックスを使用するシェーダー {#example-vertex-indices}

```csharp
[InitializeOnLoad]
internal class VertexShaderInformation : ShaderInformation
{
    static VertexShaderInformation()
    {
        ShaderInformationRegistry.RegisterShaderInformationWithGUID(
            "your-shader-guid",
            new VertexShaderInformation()
        );
    }

    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.TextureAndUVUsage | ShaderInformationKind.VertexIndexUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? uvMatrix = mainTexST is { } st 
            ? Matrix2x3.NewScaleOffset(st) 
            : null;

        matInfo.RegisterTextureUVUsage(
            "_MainTex", "_MainTex", UsingUVChannels.UV0, uvMatrix
        );

        // シェーダーはエフェクトのためにSV_VertexIDを使用
        matInfo.RegisterVertexIndexUsage();
    }
}
```

## サポート {#support}

質問やサポートが必要な場合は、以下をご利用ください。

- **Discord**: [NDMF Discord] `@anatawa12`にメンション
- **Fediverse**: [@anatawa12@misskey.niri.la][fediverse]
- **GitHub Issues**: [AvatarOptimizer Issues]

[Version Defines]: https://docs.unity3d.com/2019.4/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html#define-symbols
[NDMF Discord]: https://discord.gg/dV4cVpewmM
[fediverse]: https://misskey.niri.la/@anatawa12
[AvatarOptimizer Issues]: https://github.com/anatawa12/AvatarOptimizer/issues

[^asmdef]: Assembly-CSharp以外のアセンブリを定義するためのファイル。[unity docs](https://docs.unity3d.com/2022.3/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html)を参照してください。
