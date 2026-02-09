---
title: Introduction
type: docs
---

# AAO: Avatar Optimizer

Set of Anatawa12's non-Destructive Small Avatar Optimization Utilities.
Those utilities are applied on entering play mode or building avatars.

Avatar Optimizer is a Open Source Software developed on [GitHub] published under the [MIT License].

[![discord link][shields-discord]][ndmf-discord]

[GitHub]: https://github.com/anatawa12/AvatarOptimizer
[MIT License]: https://github.com/anatawa12/AvatarOptimizer/blob/HEAD/LICENSE
[shields-discord]: https://img.shields.io/badge/chat_on-NDMF_Discord-5865F2?logo=discord&logoColor=white

[ndmf-discord]: https://discord.gg/dV4cVpewmM

## Installation {#installation}

Avatar Optimizer is published with [VPM][vpm] repository so you can install this package using any VPM clients.

{{< beta-only color="success" >}}
<blockquote class="book-hint info">

For pre-releases of Avatar Optimizer, AAO may use pre-releases of NDMF, which is not mirrored to anatawa12's VPM repository.
You may have to add <https://vpm.nadena.dev/vpm-prerelease.json>, bd_ prereleases repository, to your VCC.

</blockquote>
{{< /beta-only >}}

<div id="installation-vcc"></div> <!-- compatibility with older docs -->

### With ALCOM (Recommended) {#installation-alcom}

1. Click [this link][VCC-add-repo-link] to add anatawa12's VPM repository.
2. Add Avatar Optimizer to your project from VCC / ALCOM.

This link is common with VCC, so you may use VCC to add this package to your project.

Since it may not work correctly due to bugs in VCC, we recommend using [ALCOM].

### Using UnityPackage {#installation-vpai}

You can install this tool with just importing one unitypackage.

1. download installer unitypackage [here][installer unitypackage 1.x.x].
2. Import the unitypackage into your project.

<details>
<summary>Installer for other versions</summary>

- [0.1.x][installer unitypackage 0.1.x]
- [0.2.x][installer unitypackage 0.2.x]
- [0.3.x][installer unitypackage 0.3.x]
- [0.4.x][installer unitypackage 0.4.x]
- [x.x.x including beta releases][installer unitypackage x.x beta]

</details>

This installation method is provided in thanks to [VPAI].

### Using vrc-get {#installation-vrc-get}

If you're familiar with command line, You may install this package using [vrc-get][vrc-get].

```bash
# add our vpm repository
vrc-get repo add https://vpm.anatawa12.com/vpm.json
# add package to your project
cd /path/to/your-unity-project
vrc-get install com.anatawa12.avatar-optimizer
```

### Using VPM CommandLine Interface {#installation-vpm-cli}

If you're familiar with command line, You may install this package using [VPM/VCC CLI][vcc-cli].

```bash
# add our vpm repository
vpm add repo https://vpm.anatawa12.com/vpm.json
# add package to your project
cd /path/to/your-unity-project
vpm add package com.anatawa12.avatar-optimizer
```

## Developers {#developers}

This project is mainly developed by two core developers:

{{< main-dev username="anatawa12" >}}
The initiator of the project and the main maintainer.\
Primarily responsible for implementing new features, fixing bugs, and writing draft documentation.
{{< /main-dev >}}

{{< main-dev username="Sayamame-beans" >}}
The main documentation editor.\
They also contribute to feature planning, collecting user feedback, and promoting the project.
{{< /main-dev >}}

In addition, many contributors have helped improve this project.
Here are some of them.

Thank you all for your contributions!

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
