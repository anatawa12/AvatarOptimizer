---
title: Distributing Prefabs
---

# Distributing Prefabs

Avatar Optimizer mey help you making your assets lightweight and optimized.
Here are some recommendations for how to distribute prefabs that depend on Avatar Optimizer.

## Guide your users to the official distribution of Avatar Optimizer {#guide-to-the-official-distribution}

Since Avatar Optimizer is published under MIT License, it's allowed to redistribute it.
However, I strongly recommend you to guide your users to the official distribution of Avatar Optimizer.
Your distribution of Avatar Optimizer may be outdated. 
If your distribution is outdated, it may have bugs that have already been fixed in the official distribution. 
In addition, since Avatar Optimizer doesn't guarantee forward compatibility,
if your user uses a newer version of Avatar Optimizer than your distribution, this will cause problems.

There are recommend ways to guide your users to the official distribution of Avatar Optimizer.
1. Let users to install Avatar Optimizer by VPM if your asset is distributed with VPM.\
   If your asset is distributed with VPM, declare Avatar Optimizer as a dependency of your asset in your `package.json`
   and guide your users to add the Avatar Optimizer VPM repository to VCC.
   [Here][add-repo] is the link to add the Avatar Optimizer repository to VCC.
   If you want, you can mirror the [Avatar Optimizer repository][repo] in your VPM repository.
    When mirroring, mirror the VPM repository, not from the GitHub Releases.
2. Link to the official documentation of Avatar Optimizer.\
   [This link][official-installation] is the official installation guide.
   The page will guide your users to install Avatar Optimizer in recommended way.
3. Link to [the booth page of Avatar Optimizer][booth-aao]. \
   The booth page is one of the official distribution page of Avatar Optimizer.
   The booth item includes the latest version of VPAI installer unitypackage.
4. Include VPAI installer unitypackage and guide your users to import the unitypackage. \
   The VPAI installer is the tool to install VPM packages like Avatar Optimizer just by importing a unitypackage.
   [Here][vpai] is the link to download the VPAI installer unitypackage for Avatar Optimizer 1.x.x

[add-repo]: https://vpm.anatawa12.com/add-repo
[repo]: https://vpm.anatawa12.com/vpm.json
[official-installation]: https://vpm.anatawa12.com/avatar-optimizer/en/#installation
[vpai]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-%7b%7d-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=1.x.x
[booth-aao]: https://anatawa12.booth.pm/items/4885109
