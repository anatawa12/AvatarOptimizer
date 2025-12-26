---
title: Shader Information API
---

# Shader Information API

Avatar Optimizer v1.0.0以降、Avatar Optimizerはカスタムシェーダーを使用したマテリアルの最適化を支援するShader Information APIを提供しています。
シェーダー情報を登録することで、Avatar Optimizerがあなたのカスタムシェーダーに対してテクスチャアトラス化やUVパッキングなどの高度な最適化を実行できるようになります。

## Shader Informationとは？ {#what-is-shader-information}

Shader Informationは、あなたのシェーダーがテクスチャ、UVチャンネル、その他のマテリアルプロパティをどのように使用しているかをAvatar Optimizerに伝える方法です。
この情報により、Avatar Optimizerは以下を実行できるようになります:

- **複数のテクスチャをテクスチャアトラスにパッキング** (`AAO Merge Material`などのコンポーネントを使用)
- **使用されているUVチャンネルを理解してUVチャンネルを最適化**
- **シェーダーが依存している場合は頂点インデックスを保持**
- **未使用のマテリアルプロパティを安全に削除**

Shader Informationがない場合、Avatar Optimizerはシェーダーを保守的に扱い、これらの最適化を実行できません。

## 組み込みシェーダーサポート {#built-in-support}

Avatar Optimizerは、一般的なシェーダー向けにShader Informationを組み込みで提供しています:

- **Unity組み込みシェーダー** (Standard、Unlitなど)
- **VRChat SDKシェーダー** (Standard Lite、Toon Lit、Toon Standard)
- **lilToon** (バージョン45までの全てのバリアント)

これらのシェーダーを使用している場合、追加のセットアップは不要です。

## Shader Informationを提供すべき場合 {#when-to-provide}

以下の場合にシェーダーのShader Informationを提供する必要があります:

1. **シェーダーがVRChatアバターで使用されており**、ユーザーがAvatar Optimizerで最適化したい場合
2. **シェーダーがUV変換を伴うテクスチャを使用している**場合 (`_MainTex_ST`のスケール/オフセットなど)
3. **シェーダーが特殊効果のために頂点インデックスを使用している**場合
4. **`AAO Merge Material`コンポーネントでテクスチャアトラス化を有効にしたい**場合

シェーダーが単純な効果のみで高度な最適化が必要ない場合は、Shader Informationを提供する必要はないかもしれません。

## はじめに {#getting-started}

シェーダーのShader Informationを提供するには、以下の手順に従ってください:

### 1. Assembly Definitionを作成 {#create-asmdef}

シェーダーパッケージにEditorのassembly definitionがない場合は、作成してください。
Shader Informationはビルド時にのみ使用されるため、アセンブリはEditor専用にする必要があります。

### 2. Assembly Referenceを追加 {#add-reference}

assembly definitionの参照に`com.anatawa12.avatar-optimizer.api.editor`を追加してください。

Avatar Optimizerを必須にしたくない場合は、`AVATAR_OPTIMIZER`シンボルを使用して[Version Defines]を使用してください:

![version-defines.png](../make-your-components-compatible-with-aao/version-defines.png)

推奨バージョン範囲: `[1.0,2.0)` (v1.x.xをサポートしますが、v2.0.0では更新が必要になります)

### 3. Shader Informationクラスを作成 {#create-class}

`ShaderInformation`を継承するクラスを作成し、`ShaderInformationRegistry`に登録してください:

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

`SupportedInformationKind`プロパティは、提供している情報をAvatar Optimizerに伝えます:

### `TextureAndUVUsage` {#texture-uv-usage}

以下に関する情報を提供していることを示します:
- シェーダーが使用するテクスチャ
- 各テクスチャがサンプリングするUVチャンネル
- UV変換マトリックス (スケール/オフセット)
- サンプラーステート (ラップモード、フィルタモード)

これにより、**テクスチャアトラス化**と**UVパッキング**の最適化が可能になります。

### `VertexIndexUsage` {#vertex-index-usage}

シェーダーが頂点インデックス (例: `SV_VertexID`) を使用していることを示します。

このフラグを**提供しない**場合、Avatar Optimizerは頂点インデックスが**使用されていない**と仮定し、最適化中に頂点をシャッフルする可能性があります。
シェーダーが頂点インデックスを使用している場合、不正なレンダリングを防ぐため、このフラグを**設定する必要があります**。

### フラグの組み合わせ {#combining-flags}

フラグは`|`演算子で組み合わせることができます:

```csharp
public override ShaderInformationKind SupportedInformationKind =>
    ShaderInformationKind.TextureAndUVUsage | ShaderInformationKind.VertexIndexUsage;
```

## マテリアル情報の登録 {#registering-information}

