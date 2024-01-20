Avatar Optimizer
====

[![GitHub release][shields-latest-release]][github-latest]
[![GitHub deployments][shields-deployment-master]][github-latest]
[![GitHub deployments][shields-deployment-vpm]][vpm-repository]
[![VPM release][shields-vpm]][vpm-repository]

[shields-latest-release]: https://img.shields.io/github/v/release/anatawa12/AvatarOptimizer?display_name=tag&sort=semver
[shields-deployment-vpm]: https://img.shields.io/github/deployments/anatawa12/AvatarOptimizer/vpm.anatawa12.com?label=VPM%20Deployment
[shields-deployment-master]: https://img.shields.io/github/deployments/anatawa12/AvatarOptimizer/master%20branch?label=Deployment
[shields-vpm]: https://img.shields.io/vpm/v/com.anatawa12.avatar-optimizer?repository_url=https%3A%2F%2Fvpm.anatawa12.com%2Fvpm.json

Set of Anatawa12's Non-Destructive Small Avatar Optimization Utilities.
Those utilies are applied on entering play mode or building VRC Avatars.

If you have problems or questions about this tool, please feel free to contact me on [twitter][twitter] or [Misskey (Activity Pub)][misskey]!

For more information, check out the [documentation].

---

anatawa12によるアバター軽量化用のちょっとした非破壊ユーティリティ群です。
これらのユーティリティはPlayモードに入るときかアバターをビルドするときに適用されます。

本ツールについて不具合や不明な点などがございましたら、[twitter][twitter]や[Misskey (Activity Pub)][misskey]からお気軽にご連絡ください!

詳細情報については[documentation][documentation-ja]をご覧ください。

[documentation]: https://vpm.anatawa12.com/avatar-optimizer/en/
[documentation-ja]: https://vpm.anatawa12.com/avatar-optimizer/ja/

[twitter]: https://go.anatawa12.com/twitter.vrchat
[misskey]: https://go.anatawa12.com/misskey.vrchat
[vpm-repository]: https://vpm.anatawa12.com/
[github-latest]: https://github.com/anatawa12/AvatarOptimizer/releases/latest

## API Stability Note

Almost all parts of this project has no stable public APIs assembly for now.

The only stable public API is in `com.anatawa12.avatar-optimizer.api.editor` assembly.
For this assembly, The Semantic Versioning is applied to the API.

For other parts of the Avatar Optimizer, the Semantic Versioning will be applied to save format of the components, 
but not for scripting usage for now.  Even if any parts of this project is exposed as public in C# code,
it can be changed / removed in future release, especially for assemblies in the `Internal` folder.

In addition, features only for debugging the components (e.g. Advanced Options on the Trace and Optimize) 
are not stable, might be changed in minor / patch versions.
