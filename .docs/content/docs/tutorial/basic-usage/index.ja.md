---
title: 基本的な使い方
---

基本的な使い方
===

自動的な最適化の設定する {#trace-and-optimize}
--

アバターには自動的にできる最適化がいくつかあります

- 使用していないBlendShapeの削除
  - BlendShapeは重みが0でないと処理不可が発生するので重みを変更しない場合固定すると軽くなります
  - また、重みが常に0であってもアバターの容量が大きくなってしまうため可能な場合には削除します。
  - また、区切り線のBlendShapeなど動かす頂点のないBlendShapeも削除されます
- 使われないPhysBone等の削除
  - もし素体の服のように使っていないPhysBoneがある場合にはその計算不可が無駄になってしまいます
- アニメーションしていないボーンの親への統合
  - 服のボーンをを用いて服を着せるような場合には動かさないボーンが多く作られます。そのようなボーンは無駄な負荷になります。

AvatarOptimizerにはこれらの自動的な最適化を行う機能があります！

自動的な最適化設定は、アバターのルートに`Trace And Optimize`を追加するだけで終わりです！

![add-trace-and-optimize.png](add-trace-and-optimize.png)

メッシュを結合してMesh Renderersを減らす {#merge-skinned-mesh}
--

Avatar Optimizerを使用すると簡単にSkinned Meshを結合することができます！
Skinned Meshを結合すると個別にオン・オフできなくなりますが、結合することで軽量化になります！

{{< hint info >}}

**なせSkinned Meshを結合するの？**

Skinned Meshを結合するとメッシュを変形させる処理の回数が減るため軽くなります。
また、MergeSkinnedMeshで結合すると同じマテリアルのマテリアルスロットを結合できるので、描画処理の回数も減らす事ができます。

{{< /hint >}}

一番単純なパターンとして、Anonちゃんを軽量化してみます。

![start.png](./start.png)

まず初めに、統合先のGameObjectを作りましょう。
アバターのGameObjectを右クリックから `Create Empty` をクリックして新たなGameObjectを作ります。
そうしたら、わかりやすい名前に変えておいてください。この記事では`Anon_Merged`とします。

![create-empty.png](./create-empty.png)

次に、`Anon_Merged`に`Merge Skinned Mesh`を追加しましょう。

![add-merge-skinned-mesh.png](./add-merge-skinned-mesh.png)

すると`Merge Skinned Mesh`と`Skinned Mesh Renderer`が追加されます。

この`Merge Skinned Mesh`は、指定されたメッシュ[^mesh]を一緒についているメッシュに統合します。
統合を機能させるために、`Merge Skinned Mesh`に統合対象のメッシュを指定しましょう！

指定を楽にするために、`Anon_Merged`を選択した状態でinspectorをロックしましょう。
こうすることで複数のメッシュをまとめてドラックアンドドロップできるようになります。[^tip-lock-inspector]

![lock-inspector.png](./lock-inspector.png)

それではHierarchyで顔のメッシュであるBody以外のメッシュを選択してドラックアンドドロップでSkinned Renderersに指定しましょう！

![drag-and-drop.png](./drag-and-drop.png)

{{< hint info >}}

**なせ顔のメッシュは統合しないの？**

BlendShape(シェイプキー)は頂点数とBlendShape数の積に比例して重くなる処理です。
そのため、BlendShapeの数が多い顔のメッシュを頂点数の多い体のメッシュと統合するとかえって重くなってしまうため、顔は別のままにするのを推奨しています。

{{< /hint >}}

続いて、`Anon_Merged`の設定をしましょう！

`Merge Skinned Mesh`は諸事情[^merge-skinned-mesh]によりボーン、メッシュ、マテリアル、BlendShape、Bounds以外の設定を自動的には行いません。
そのため、Root Bone, Anchor Override等を手動で設定してください。
Anchor Overrideには素体で用いられているものを、Root BoneにはHipsを指定すると上手くいくことが多いと思います。

{{< hint info >}}

### UploadせずにPerformance Rankを見る方法 {#performance-rank-without-upload}

このAvatar Optimizerは非破壊改変ツールのため、VRCSDKのControl Panel上のPerformance Rankはあてにならなくなります。

その代わりにPlayモードに入った際のPerformance Rankをanatawa12's Gist PackのActual Performance Windowを使用してみられます。
詳しくは[anatawa12's Gist Packの使い方][gists-basic-usage]および[Actual Performance Windowのドキュメント][Actual Performance Window]を参照してください。

[gists-basic-usage]: https://vpm.anatawa12.com/gists/ja/docs/basic-usage/
[Actual Performance Window]: https://vpm.anatawa12.com/gists/ja/docs/reference/actual-performance-window/

{{< /hint >}}

[^tip-lock-inspector]: PhysBoneに複数のコライダーを指定したりするのにも使えます。色んなところで使えるので覚えておくと便利だと思います。
[^merge-skinned-mesh]: Root Bone/Anchor Overrideは等しくないと統合できないため対応予定がありません。もし良いアルゴリズムがあれば教えてください。
[^mesh]: この記事ではメッシュはUnityのMesh assetではなくSkinnedMeshRendererの意味で使用しています。

シュリンクBlendShapeでメッシュを減らす
---

服に隠れている素体部分など、見えない部分のメッシュを削除すると描画負荷、BlendShapeの処理負荷などが減るため軽量化になります。
AvatarOptimizerではそのために多くの素体で含まれているシュリンク用のBlendShapeを用いてメッシュを削除できます！

素体のメッシュに`Remove Mesh By BlendShape`コンポーネントを追加しましょう！

思ってもいない部位が削除されないかを確認するため、`プレビューのために切り替えたブレンドシェイプの値を自動的に変更する`にチェックし、
下のBlendShapeの一覧から削除したい部位のシュリンク用BlendShapeを選択しましょう！

TODO: 写真
