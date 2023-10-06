---
title: Introduction
type: docs
---

# Avatar Optimizer

anatawa12によるアバター軽量化用のちょっとした非破壊ユーティリティ群です。
これらのユーティリティはPlayモードに入るときかアバターをビルドするときに適用されます。

Avatar Optimizerは[GitHub]上で開発され、[MIT License]で公開されているオープンソースソフトウェアです。

[GitHub]: https://github.com/anatawa12/AvatarOptimizer
[MIT License]: https://github.com/anatawa12/AvatarOptimizer/blob/HEAD/LICENSE

## インストール {#installation}

Avatar Optimizerは[VPM][vpm]レポジトリを使用して公開されているため、任意のvpmクライアントを使用してインストールできます。

### VCC を使用する (推奨) {#installation-vcc}

1. [このリンク][VCC-add-repo-link]をクリックしてanatawa12のレポジトリを追加する。
2. VCCでAvatar Optimizerを追加する。

### UnityPackageを使用する {#installation-vpai}

unitypackageをインポートするだけでもこのツールをインストールできます。（VCCから追加する方法と全く同じようになります）

1. [ここ][installer unitypackage 1.x.x]からインストーラunitypackageをダウンロードする。
2. unitypackageをプロジェクトにインポートする。

<details>
<summary>他のバージョン用のインストーラ</summary>

- [0.1.x][installer unitypackage 0.1.x]
- [0.2.x][installer unitypackage 0.2.x]
- [0.3.x][installer unitypackage 0.3.x]
- [0.4.x][installer unitypackage 0.4.x]
- [x.x.x including beta releases][installer unitypackage x.x beta]

</details>

このインストール方法は[VPAI]により実現されています。

### vrc-getを使用する {#installation-vrc-get}

もしコマンドラインに精通しているのであれば、[vrc-get][vrc-get]を使用してインストールできます。

```bash
# add our vpm repository
vrc-get repo add https://vpm.anatawa12.com/vpm.json
# add package to your project
cd /path/to/your-unity-project
vrc-get install com.anatawa12.avatar-optimizer
```

### VPMコマンドラインインターフェースを使用する {#installation-vpm-cli}

もしコマンドラインに精通しているのであれば、[VPM/VCC CLI][vcc-cli]を使用してインストールできます。

```bash
# add our vpm repository
vpm add repo https://vpm.anatawa12.com/vpm.json
# add package to your project
cd /path/to/your-unity-project
vpm add package com.anatawa12.avatar-optimizer
```

[VPAI]: https://github.com/anatawa12/VPMPackageAutoInstaller
[vpm]: https://vcc.docs.vrchat.com/vpm/
[vcc-cli]: https://vcc.docs.vrchat.com/vpm/cli
[vrc-get]: https://github.com/anatawa12/vrc-get
[VCC-add-repo-link]: https://vpm.anatawa12.com/add-repo

[installer unitypackage 1.x.x]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=1.x.x
[installer unitypackage 0.4.x]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=0.4.x
[installer unitypackage 0.3.x]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=0.3.x
[installer unitypackage 0.2.x]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=0.2.x
[installer unitypackage 0.1.x]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=0.1.x
[installer unitypackage x.x beta]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-beta-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=x.x.x&prerelease
