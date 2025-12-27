---
title: Shader Information API
---

# Shader Information API

Avatar Optimizer v1.8.0以降は、カスタムシェーダーを使用するマテリアルの最適化を支援するShader Information APIを提供しています。シェーダー情報を登録することで、Avatar Optimizerがテクスチャのアトラス化やUVのパッキングなどの高度な最適化を実行できるようになります。

## Shader Informationとは？ {#what-is-shader-information}

Shader Informationは、あなたのシェーダーがテクスチャ、UVチャンネル、その他のマテリアルプロパティをどのように使用しているかをAvatar Optimizerに伝える方法です。

現在のAvatar Optimizerはこの情報を用いて、以下の方法でアバターを最適化します。将来的にさらに多くの最適化が追加される可能性があります。[^optimization-note] ただし、すべての最適化が Trace and Optimize によって自動的に実行されるわけではありません。

- 複数のテクスチャをテクスチャアトラスにパッキング (`AAO Merge Material`などのコンポーネントを使用)
- シェーダー機能で使用されているがマテリアル設定で無効化されているテクスチャを削除

Shader Informationがない場合、Avatar Optimizerはシェーダーを保守的に扱い、これらの最適化の一部を実行できません。

[^optimization-note]: 例えば、UVチャンネル最適化は現在実装されていませんが、将来のバージョンで追加される可能性があります。

## コアコンセプト {#core-concepts}

### 主要なクラス {#main-classes}

Shader Information APIは3つの主要なクラスで構成されています:

- `ShaderInformation`: シェーダーに関する情報を提供するために継承する基底クラス。`GetMaterialInformation`をオーバーライドして、シェーダーを使用するマテリアルのテクスチャとUV使用状況を登録します。
- `ShaderInformationRegistry`: エディタ初期化中に`ShaderInformation`実装をAvatar Optimizerに[登録](#registration-methods)するために使用する静的クラス。
- `MaterialInformationCallback`: `GetMaterialInformation`に渡され、マテリアルプロパティを読み取り、テクスチャ/UV使用情報を登録するメソッドを提供します。

### Null値 {#null-values}

Shader Information API全体を通して、`null`値には一貫した意味があります: **不明な値**または**アニメーション化された(静的に決定不可能な)値**を表します。マテリアルプロパティがアニメーション化されている可能性があるか、ビルド時にその値を決定できない場合、APIは不確実性を示すために`null`を返します。値を静的に決定できない場合は、パラメータに`null`を渡す必要があります。

## はじめに {#getting-started}

シェーダーのShader Informationを提供するには、以下の手順に従ってください:

### 1. Assembly Definitionを作成 {#create-asmdef}

シェーダーパッケージにエディタ用のAssembly Definition（asmdef）がない場合は、作成してください。 Shader Informationはビルド時にのみ使用され、Shader Information APIはエディタでのみ利用可能なため、該当するアセンブリはエディタ専用にしてください。

### 2. Assembly Referenceを追加 {#add-reference}

assembly definitionの参照に`com.anatawa12.avatar-optimizer.api.editor`を追加してください。

Avatar Optimizerを必須の依存関係にしたくない場合は、`AVATAR_OPTIMIZER`のようなシンボルで[Version Defines]を使用して、Avatar Optimizerがインストールされているか、AAOバージョンが指定されたバージョンより新しいかを検出してください。

![version-defines.png](../version-defines.png)

`[1.8,2.0)`のようなバージョン範囲を推奨します (v1.8.0以降をサポートしますが、v2.0.0では更新が必要になります)。一部のAPIは後のバージョンで追加された可能性があるため、使用するAPIに基づいてバージョン範囲を調整する必要がある場合があることに注意してください。

### 3. Shader Informationクラスを作成 {#create-class}

`ShaderInformation`を継承するクラスを作成し、`ShaderInformationRegistry`に登録してください。
Avatar Optimizerがマテリアルを処理する前に登録されることを保証するため、登録は`[InitializeOnLoad]`属性とstaticコンストラクタを使って行ってください:

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
            // シェーダーGUIDで登録 (シェーダーアセットに推奨)
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
            // ここでテクスチャとUV使用状況を登録 (以下の例を参照)
        }
    }
}

