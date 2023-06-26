---
title: Introduction
type: docs
---

# Avatar Optimizer

Set of Anatawa12's non-Destructive Small Avatar Optimization Utilities.
Those utilities are applied on entering play mode or building VRC Avatars.

Avatar Optimizer is a Open Source Software developed on [GitHub] published under the [MIT License].

[GitHub]: https://github.com/anatawa12/AvatarOptimizer
[MIT License]: https://github.com/anatawa12/AvatarOptimizer/blob/HEAD/LICENSE

## Installation {#installation}

Avatar Optimizer is published with [VPM][vpm] repository so you can install this package using any vpm clients.

### With VCC (Recommended) {#installation-vcc}

1. Click [this link][VCC-add-repo-link] to add anatawa12's repository.
2. Add Avatar Optimizer from VCC.

### Using Installer UnityPackage with VPAI {#installation-vpai}

With [VPAI] You can install this tool with just importing one unitypackage.

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

[installer unitypackage 1.x.x]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=1.x.x
[installer unitypackage 0.4.x]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=0.4.x
[installer unitypackage 0.3.x]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=0.3.x
[installer unitypackage 0.2.x]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=0.2.x
[installer unitypackage 0.1.x]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=0.1.x
[installer unitypackage x.x beta]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-{}-beta-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=x.x.x&prerelease
