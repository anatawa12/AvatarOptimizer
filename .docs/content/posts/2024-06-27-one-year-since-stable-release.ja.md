---
weight: 1
title: "v1.0.0一周年を記念してちょっと歴史を振り返る"
---

AAO: Avatar Optimizer v1.0.0一周年を記念してちょっと歴史を振り返ってみることにしてみる。

書いた人はanatawa12なので「私」はanatawa12を指す。

## Avatar Optimizerが生まれるまで {#before-born}

### 2021年11月 {#2021-11}

私がVRChatを始める。
このときにPublic (現在のLegacy欄)アバターから 2a7sちゃんを選んで使うことにした。
そしてこの子はSDK2アバターでかつ、パーツが結合された状態でfbxが組まれていた。
このときの経験がSkinned Mesh Rendererを増やすことに対する待避感につながってる気がする。

### 2022年 {#2022}

和服を作ってた間などに(?)fbx更新でモデルがひどくなることの対策にprefab variantが使えることを知った。
それで非破壊で服を着せるツールが欲しくなってたりした。

また、買っただけになってたアバターなどをアップロードを始めてメッシュの分割などが多くて結合するツールが欲しくなってたが、
お金はかけたくなかった。

### 2022年11月末 {#2022-11}

Modular Avatar v1.0.0がリリースされる。

{{< tweet user=bd_j id=1596993180573978625 >}}

当日中にvpmでインストールして使ってみて、すぐに良さそうだなってなった。
と同時にissueとか書き始める([PhysBone Blocker]とか)。

## Avatar Optimizerが生まれる {#born}

### pre-0.0.1 (2022年12月末) {#pre-0.0.1}

Modular Avatarを読んでできそうということで、PhysBoneやMerge Skinned Meshのマージをするツールを作り始める。
当初はMergerという名前で作っていた。

