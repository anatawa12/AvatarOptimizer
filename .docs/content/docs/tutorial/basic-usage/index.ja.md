---
title: 基本的な使い方
---

基本的な使い方
===

メッシュを結合してMesh Renderersを減らす {#merge-skinned-mesh}
--

Avatar Optimizerを使用すると簡単にSkinned Meshを結合することができます！

{{< hint info >}}

**なせSkinned Meshを結合するの？**

SkinnedMeshを結合するとメッシュを変形させる処理の回数が減るため軽くなります。
また、MergeSkinnedMeshで結合すると同じマテリアルのマテリアルスロットを結合できるので、描画処理の回数も減らす事ができます

{{< /hint >}}

今回はまず初めに一番単純なパターンとしてAnonちゃんを軽量化します

![start.png](./start.png)

まず初めにマージ先のGameObjectを作りましょう。
アバターのGameObjectを右クリックから `Create Empty` をクリックして新たなGameObjectを作ります。
そしたらわかりやすい名前に変えておいてください。この記事では`Anon_Merged`とします

![create-empty.png](./create-empty.png)

そしたら`Anon_Merged`に`Merge Skinned Mesh`を追加しましょう。

![add-merge-skinned-mesh.png](./add-merge-skinned-mesh.png)

すると`Merge Skinned Mesh`と`Skinned Mesh Renderer`が追加されます。

この`Merge Skinned Mesh`は、指定されたメッシュ[^mesh]を一緒についているメッシュにマージします。
マージを機能させるために`Merge Skinned Mesh`にマージするメッシュを指定しましょう！

指定を楽にするために、`Anon_Merged`を選択した状態でinspectorをロックしましょう。
こうすることで複数のメッシュをまとめてドラックアンドドロップできるようになります。[^tip-lock-inspector]

![lock-inspector.png](./lock-inspector.png)

それではHierarchyで顔のメッシュであるBody以外のメッシュを選択してドラックアンドドロップでSkinned Renderersに指定しましょう！

![drag-and-drop.png](./drag-and-drop.png)

{{< hint info >}}

**なせ顔のメッシュを結合しないの？**

BlendShape(シェイプキー)[^blend-shape]は頂点数とBlendShape数の積に比例して重くなる処理です。
そのため、BlendShapeの数が多い顔のメッシュを頂点数の多い体のメッシュと結合するとかえって重くなってしまうため、顔は別のままにするのを推奨しています

{{< /hint >}}

続いて、`Anon_Merged`の設定をしましょう！

`Merge Skinned Mesh`は諸事情[^merge-skinned-mesh]によりボーン、メッシュ、マテリアル、BlendShape、Bounds以外の設定を自動的には行いません。
そのため、Root Bone, Anchor Override等を手動で設定してください。
Anchor Overrideは素体のを、Root BoneはHipsを指定するとうまくいくことが多いと思います。

{{< hint info >}}

### UploadせずにPerformance Rankを見る方法 {#performance-rank-without-upload}

このAvatar Optimizerは非破壊改変ツールのため、VRCSDKのControl Panel上のPerformance Rankはあてにならなくなります。

その代わりにPlayモードに入った際のPerformance Rankをanatawa12's Gist PackのActual Performance Windowを使用してみられます。
詳しくは[anatawa12's Gist Packの使い方][gists-basic-usage]および[Actual Performance Windowのドキュメント][Actual Performance Window]を参照してください。

[gists-basic-usage]: https://vpm.anatawa12.com/gists/ja/docs/basic-usage/
[Actual Performance Window]: https://vpm.anatawa12.com/gists/ja/docs/reference/actual-performance-window/

{{< /hint >}}

[^tip-lock-inspector]: PhysBoneに複数のコライダーを指定するのにも使えたり、色んなところで使えるので覚えておくと便利だと思います。
[^merge-skinned-mesh]: Root Bone/Anchor Overrideは等しくないとマージできないため対応予定がないです。もし良いアルゴリズムを教えてください。
[^mesh]: この記事ではメッシュはUnityのMesh assetではなくSkinnedMeshRendererの意味で使用しています。

BlendShapeを固定する {#freeze-blendshape}
---

また、Avatar Optimizerを使用すると簡単にBlendShape(シェイプキー)[^blend-shape]を固定することができます！

{{< hint info >}}

**なせBlendShapeを固定するの？**

前述のように、BlendShapeは頂点数とBlendShape数の積に比例して重くなる処理です。
また、BlendShapeはweightに関わらず存在するだけで負荷になってしまいます。
そのためPerformance Rankには反映されませんが固定することが軽量化になります。
可能であれば結合したメッシュはBlendShapeが存在しないmeshにすると良いです。

{{< /hint >}}

それでは使わない素体や服のの体型変更用のBlendShapeを結合してみましょう！

頂点数が増えたメッシュである先程の`Anon_Merged`に`Freeze BlendShapes`を追加しましょう！

![add-freeze-blendshape.png](add-freeze-blendshape.png)

`Freeze BlendShape`は一緒についているメッシュのBlendShapeを固定します。

マージを機能させるために固定するBlendShapeを指定しましょう！
チェックボックスにチェックするとそのBlendShapeは固定されます。

![freeze-blendshape.png](freeze-blendshape.png)

これでBlendShapeの固定の設定は終わりです！

[^blend-shape]: BlendShapeはUnity上のシェイプキーの名前です。UnityやMayaではBlend Shape、BlenderではShape Key、MetasequoiaやMMDではモーフと呼ばれます。
