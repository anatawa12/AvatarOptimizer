Avatar Optimizer
====

[![GitHub release][shields-latest-release]][github-latest]
[![GitHub deployments][shields-deployment-master]][github-latest]
[![GitHub deployments][shields-deployment-vpm]][vpm-repository]
[![VPM release][shields-vpm]][vpm-repository]
[![discord link][shields-discord]][ndmf-discord]

[shields-latest-release]: https://img.shields.io/github/v/release/anatawa12/AvatarOptimizer?display_name=tag&sort=semver
[shields-deployment-vpm]: https://img.shields.io/github/deployments/anatawa12/AvatarOptimizer/vpm.anatawa12.com?label=VPM%20Deployment
[shields-deployment-master]: https://img.shields.io/github/deployments/anatawa12/AvatarOptimizer/master%20branch?label=Deployment
[shields-vpm]: https://img.shields.io/vpm/v/com.anatawa12.avatar-optimizer?repository_url=https%3A%2F%2Fvpm.anatawa12.com%2Fvpm.json
[shields-discord]: https://img.shields.io/badge/chat_on-NDMF_Discord-5865F2?logo=discord&logoColor=white
[ndmf-discord]: https://discord.gg/dV4cVpewmM

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

This project uses [Semantic Versioning 2.0.0](https://semver.org/spec/v2.0.0.html).
As section 1 of the specification requires, this part of README defines what is the public API of AAO.

### Stability for scripting usage

First, most of the assemblies in this project are not part of the public API, especially for internal assemblies.
Public API is included in the assemblies `com.anatawa12.avatar-optimizer.api.editor` and `com.anatawa12.avatar-optimizer.runtime`.

In those assemblies, most members exposed to other assemblies in the C# code are part of the public API,
but the members in the namespaces includes `Internal` in the name are not.
For example, members in `Anatawa12.AvatarOptimizer.APIInternal` namespace are not part of the public API.

In addition, adding components of AAO needs extra care since default behavior of the components might be changed as described below.
You have to call `void Initialize(int version)` with the configuration version you want to use just after adding the component (calling `AddComponent`) to guarantee expected behaviour.
`Initialize` will rollback the configuration to the version you specified if the current configuration version is newer than the version you specified.
The current configuration version for the component can be retrieved with documentation of the `Initialize` method.

If the component doesn't support configuring by script, the `Initialize` method will not be provided.
What script can do is adding components with the default configuration.

### Stability for save format

The Semantic Versioning will also be applied to save format of the most components.

For patch versions, the save format will not be changed. In other words, forwards compatibility in the same minor version is guaranteed.\
For minor versions, the save format might be changed in a backwards compatible way.\
For major versions, the save format might not be compatible. There are no guarantees for v2.0.0 or later.

There are several exceptions and important notes for stability of save format.
- The features only for debugging the components are not guaranteed to follow the rules above.\
  For example, Debug Options on the Trace and Optimize might be changed in any version.
- The features marked as experimental are not guaranteed to follow the rules above.
- The behavior of `Trace and Optimize` component might be changed by implementing new optimization.
  However, the default settings of `Trace and Optimize` component will never change the behavior of your avatar, so changes must not affect the avatar.
  (If your avatar behavior is changed by the `Trace and Optimize` component, please report it as a bug.)
- The behavior of the components just after adding components or resetting components is not part of the stable save format.
  The default settings of the components might be changed, but it will never change existing / already added components behavior.

### Other notes for Versioning

AAO is a tool on Unity and mostly depends on VRChat SDK.

AAO will update minimum version of VRChat SDK or Unity in minor version of AAO.\
AAO will add support for newer version of VRChat SDK or Unity in patch release of AAO.

Dropping older version of VRChat SDK or Unity will be documented in the release notes.