(この名残で https://github.com/anatawa12/Merger というリンクでからAvatar Optimizerに飛べるようになっています。)

{{< tweet user=anatawa12_vrc id=1607780946845257730 >}}

### 0.0.1~0.1.3 (2023年1月~2月) {#0.0.1-0.1.3}

マージする以外にBlendShapeの固定機能等をいれることにしてAvatarOptimizerに改名し、初回リリースをする。
https://github.com/anatawa12/AvatarOptimizer/releases/tag/v0.0.1

READMEに書かれてる通り、以下のコンポーネントが当時あった。

- Merge Skinned Mesh
- Merge PhysBone
- Freeze BlendShape
- Merge Bone
- Clear Endpoint Position

その後、0.0.2ではMerge ToonLit Material, 0.1.1ではRemove Mesh in Boxを追加し、バグ修正でリリースを行っていた。

## Avatar Optimizerが広まり始める {#begin-spread}

### v0.2.0開発中 & 0.1.4 (2023年2月頭) {#0.2.0-developing}

保存形式の破壊的変更を行うため、v0.2.0を開発中、Avatar Optimizerの紹介記事が書かれる。
[ModularAvatarとAvatarOptimizerを使ったアバター編集｜かんとう](https://note.com/tazutazu/n/na52579a6656f)

当時の私の叫び

{{< tweet user=anatawa12_vrc id=1629057081335771136 >}}

破壊的変更に対応させるために 0.1.4 で破壊的変更への準備リリースを急遽用意し、マイグレーションの仕組みの開発を決定

## 継続的な開発 {#continue-develop}

### v0.2.0 (2023年3月頭) {#0.2.0}

いくつかのprereleaseを経て、v0.2.0をリリースした。

この時期からそれなりにクオリティを保ったソフトウェアとして開発を進めていくことになる。
そのため、Changelogの管理やprereleaseの仕組みを追加したのがこの時期。

### v0.2.2 (2023年3月中旬) {#0.2.2}

Make Childrenコンポーネントを追加。
v1.0.0まで残るのですが、今となってはこれをAAOに入れたのは後悔してる。
(当初はNDMFがなく、Apply on playがクソ大変だったのでAvatar Optimizerに入れちゃってた。)

### v0.2.6 (2023年3月末) {#0.2.7}

多言語サポートの内部的な追加を行う(が、日本語の追加は0.4.0まで延びる)。
(誰か和訳作ってくれ〜と思ってたけど誰もやってくれなかった。)

### v0.3.2 (2023年5月) {#0.3.2}

公式ドキュメントを作成。

書くの面倒だなぁって思いながら書いていた。

### v0.4.0-v0.4.12 (2023年6月) {#0.4.0-0.4.6}

日本語の追加やMergePhysBoneの保存形式の変更を行ってた。

v1.0.0を見据えて色々準備してた。

## ついに安定版 {#stable}

### v1.0.0 (2023年6月末) {#1.0.0}

半月ほどのprereleaseを経て、v1.0.0をリリースした。

やっとできたって感じがあった。
さやまめさんに背中押されたりして大々的に告知を打ったら拡散され方に少し恐れてた

{{< tweet user=anatawa12_vrc id=1673650862160482304 >}}

### v1.1.x (2023年7月中旬~) {#1.1.x}

このタイミングでModular AvatarのApply on Playを上書きする仕組みなどを追加した。

当初は内部的な仕組みを公開するつもりがある程度あったけど、後にNDMFで全く別な形になる。

### v1.2.0 (2023年7月中旬) {#1.2.x}

今日のAvatar Optimizerの目玉機能となった Trace and Optimize の前身となる Automatic Configuration 機能を追加した。

当時は FreezeBlendShape の自動的に設定を行っていた。この時点では手動設定のできるものを自動的に設定する機能のみであったため、
Automatic Configuration という名前になっていた。

当初はここまで目玉になるとは思ってなかった。

### v1.3.x (2023年8月中旬~) {#1.3.x}

使われていないものの削除のように自動設定以上のものが追加されたため、Automatic Configuration が Trace and Optimize に改名された。

また、Avatar Optimizerの名前がAnatawa12's Avatar Optimizerとされ、AAOという略称も定まった。

当初からAnatawa12'sを入れるのが嫌いじゃなかったので後に`AAO: Avatar Optimizer`と再帰的接頭語に変更された。

### v1.4.x (2023年9月頭~) {#1.4.x}

Trace and Optimizeで使っていたAnimatorのパーサーが強化された。

このリリースは主にバグ修正だけど大きな変更があったからminorにした記憶がある。

### v1.5.x (2023年10月頭~) {#1.5.x}

Modular Avatarなどの非破壊ツール向けのフレームワークである[Non Destructive Modifying Framework][NDMF]がリリースされ、合わせてAAOもNDMFに対応した。

また、この時期から本格的に Trace and Optimize に注力するようになる。

### v1.6.x (2023年11月末~) {#1.6.x}

Trace and Optimizeへのコンポーネント対応を追加するためのAPIを整備した。

また、Trace and OptimizeにメッシュのオンオフにPhysBoneが追従する機能を追加した。

#### v1.6.3 (2023年12月初旬) {#1.6.3}

VRCSDK 3.5.0の3時間betaに振り回されたリリース

CHANGELOGより引用

> - I was planned to release this changes while VRCSDK 3.5.0 is in beta.
> - However, VRCSDK 3.5.0 beta was only 3 hours so I could not.

このVRCSDKの動きが vrc-get-gui / ALCOM を作る原動力になった。

### v1.7.x (2024年4月中旬~現在) {#1.7.x}

Animator Optimizerや自動 Merge Skinned Mesh、コンポーネント自身のAPI、Remove Mesh by Mask等とともにリリース。

VRCSDK 3.5.0 + VCC 2.2.0の酷さに困って vrc-get-gui (現ALCOM) を作ってたので間が空いてしまった。

結構長い間空いちゃったなぁと思いつついろんな最適化を入れれてるので満足してる。

## 未来 {#future}

今後はTrace and Optimizeの機能を強化していこうかなと思ってる。
特にテクスチャメモリ周りを一切触れていないのでうまいことやりたいなぁと思ってる。

[ma-1.0.0-tw]: https://x.com/bd_j/status/1596993180573978625
[PhysBone Blocker]: https://github.com/bdunderscore/modular-avatar/issues/104
[NDMF]: https://ndmf.nadena.dev/
