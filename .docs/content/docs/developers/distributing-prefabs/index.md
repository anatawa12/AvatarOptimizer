---
title: Distributing Prefabs
---

# Distributing Prefabs

Avatar Optimizer mey help you making your assets lightweight and optimized.
Here are some recommendations for how to distribute prefabs that depend on Avatar Optimizer.

## Guide your users to the official distribution of Avatar Optimizer {#guide-to-the-official-distribution}

Since Avatar Optimizer is published under MIT License, redistribution is allowed.
However, I strongly recommend you to guide your users to the official distribution of Avatar Optimizer.\
Your unofficial distribution of Avatar Optimizer may be outdated. 
If users use outdated version, they may encounter bugs that have already been fixed in the official distribution. 
In addition, since Avatar Optimizer doesn't guarantee forward compatibility,
if your users use a newer version of Avatar Optimizer than your unofficial distribution and downgrade it to outdated version, some problems will be caused.

There are recommended ways to guide your users to the official distribution of Avatar Optimizer.
1. If your asset is distributed via VPM, let users install Avatar Optimizer via VPM as well.\
   Declare Avatar Optimizer as a dependency of your asset in your `package.json`
   and guide your users to add the Avatar Optimizer VPM repository to ALCOM or VCC.
   [Here][add-repo] is the link to add the Avatar Optimizer Official repository to ALCOM or VCC.\
   If you want, you can mirror the [Avatar Optimizer repository][repo] in your VPM repository.
   When mirroring, mirror the VPM repository, not from the GitHub Releases.
2. Link to the official documentation of Avatar Optimizer.\
   [This link][official-installation] is the official installation guide of Avatar Optimizer.
   The page will guide your users to install Avatar Optimizer in recommended way.
3. Link to [the Booth page of Avatar Optimizer][booth-aao].\
   The Booth page is one of the official distribution page of Avatar Optimizer.
   In the Booth page, the latest version of VPAI installer unitypackage is provided.
4. Include VPAI installer unitypackage and guide your users to import the unitypackage.\
   The VPAI installer is the tool to install VPM packages like Avatar Optimizer just by importing a unitypackage.
   [Here][vpai] is the link to download the VPAI installer unitypackage for Avatar Optimizer 1.x.x .

[add-repo]: https://vpm.anatawa12.com/add-repo
[repo]: https://vpm.anatawa12.com/vpm.json
[official-installation]: https://vpm.anatawa12.com/avatar-optimizer/en/#installation
[vpai]: https://api.anatawa12.com/create-vpai/?name=AvatarOptimizer-%7b%7d-installer.unitypackage&repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=1.x.x
[booth-aao]: https://anatawa12.booth.pm/items/4885109