`GetMaterialInformation`メソッドは、シェーダーを使用する各マテリアルに対して呼び出されます。
`MaterialInformationCallback`を使用してテクスチャとUV使用状況を登録してください。

### マテリアルプロパティの読み取り {#reading-properties}

コールバックはマテリアルプロパティを読み取るメソッドを提供します:

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

これらのメソッドは以下の場合に`null`を返します:
- プロパティが存在しない
- プロパティがアニメーション化されている (`considerAnimation: true`の場合、これがデフォルト)
- 値が不明または混在している

### テクスチャ使用状況の登録 {#registering-textures}

`RegisterTextureUVUsage`を使用して、各テクスチャについてAvatar Optimizerに伝えます:

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

#### パラメータ {#texture-params}

- **`textureMaterialPropertyName`**: テクスチャプロパティ名 (例: `"_MainTex"`)
- **`samplerState`**: 使用するサンプラー ([サンプラーステート](#sampler-states)を参照)
- **`uvChannels`**: テクスチャが使用するUVチャンネル ([UVチャンネル](#uv-channels)を参照)
- **`uvMatrix`**: UV変換マトリックス、または不明/動的な場合は`null`

### サンプラーステート {#sampler-states}

サンプラーステートはテクスチャのラッピングとフィルタリングを定義します。いくつかの方法で指定できます:

#### マテリアルプロパティサンプラーを使用 {#material-sampler}

ほとんどのシェーダーはマテリアルプロパティのサンプラーを使用します。プロパティ名を使用してください:

```csharp
matInfo.RegisterTextureUVUsage(
    "_MainTex",
    samplerState: "_MainTex",  // 文字列は暗黙的に変換されます
    UsingUVChannels.UV0,
    uvMatrix
);
```

または明示的に:

```csharp
samplerState: new SamplerStateInformation("_MainTex")
```

#### ハードコードされたサンプラーを使用 {#hardcoded-sampler}

シェーダーがインラインサンプラーを使用している場合 (例: `SamplerState linearClampSampler`)、定義済みの定数を使用してください:

```csharp
// ポイントフィルタリング
SamplerStateInformation.PointClampSampler
SamplerStateInformation.PointRepeatSampler
SamplerStateInformation.PointMirrorSampler
SamplerStateInformation.PointMirrorOnceSampler

// リニアフィルタリング
SamplerStateInformation.LinearClampSampler
SamplerStateInformation.LinearRepeatSampler
SamplerStateInformation.LinearMirrorSampler
SamplerStateInformation.LinearMirrorOnceSampler

// トリリニア/異方性フィルタリング
SamplerStateInformation.TrilinearClampSampler
SamplerStateInformation.TrilinearRepeatSampler
SamplerStateInformation.TrilinearMirrorSampler
SamplerStateInformation.TrilinearMirrorOnceSampler
```

例:

```csharp
matInfo.RegisterTextureUVUsage(
    "_NoiseTexture",
    SamplerStateInformation.LinearRepeatSampler,
    UsingUVChannels.NonMesh,
    null
);
```

#### 不明なサンプラー {#unknown-sampler}

サンプラーを判定できない場合:

```csharp
samplerState: SamplerStateInformation.Unknown
```

これにより、サンプラーステートに依存する最適化が防止されます。

### UVチャンネル {#uv-channels}

`UsingUVChannels`を使用して、テクスチャがサンプリングするUVチャンネルを指定します:

```csharp
UsingUVChannels.UV0  // TEXCOORD0
UsingUVChannels.UV1  // TEXCOORD1
UsingUVChannels.UV2  // TEXCOORD2
UsingUVChannels.UV3  // TEXCOORD3
UsingUVChannels.UV4  // TEXCOORD4
UsingUVChannels.UV5  // TEXCOORD5
UsingUVChannels.UV6  // TEXCOORD6
UsingUVChannels.UV7  // TEXCOORD7
UsingUVChannels.NonMesh  // スクリーンスペース、法線など
UsingUVChannels.Unknown  // 判定不可
```

#### 複数のUVチャンネル {#multiple-uv}

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

#### 非メッシュUV {#non-mesh-uv}

メッシュUVを使用しないテクスチャの場合 (スクリーンスペース、MatCap、ビュー方向ベースなど):

```csharp
matInfo.RegisterTextureUVUsage(
    "_MatCapTexture",
    "_MatCapTexture", 
    UsingUVChannels.NonMesh,  // メッシュUVからではない
    null  // UV変換なし
);
```

### UV変換マトリックス {#uv-matrices}

UV変換マトリックスは、UVがどのようにスケールおよびオフセットされるかを記述します (`_MainTex_ST`のように)。

#### スケール/オフセットベクトルから {#scale-offset}

ほとんどのUnityシェーダーは`(scaleX, scaleY, offsetX, offsetY)`のVector4を使用します:

```csharp
var texST = matInfo.GetVector("_MainTex_ST");
Matrix2x3? uvMatrix = texST is { } st 
    ? Matrix2x3.NewScaleOffset(st)
    : null;
```

#### 手動構築 {#manual-matrix}

マトリックスを手動で構築できます:

```csharp
// 単位行列 (変換なし)
Matrix2x3.Identity

// スケールのみ
Matrix2x3.Scale(2.0f, 2.0f)

// 平行移動のみ
Matrix2x3.Translate(0.5f, 0.5f)

// 回転 (ラジアン)
Matrix2x3.Rotate(Mathf.PI / 4)

// 変換を乗算で組み合わせ
var matrix = Matrix2x3.Scale(2, 2) * Matrix2x3.Translate(0.5f, 0.5f);

// 完全な手動構築
new Matrix2x3(
    m00: 1, m01: 0, m02: 0,  // 1行目: x変換
    m10: 0, m11: 1, m12: 0   // 2行目: y変換
);
```

#### 動的または不明な変換 {#unknown-transform}

UV変換がアニメーション化されているか実行時に計算される場合、`null`を使用してください:

```csharp
matInfo.RegisterTextureUVUsage(
    "_ScrollingTexture",
    "_ScrollingTexture",
    UsingUVChannels.UV0,
    null  // 変換が動的、最適化不可
);
```

### その他のUV使用状況の登録 {#other-uv-usage}

シェーダーがテクスチャサンプリング以外の目的でUVを使用する場合 (ただし整数部分のみを使用):

```csharp
// 例: floor(UV)のみを使用するUVベースのメッシュ間引き
matInfo.RegisterOtherUVUsage(UsingUVChannels.UV1);
```

**注意**: シェーダーがUVの整数部分のみを使用する場合にのみこれを使用してください (UV Tile Discardなど)。
シェーダーが計算のためにUVの小数値を使用する場合、これは不正確です。

### 頂点インデックス使用状況の登録 {#register-vertex-index}

シェーダーが頂点インデックスを使用している場合 (例: ノイズや特殊効果):

```csharp
public override void GetMaterialInformation(MaterialInformationCallback matInfo)
{
    // ... テクスチャを登録 ...

    // Avatar Optimizerにこのシェーダーが頂点インデックスを使用していることを伝える
    matInfo.RegisterVertexIndexUsage();
}
```

**重要**: 頂点インデックスが視覚的結果に大きく影響する場合にのみこれを呼び出してください。
頂点インデックスが微妙なノイズや軽微な効果にのみ使用されている場合は、より良い最適化を可能にするためこれを省略できます。

## 完全な例 {#examples}

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

        // 法線マップ (キーワードで条件付き)
        if (matInfo.IsShaderKeywordEnabled("_NORMALMAP") != false)
        {
            matInfo.RegisterTextureUVUsage(
                "_BumpMap", "_BumpMap", UsingUVChannels.UV0, mainUVMatrix
            );
        }

        // 詳細テクスチャ (プロパティで条件付き)
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

### 頂点インデックスを使用するシェーダー {#example-vertex-index}

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
        ShaderInformationKind.TextureAndUVUsage | 
        ShaderInformationKind.VertexIndexUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        // テクスチャを登録...
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

## 登録方法 {#registration-methods}

Shader Informationを登録する方法は2つあります:

### GUIDで登録 (推奨) {#register-by-guid}

シェーダーアセットの場合、シェーダーのGUIDを使用します:

```csharp
ShaderInformationRegistry.RegisterShaderInformationWithGUID(
    "your-shader-asset-guid",
    new YourShaderInformation()
);
```

**シェーダーGUIDの見つけ方:**

1. Projectウィンドウでシェーダーを選択
2. 右クリック → Copy GUID
3. または`.meta`ファイルを開いてGUIDをコピー

**利点:**
- シェーダーアセットがまだロードされていなくても機能します
- パッケージとして配布されるシェーダーアセットに推奨されます

### シェーダーインスタンスで登録 {#register-by-instance}

ランタイム生成シェーダーまたはシェーダーインスタンスがある場合:

```csharp
Shader shader = Shader.Find("Your/Shader/Name");
ShaderInformationRegistry.RegisterShaderInformation(
    shader,
    new YourShaderInformation()
);
```

**利点:**
- ランタイム生成シェーダーで機能します
- シェーダーインスタンスへの直接参照

**注意:** 同じシェーダーが両方の方法で登録されている場合、インスタンス登録が優先されます。

## ベストプラクティス {#best-practices}

### 1. InitializeOnLoadを使用 {#practice-initonload}

`[InitializeOnLoad]`を使用してstatic constructorでShader Informationを登録してください:

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

### 2. 不明な値を処理 {#practice-unknown}

マテリアルプロパティはアニメーション化されている可能性があります。`null`値を処理してください:

```csharp
// パターンマッチングを使用
var st = matInfo.GetVector("_MainTex_ST");
Matrix2x3? uvMatrix = st is { } st2 ? Matrix2x3.NewScaleOffset(st2) : null;

// デフォルト用のnull合体を使用
var uvChannel = matInfo.GetFloat("_UVChannel") switch
{
    0 => UsingUVChannels.UV0,
    1 => UsingUVChannels.UV1,
    null => UsingUVChannels.UV0 | UsingUVChannels.UV1,  // 不明
    _ => UsingUVChannels.UV0 | UsingUVChannels.UV1
};
```

### 3. キーワードとプロパティをチェック {#practice-keywords}

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

### 4. 正確な情報を提供 {#practice-accurate}

- 頂点インデックスが本当に重要な場合にのみ`VertexIndexUsage`を設定
- 正しいサンプラーステートを使用 (アトラス化中のテクスチャフィルタリングに影響)
- UV行列が動的またはアニメーション化されている場合は`null`に設定
- スクリーンスペースUVには`UsingUVChannels.NonMesh`を使用

### 5. 実装をテスト {#practice-test}

`AAO Merge Material`コンポーネントでテストして確認してください:

1. テクスチャが正しくアトラス化できる
2. UV変換が適切に適用される
3. 最適化後に視覚的なアーティファクトがない
4. 異なる設定を持つマテリアルが正しく処理される

## 制限事項と今後の計画 {#limitations}

### 現在の制限事項 {#current-limitations}

Avatar Optimizerは現在、以下の場合にUVパッキングを実行します:

- テクスチャが少数のマテリアルで使用されている
- UVチャンネルが単一チャンネル (マテリアルごと)
- UV変換が単位行列 (スケール/オフセット/回転なし)

これらの制限は将来のバージョンで緩和される可能性があります。

### 計画された改善 {#planned-improvements}

将来のバージョンでは以下をサポートする可能性があります:

- スケール ≤ 1.0で90°の倍数の回転を持つUV変換
- 複数のUVチャンネルテクスチャ
- より複雑なアトラス化戦略

これらの機能が追加されても、あなたのShader Informationは引き続き機能します。

## トラブルシューティング {#troubleshooting}

### テクスチャがアトラス化されない {#troubleshoot-no-atlas}

`AAO Merge Material`がテクスチャをアトラス化しない場合:

1. **Shader Informationが登録されているかを確認:**
   - static constructorにデバッグログを追加
   - シェーダーGUIDが正しいか確認

2. **UV行列を確認:**
   - 現在、アトラス化には単位行列のみがサポートされています
   - `_ST`が`(1,1,0,0)`の場合は`Matrix2x3.Identity`または`null`に設定

3. **UVチャンネルをチェック:**
   - 現在、マテリアルごとに単一のUVチャンネルのみがサポートされています
   - 複数のUVチャンネルを組み合わせない

4. **サンプラーステートを確認:**
   - サンプラーステートがアトラス化と互換性があることを確認

### ランタイムエラー {#troubleshoot-errors}

Shader Informationに関するエラーが発生した場合:

1. **"The shader is already registered"**
   - 同じシェーダーを複数回登録しない
   - 組み込み情報を置き換える場合は`IsInternalInformation`を使用

2. **アセンブリ参照エラー**
   - asmdefに`com.anatawa12.avatar-optimizer.api.editor`があることを確認
   - `#if AVATAR_OPTIMIZER && UNITY_EDITOR`でコードをラップ

### 視覚的なアーティファクト {#troubleshoot-artifacts}

最適化後にマテリアルが正しく見えない場合:

1. **UV行列を確認** - シェーダーの実際のUV変換と一致するか確認
2. **UVチャンネルを確認** - 正しいUVチャンネルを報告しているか確認
3. **サンプラーステートを確認** - 間違ったラップモードがテクスチャの繰り返し問題を引き起こす可能性
4. **頂点インデックス使用をテスト** - 頂点インデックスを使用している場合、`RegisterVertexIndexUsage()`を呼び出していることを確認

## サポート {#support}

質問やヘルプが必要な場合:

- **Discord**: [NDMF Discord] (#avatar-optimizerチャンネル)
- **Fediverse**: [@anatawa12@misskey.niri.la][fediverse]
- **GitHub Issues**: [AvatarOptimizer Issues]

ヘルプを求める際は、以下を含めてください:
- シェーダーコード (可能な場合)
- ShaderInformation実装
- 達成しようとしている最適化
- エラーメッセージや予期しない動作

[Version Defines]: https://docs.unity3d.com/2019.4/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html#define-symbols
[NDMF Discord]: https://discord.gg/dV4cVpewmM
[fediverse]: https://misskey.niri.la/@anatawa12
[AvatarOptimizer Issues]: https://github.com/anatawa12/AvatarOptimizer/issues