#endif
```

## ShaderInformationKindフラグ {#information-kinds}

`SupportedInformationKind`プロパティは、提供している情報をAvatar Optimizerに伝えます。

現在、2つの情報種類があります:

- `TextureAndUVUsage`: シェーダーが使用するテクスチャ、各テクスチャがサンプリングするUVチャンネル、UV変換マトリックス、サンプラーステートに関する情報を提供していることを示します。[テクスチャ使用状況の登録](#registering-textures)を参照してください。
- `VertexIndexUsage`: シェーダーが頂点インデックス (例: `SV_VertexID`) を使用していることを示します。このフラグを提供しない場合、Avatar Optimizerは頂点インデックスが使用されていないと仮定し、最適化中に頂点をシャッフルする可能性があります。[頂点インデックス使用状況の登録](#register-vertex-index)を参照してください。

これはフラグ列挙型なので、`|`演算子で複数の値を組み合わせることができます。
```csharp
public override ShaderInformationKind SupportedInformationKind =>
    ShaderInformationKind.TextureAndUVUsage | ShaderInformationKind.VertexIndexUsage;
```

## マテリアル情報の登録 {#registering-information}

`GetMaterialInformation`メソッドは、シェーダーを使用する各マテリアルに対して呼び出されます。
`MaterialInformationCallback`を使用してテクスチャとUV使用状況を登録してください。

各メソッドの詳細については、APIドキュメントコメントを参照してください。

### マテリアルプロパティの読み取り {#reading-properties}

コールバックは、シェーダー上のマテリアルプロパティを読み取るメソッドを提供します:

```csharp
// floatプロパティを読み取り
float? value = matInfo.GetFloat("_PropertyName");

// intプロパティを読み取り
int? value = matInfo.GetInt("_PropertyName");

// Vector4プロパティを読み取り (_MainTex_STなど)
Vector4? value = matInfo.GetVector("_MainTex_ST");

