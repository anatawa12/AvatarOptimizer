---
title: Introduction
type: docs
---

# Avatar Optimizer

Set of Anatawa12's Small Avatar Optimization Utilities.
Those utilities are applied on entering play mode or building VRC Avatars.

## Installation {#installation}

Avatar Optimizer is published with [VPM][vpm] repository so you can install this package using any vpm clients.

### With VCC (Recommended) {#installation-vcc}

1. Click [this link][VCC-add-repo-link] to add anatawa12's repository.
2. Add Avatar Optimizer from VCC.

[vcc-bug]: https://github.com/vrchat-community/creator-companion/issues/252#issuecomment-1513381955

### Using Installer UnityPackage with VPM {#installation-vpai}

With [VPAI] You can install this tool with just importing one unitypackage.

1. download installer unitypackage [here][installer unitypackage].
2. Import the unitypackage into your project.

<details>
<summary>Installer for other versions</summary>

- [0.1.x][installer unitypackage 0.1.x]
- [0.2.x][installer unitypackage 0.2.x]
- [0.x.x including beta releases][installer unitypackage 0.x beta]

[installer unitypackage 0.2.x]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=0.2.x
[installer unitypackage 0.1.x]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=0.1.x
[installer unitypackage 0.x beta]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-beta-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=0.x.x&prerelease

</details>

[installer unitypackage]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=0.3.x

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

[VPAI]: https://github.com/anatawa12/VPMPackageAutoInstaller
[vpm]: https://vcc.docs.vrchat.com/vpm/
[vcc-cli]: https://vcc.docs.vrchat.com/vpm/cli
[vrc-get]: https://github.com/anatawa12/vrc-get
[VCC-add-repo-link]: https://vpm.anatawa12.com/add-repo
