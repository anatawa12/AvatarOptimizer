---
title: アセットの配布に関して
---

# アセットの配布に関して {#distributing-prefabs}

Avatar Optimizer はアセットを軽量化し最適化するのに役立つかもしれません。
Avatar Optimizer に依存するアセットを配布する際のいくつかの推奨事項を以下に示します。

## ユーザーを公式の Avatar Optimizer 配布先に誘導する {#guide-to-the-official-distribution}

Avatar Optimizer は MIT ライセンスの下で公開されているため、再配布することは許可されています。
ただし、公式の Avatar Optimizer 配布先にユーザーを誘導することを強くお勧めします。
あなたの配布する Avatar Optimizer は古い可能性があります。
もし古いバージョンの Avatar Optimizer を使っている場合、公式版にはすでに修正されたバグが含まれているかもしれません。
また、Avatar Optimizer は前方互換性を保証していないため、あなたの配布するものよりも新しいバージョンの Avatar Optimizer を使っているユーザーの環境で問題が発生します。

ユーザーを公式の Avatar Optimizer 配布先に誘導するための推奨方法は以下の通りです。
1. VPM であなたのアセットが配布されている場合は、VPM で Avatar Optimizer をインストールするようにユーザーに指示してください。\
   あなたのアセットが VPM で配布されている場合は、`package.json` で Avatar Optimizer をあなたのアセットの依存関係として宣言し、
   ユーザーに Avatar Optimizer の VPM リポジトリを VCC に追加するように誘導してください。
   [こちら][add-repo] は Avatar Optimizer リポジトリを VCC に追加するためのリンクです。
   あなたの VPM リポジトリで[Avatar Optimizerのリポジトリ][repo]をミラーしても構いません。
   ミラーする際にはGitHubのReleasesからではなく、VPMのレポジトリをミラーしてください。
2. 公式の Avatar Optimizer のドキュメントへのリンクを提供してください。\
   [このリンク][official-installation] は公式のインストールガイドです。
    このページはユーザーに Avatar Optimizer を推奨される方法でインストールするように誘導します。
3. [Avatar Optimizer のブースページ][booth-aao] へのリンクを提供してください。\
   ブースページは Avatar Optimizer の公式配布ページの一つです。
   ブースアイテムには最新バージョンの VPAI インストーラー unitypackage が含まれています。
4. VPAI インストーラー unitypackage を含め、ユーザーに unitypackage をインポートするように誘導してください。\
   VPAI インストーラーは Avatar Optimizer のような VPM パッケージを unitypackage をインポートするだけでインストールするためのツールです。
   Avatar Optimizer 1.x.x 用の VPAI インストーラー unitypackage をダウンロードするためのリンクは[こちら][vpai]です。

[add-repo]: https://vpm.anatawa12.com/add-repo
[repo]: https://vpm.anatawa12.com/vpm.json
[official-installation]: https://vpm.anatawa12.com/avatar-optimizer/ja/#installation
[vpai]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-%7b%7d-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=1.x.x
[booth-aao]: https://anatawa12.booth.pm/items/4885109