// シェーダーキーワードが有効かどうかをチェック
bool? enabled = matInfo.IsShaderKeywordEnabled("KEYWORD_NAME");
```

これらのメソッドは、プロパティが存在しないか値が不明な場合に`null`を返します。

### テクスチャ使用状況の登録 {#registering-textures}

`RegisterTextureUVUsage`を使用して、各2Dテクスチャについて Avatar Optimizerに伝えます。パラメータの詳細については、APIドキュメントコメントを参照してください。

```csharp
public override void GetMaterialInformation(MaterialInformationCallback matInfo)
{
    // UV変換 (スケール/オフセット) を取得
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

#### サンプラーステート {#sampler-states}

サンプラーステートはテクスチャのラッピングとフィルタリングを定義します。ほとんどのシェーダーはマテリアルプロパティのサンプラーを使用します - プロパティ名を使用してください (文字列は暗黙的に`SamplerStateInformation`に変換されます):

```csharp
matInfo.RegisterTextureUVUsage(
    "_MainTex",
    samplerState: "_MainTex",  // 文字列は暗黙的に変換されます
    UsingUVChannels.UV0,
    uvMatrix
);
```

シェーダーがインラインサンプラーを使用している場合 (例: `SamplerState linearClampSampler`)、`SamplerStateInformation.LinearRepeatSampler`のような定義済みの定数を使用してください。

サンプラーを判定できない場合は、`SamplerStateInformation.Unknown`を使用してください。

#### UVチャンネル {#uv-channels}

`UsingUVChannels`を使用して、テクスチャがサンプリングするUVチャンネルを指定します。メッシュUVを使用しないテクスチャ (スクリーンスペース、MatCap、ビュー方向ベースなど) の場合は、`UsingUVChannels.NonMesh`を使用してください:

```csharp
matInfo.RegisterTextureUVUsage(
    "_MatCapTexture",
    "_MatCapTexture", 
    UsingUVChannels.NonMesh,  // メッシュUVからではない
    null  // UV変換なし
);
```

UVチャンネルがマテリアルプロパティに依存する場合:

```csharp
var uvChannel = matInfo.GetFloat("_UVChannel") switch
{
    0 => UsingUVChannels.UV0,
    1 => UsingUVChannels.UV1,
    _ => UsingUVChannels.UV0 | UsingUVChannels.UV1  // 不明、どちらかの可能性
};

matInfo.RegisterTextureUVUsage("_DetailTex", "_DetailTex", uvChannel, uvMatrix);
```

#### UV変換マトリックス {#uv-transform-matrices}

UV変換マトリックスは、UVがどのようにスケールおよびオフセットされるかを記述します (`_MainTex_ST`のように)。ほとんどのUnityシェーダーは`(scaleX, scaleY, offsetX, offsetY)`のVector4を使用します:

```csharp
var texST = matInfo.GetVector("_MainTex_ST");
Matrix2x3? uvMatrix = texST is { } st 
    ? Matrix2x3.NewScaleOffset(st)
    : null;
```

必要に応じてマトリックスを手動で構築することもできます。UV変換がアニメーション化されているか動的に計算される場合は、`null`を使用してください。

### 頂点インデックス使用状況の登録 {#register-vertex-index}

シェーダーが頂点インデックス使用状況を登録すると、Avatar Optimizerは元のメッシュから頂点インデックスを保持しようとします。
これは現在、Trace and Optimizeの自動Merge Skinned Mesh機能を無効にしますが、将来的にはより多くの機能が影響を受ける可能性があります。

このメソッドは頂点インデックスを保持するためのものです。シェーダーがランダムなシーケンスを生成する目的でのみ頂点インデックスを使用している場合は、RegisterVertexIndexUsage を呼び出すべきではありません。

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

シェーダーに情報をリンクするには、ShaderInformation実装を登録する必要があります。
Shader Informationを登録する方法は2つあります:

### GUIDで登録 (推奨) {#register-by-guid}

シェーダーアセットの場合、シェーダーのGUIDを使用します。このメソッドは、GUIDが変更されず、重複せず、AssetDatabaseへのアクセスを必要としないため推奨されます (InitializeOnLoadメソッドでAssetDatabaseにアクセスすることは無効であるため、シェーダーインスタンスによる登録は無効になる可能性があります)。

```csharp
ShaderInformationRegistry.RegisterShaderInformationWithGUID(
    "your-shader-asset-guid",
    new YourShaderInformation()
);
```

### シェーダーインスタンスで登録 {#register-by-instance}

ビルド時に動的に作成されるシェーダー、またはシェーダーインスタンスがある場合、シェーダーインスタンスで登録できます。

```csharp
Shader shader = Shader.Find("Your/Shader/Name");
ShaderInformationRegistry.RegisterShaderInformation(
    shader,
    new YourShaderInformation()
);
```

## ベストプラクティス {#best-practices}

### InitializeOnLoadを使用 {#use-initializeonload}

`[InitializeOnLoad]`を使用してstatic constructorでShader Informationを登録し、'apply on play'ビルドの前に登録してください。

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

### 不明な値を処理 {#handle-unknown-values}

マテリアルプロパティはアニメーション化されている可能性があります。`null`値を想定して処理してください。シェーダーがプロパティのアニメーション化をサポートしていない場合でも、Avatar Optimizerが一度に複数のマテリアルを処理することにより`null`が渡されることがあります。

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

### キーワードとプロパティをチェック {#check-keywords-properties}

実際に使用されているテクスチャのみを登録してください:

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

**注意:** `!= false`は、値が`true`または`null` (不明) かどうかをチェックします。
この保守的なアプローチは、不明な場合に機能が有効であると仮定します。

### 正確な情報を提供 {#provide-accurate-information}

- 頂点インデックスが本当に重要な場合にのみ`VertexIndexUsage`を設定
- 正しいサンプラーステートを使用 (アトラス化中のテクスチャフィルタリングに影響)
- UV行列が動的またはアニメーション化されている場合は`null`に設定
- スクリーンスペースUVには`UsingUVChannels.NonMesh`を使用

### Shader Informationクラスに`internal class`を使用する {#use-internal-class}

Shader Informationクラスをアセンブリの公開APIにさらさないために、`internal class` として宣言することを推奨します。これによりコードベースがクリーンになり、内部の実装詳細が誤って外部から使用されるのを防げます。

エディタ側に公開APIがない場合、アセンブリ定義の Auto Reference を false に設定して、クラスが `Assembly-CSharp` に露出するのを防ぐことができます。

## 完全な例 {#examples}

シンプルなシェーダーのためのシンプルなShaderInformation例です。

より複雑な例については、[GitHub][shader-information-impl]にあるAvatar Optimizerの組み込みシェーダー情報実装を参照してください。

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

### 条件付き機能を持つシェーダー {#example-conditional}

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

        // シェーダーは効果のためにSV_VertexIDを使用
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
