---
title: アセットの配布について
---

# アセットの配布について {#distributing-prefabs}

Avatar Optimizerは配布したいアセットを軽量化したり最適化したりするのに役立つかもしれません。
Avatar Optimizerに依存するアセットを配布する際の推奨事項をいくつか示します。

## ユーザーをAvatar Optimizer公式の配布場所に誘導する {#guide-to-the-official-distribution}

Avatar OptimizerはMITライセンスの下で公開されているため、再配布することが認められています。
それでも、Avatar Optimizer公式の配布場所にユーザーを誘導することを強くお勧めします。\
非公式に配布されるAvatar Optimizerはバージョンが古い可能性があります。
古いバージョンのAvatar Optimizerを使用していると、公式版では既に修正されているバグに遭遇するかもしれません。
また、Avatar Optimizerは前方互換性を保証していないため、新しいバージョンから古いバージョンにダウングレードさせてしまうと、そのユーザーの環境で何らかの問題が発生する可能性があります。

ユーザーをAvatar Optimizer公式の配布場所に誘導する際に推奨される方法は以下の通りです。
1. VPMでアセットを配布する場合は、Avatar OptimizerをVPMからインストールするように指示してください。\
   VPMで配布するアセットの`package.json`でAvatar Optimizerを依存関係として宣言し、
   Avatar OptimizerのVPMリポジトリをALCOMまたはVCCに追加するようにユーザーを誘導してください。
   [こちら][add-repo]はAvatar Optimizerの公式リポジトリをALCOMまたはVCCに追加するためのリンクです。\
   また、配布するアセットと同じVPMリポジトリに[Avatar Optimizerの公式リポジトリ][repo]をミラーしても構いません。
   ミラーする場合は、GitHubのReleasesからミラーするのではなく、VPMリポジトリ自体をミラーしてください。
2. Avatar Optimizerの公式ドキュメントへのリンクを提供してください。\
   [こちら][official-installation]はAvatar Optimizer公式のインストールガイドです。
   このガイドは、Avatar Optimizerを推奨される方法でインストールするようにユーザーを誘導します。
3. [Avatar OptimizerのBoothページ][booth-aao]へのリンクを提供してください。\
   BoothページはAvatar Optimizer公式の配布場所の1つです。
   Boothでは、最新バージョンのVPAIインストーラーunitypackageが含まれています。
4. VPAIインストーラーunitypackageを同梱し、そのunitypackageをインポートするように指示してください。\
   VPAIインストーラーは、Avatar OptimizerのようなVPMパッケージを、unitypackageをインポートするだけでインストールするためのツールです。
   Avatar Optimizer 1.x.x用のVPAIインストーラーunitypackageをダウンロードするためのリンクは[こちら][vpai]です。

[add-repo]: https://vpm.anatawa12.com/add-repo
[repo]: https://vpm.anatawa12.com/vpm.json
[official-installation]: https://vpm.anatawa12.com/avatar-optimizer/ja/#installation
[vpai]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-%7b%7d-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=1.x.x
[booth-aao]: https://anatawa12.booth.pm/items/4885109
