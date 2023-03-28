# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog].

[Keep a Changelog]: https://keepachangelog.com/en/1.0.0/

## [Unreleased]
### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.2.6-rc.1] - 2023-03-28
### Added
- Internationalization support [`#77`](https://github.com/anatawa12/AvatarOptimizer/pull/77)
  - This adds way to translate editor elements.
  - However, no other language translation than English is not added yet.
  - Please feel free to make PullRequest if you can maintain the translation.

### Fixed
- Remove Empty Renderer Object is not shown on the inspector [`#76`](https://github.com/anatawa12/AvatarOptimizer/pull/76)
- normal vector and tangent vector might not be unit length [`#81`](https://github.com/anatawa12/AvatarOptimizer/pull/81)
  - This can be problem with FreezeBlendShape.

## [0.2.5] - 2023-03-24
### Added
- Show SaveVersion internal property on editor. [`#71`](https://github.com/anatawa12/AvatarOptimizer/pull/71)
  - This makes it easier to make it easier to see prefab overrides

### Changed
- use ExecuteAlways instead of ExecuteInEditMode [`#72`](https://github.com/anatawa12/AvatarOptimizer/pull/72)

### Fixed
- None is added/removed on the prefab modifications [`#73`](https://github.com/anatawa12/AvatarOptimizer/pull/73)
- NullReferenceException in SetCurrentSaveVersion [`#74`](https://github.com/anatawa12/AvatarOptimizer/pull/74)

## [0.2.5-rc.1] - 2023-03-23
### Changed
- reduce unnecessary modification in PrefabSafeSet/List [`#64`](https://github.com/anatawa12/AvatarOptimizer/pull/64)
  - Previously PrefabSafeSet/List will always generates array size change modification.
  - Now, array size change will be generated when added/removed elements from the collection.

### Fixed
- save version is not saved again [`#69`](https://github.com/anatawa12/AvatarOptimizer/pull/69)

## [0.2.4] - 2023-03-22
### Changed
- make accessing v1 error [`#61`](https://github.com/anatawa12/AvatarOptimizer/pull/61)
  - This reduces future mistakes like #59

### Fixed
- RemoveMeshInBox refers old v1 configuration [`#60`](https://github.com/anatawa12/AvatarOptimizer/pull/60)

## [0.2.3] - 2023-03-20
### Fixed
- instantiating material occurs [`#58`](https://github.com/anatawa12/AvatarOptimizer/pull/58)

## [0.2.2] - 2023-03-20
### Added
- Make Children [`#53`](https://github.com/anatawa12/AvatarOptimizer/pull/53)

### Changed
- Do not use cache on applying components now [`#56`](https://github.com/anatawa12/AvatarOptimizer/pull/56)

### Fixed
- NullReferenceException if some component is removed [`#54`](https://github.com/anatawa12/AvatarOptimizer/pull/54)
- save version is not saved. this may break future migration [`#55`](https://github.com/anatawa12/AvatarOptimizer/pull/55)

## [0.2.1] - 2023-03-20
## [0.2.1-beta.1] - 2023-03-20
### Fixed
- Migration failed if some renderer is None [`#49`](https://github.com/anatawa12/AvatarOptimizer/pull/49)

## [0.2.0] - 2023-03-19
## [0.2.0-rc.2] - 2023-03-16
### Added
- Reopening scene after migration [`#47`](https://github.com/anatawa12/AvatarOptimizer/pull/47)

### Fixed
- unnecessary logs on migration [`#38`](https://github.com/anatawa12/AvatarOptimizer/pull/38)
- We may forget checking components on disable objects [`#46`](https://github.com/anatawa12/AvatarOptimizer/pull/46)

## [0.2.0-rc.1] - 2023-03-12
### Fixed
- SkinnedMeshRenderers without bones will break mesh [`#35`](https://github.com/anatawa12/AvatarOptimizer/pull/35)
- Reference to components in prefab asset will remain [`#37`](https://github.com/anatawa12/AvatarOptimizer/pull/37)

## [0.2.0-beta.2] - 2023-03-04
### Added
- Forge Migration [`#31`](https://github.com/anatawa12/AvatarOptimizer/pull/31)
  - With this feature, you can re-migrate everything but you may lost changes you made in `v0.2`

### Fixed
- Migration of PrefabSafeSet prefab overrides is not well [`#29`](https://github.com/anatawa12/AvatarOptimizer/pull/29)
- Fixed IndexOutOfError if there are more bones than bindposes [`#30`](https://github.com/anatawa12/AvatarOptimizer/pull/30)

## [0.2.0-beta.1] - 2023-03-03
### Added
- Support for Prerelease in publish system [`#19`](https://github.com/anatawa12/AvatarOptimizer/pull/19)
- Changelogs (including ones for traditional releases) [`#19`](https://github.com/anatawa12/AvatarOptimizer/pull/19)
- Auto Test [`#23`](https://github.com/anatawa12/AvatarOptimizer/pull/23)
- Prefab support [`#11`](https://github.com/anatawa12/AvatarOptimizer/pull/11)

### Changed
- **BREAKING** Save format for many components [`#11`](https://github.com/anatawa12/AvatarOptimizer/pull/11)
  - Even if you added more elements than before on prefab, added elements on prefab instance will be kept.
  - In previous implementation (unity default array prefab overrides implementation), can be broken easily.
- **BREAKING** All materials are merged by default [`#11`](https://github.com/anatawa12/AvatarOptimizer/pull/11)
  - Due to save format migration, every materials will be marked as merged.
  - If you have some materials not to be merged, please re-reconfigure that.

## [0.1.4]
### Added
- Support for feature Migration [`be0147b`](https://github.com/anatawa12/AvatarOptimizer/commit/be0147bb783cb9ecb5f7193c360f9d1483853e33)

### Changed
- Box editor of RemoveMeshInBox [`15fc931`](https://github.com/anatawa12/AvatarOptimizer/commit/15fc931fed401ba62bba7c2ad51a11bf69e3d044)
- Installer unitypackage name [`b32167f`](https://github.com/anatawa12/AvatarOptimizer/commit/b32167fbacb6f2e6539c3d9c9d02dbde7ac3147e)
- Warn if MergeSkinnedMesh is with SkinnedMeshRenderer with Mesh [`1016aa6`](https://github.com/anatawa12/AvatarOptimizer/commit/1016aa6cd3a70e34bff093f84eca0386c867ce33)

### Fixed
- MergeToonLit is always marked as dirty [`82ba212`](https://github.com/anatawa12/AvatarOptimizer/commit/82ba2126f5c5f53fc0d804fe2387731af27d9471)
- RemoveMeshInBox does not handle bone correctly [`b2fea4f`](https://github.com/anatawa12/AvatarOptimizer/commit/b2fea4fa6c8c6fd99eabb1773a187a427ac66216)

## [0.1.3]
### Fixed
- Merge ToonLit error [`d0f1ef2`](https://github.com/anatawa12/AvatarOptimizer/commit/d0f1ef213919a0a20b280ab19c36371cfe3509d4)
- NRE if some bone is null [`88efc2a`](https://github.com/anatawa12/AvatarOptimizer/commit/88efc2aed4da135e5ac9850c40720807734fd3f3)

## [0.1.2]
### Fixed
- FreezeBlendShape behaviour [`0cebf27`](https://github.com/anatawa12/AvatarOptimizer/commit/0cebf27e6aeb39ead464745c649a350ae8bb7726)

## [0.1.1]
### Added
- (internal) MeshInfo2 system for speed up [`36716dd`](https://github.com/anatawa12/AvatarOptimizer/commit/36716ddb27bf987d0a993b40f4a03f6fc1f79988)
- RemoveMeshInBox component [`96f3526`](https://github.com/anatawa12/AvatarOptimizer/commit/96f35265c0b4cdebc38ed634e58cd1a5490ec5a5)

### Changed
- Make VPAI installer unitypackage install 0.1.x [`6615991`](https://github.com/anatawa12/AvatarOptimizer/commit/66159914b7f609851dcb18971ad34187f9b7c1d9)
- Editor of FreezeBlendShape [`cdd2031`](https://github.com/anatawa12/AvatarOptimizer/commit/cdd20317c449641dc1607780bdba8bab598b8b99)

### Fixed
- Several bugs

## [0.1.0] - 2023-01-16
### Changed
- Move components from `Anatawa12/` to `Optimizer/` [`949d267`](https://github.com/anatawa12/AvatarOptimizer/commit/949d267dcf164ff0762f2314457e7a54ed3c6432)

### Fixed
- Several bugs

## [0.0.2] - 2023-01-15
### Added
- Build Badges onto README [`57614ac`](https://github.com/anatawa12/AvatarOptimizer/commit/57614ac8dfd5b375966fb06dafd8dd99b10ce792)
- How to build onto README [`2ad57cc`](https://github.com/anatawa12/AvatarOptimizer/commit/2ad57cc74799f9b26a1028af1f31ad7199fd81f4)
- Merge ToonLit Material [`a460e3f`](https://github.com/anatawa12/AvatarOptimizer/commit/a460e3f0c9c70e302d0419fcd474240ed1d5624b)

### Fixed
- FreezeBlendShape remains [`95c0d43`](https://github.com/anatawa12/AvatarOptimizer/commit/95c0d4352c8641a7e2cfd1a9f3c8d2c5b9424d42)

## [0.0.1] - 2023-01-13
### Added
- Merge Skinned Mesh
- Merge PhysBone
- Freeze BlendShape
- Merge Bone
- Clear Endpoint Position

[Unreleased]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.6-rc.1...HEAD
[0.2.6-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.5...v0.2.6-rc.1
[0.2.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.5-rc.1...v0.2.5
[0.2.5-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.4...v0.2.5-rc.1
[0.2.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.3...v0.2.4
[0.2.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.2...v0.2.3
[0.2.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.1...v0.2.2
[0.2.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.1-beta.1...v0.2.1
[0.2.1-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.0...v0.2.1-beta.1
[0.2.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.0-rc.2...v0.2.0
[0.2.0-rc.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.0-rc.1...v0.2.0-rc.2
[0.2.0-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.0-beta.2...v0.2.0-rc.1
[0.2.0-beta.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.0-beta.1...v0.2.0-beta.2
[0.2.0-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.4...v0.2.0-beta.1
[0.1.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.3...v0.1.4
[0.1.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.2...v0.1.3
[0.1.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.0.2...v0.1.0
[0.0.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.0.1...v0.0.2
[0.0.1]: https://github.com/anatawa12/AvatarOptimizer/releases/tag/v0.0.1
