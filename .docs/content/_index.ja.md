---
title: Introduction
type: docs
---

# AAO: Avatar Optimizer

anatawa12によるアバター軽量化用のちょっとした非破壊ユーティリティ群です。
これらのユーティリティはPlayモードに入るときかアバターをビルドするときに適用されます。

Avatar Optimizerは[GitHub]上で開発され、[MIT License]で公開されているオープンソースソフトウェアです。

[GitHub]: https://github.com/anatawa12/AvatarOptimizer
[MIT License]: https://github.com/anatawa12/AvatarOptimizer/blob/HEAD/LICENSE

## インストール {#installation}

Avatar Optimizerは[VPM][vpm]リポジトリを使用して公開されているため、任意のVPMクライアントを使用してインストールできます。

{{< beta-only color="success" >}}
<blockquote class="book-hint info">

プレリリース版のAvatar Optimizerでは、anatawa12のVPMリポジトリで提供されていないNDMFのプレリリース版が使用されている場合があります。
その場合、ALCOMにbd_ prereleasesリポジトリ(<https://vpm.nadena.dev/vpm-prerelease.json>)を追加する必要があります。

</blockquote>
{{< /beta-only >}}

<div id="installation-vcc"></div> <!-- compatibility with older docs -->

### ALCOM を使用する (推奨) {#installation-alcom}

1. [このリンク][VCC-add-repo-link]をクリックして、anatawa12のVPMリポジトリを追加します。
2. AAO: Avatar Optimizerをプロジェクトに追加します！

このリンクは VCC と共通であるため、同じ方法で VCC を使用してこのパッケージをプロジェクトに追加できます。

VCC に存在するバグ等により、正しく動作しない可能性があるため、 [ALCOM] を使用することをお勧めします。

### UnityPackageを使用する {#installation-vpai}

unitypackageをインポートするだけでもこのツールをインストールできます。（ALCOMから追加する方法と全く同じようになります）

1. [ここ][installer unitypackage 1.x.x]からインストーラunitypackageをダウンロードします。
2. unitypackageをプロジェクトにインポート！

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

もしコマンドラインに精通しているのであれば、[vrc-get][vrc-get]を使用してインストールすることもできます。

```bash
# add our vpm repository
vrc-get repo add https://vpm.anatawa12.com/vpm.json
# add package to your project
cd /path/to/your-unity-project
vrc-get install com.anatawa12.avatar-optimizer
```

### VPMコマンドラインインターフェースを使用する {#installation-vpm-cli}

もしコマンドラインに精通しているのであれば、[VPM/VCC CLI][vcc-cli]を使用してインストールすることもできます。

```bash
# add our vpm repository
vpm add repo https://vpm.anatawa12.com/vpm.json
# add package to your project
cd /path/to/your-unity-project
vpm add package com.anatawa12.avatar-optimizer
```
## 開発者 {#developers}

本プロジェクトは、主に2名のコア開発者によって開発されています。

{{< main-dev username="anatawa12" >}}
プロジェクトの発起人であり、メインメンテナーです。\
新機能の実装やバグ修正、ドキュメントの草稿作成などを主に担当しています。
{{< /main-dev >}}

{{< main-dev username="Sayamame-beans" >}}
主にドキュメント編集を担当しています。\
機能方針の検討やユーザーフィードバックの収集、プロジェクトの広報にも携わっています。
{{< /main-dev >}}

また、本プロジェクトの改善には、多くのコントリビューターの皆様にもご協力いただいています。
以下は、その一部の方々です。

皆様のご協力に心より感謝いたします。

{{< contributors >}}

[ALCOM]: https://vrc-get.anatawa12.com/alcom/
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
