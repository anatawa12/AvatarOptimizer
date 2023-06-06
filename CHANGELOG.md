# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog].

[Keep a Changelog]: https://keepachangelog.com/en/1.1.0/

## [Unreleased]
### Added

### Changed

### Deprecated

### Removed

### Fixed
- Error in MergeSkinnedMeshProcessor with RecordMoveProperty `#214`

### Security

## [0.4.4] - 2023-06-04
### Changed
- Make `Remove Empty Renderer Object` enabled by default [`#208`](https://github.com/anatawa12/AvatarOptimizer/pull/208)

## [0.4.3] - 2023-06-02
### Added
- Adding multiple values to PrefabSafeSet [`#200`](https://github.com/anatawa12/AvatarOptimizer/pull/200)
  - See [this video](https://github.com/anatawa12/AvatarOptimizer/issues/128#issuecomment-1568540903) for more details.
- Overriden PrefabSafeSet properties are now highlighted as blue and bold [`#200`](https://github.com/anatawa12/AvatarOptimizer/pull/200)

### Fixed
- Error when we removed some modification in PrefabSafeSet [`#201`](https://github.com/anatawa12/AvatarOptimizer/pull/201)
  - There are several situations for this problem:
    - When we removed value in original component
    - When we removed new value in overrides
    - When we reverted added twice in overrides
    - When we reverted deletion in overrides
    - When we reverted fake deletion in overrides
- Error when we reverted whole PrefabSafeSet with modifications [`#201`](https://github.com/anatawa12/AvatarOptimizer/pull/201)

## [0.4.2] - 2023-05-30
### Fixed
- MergeSkinnedMesh depends on other EditSkinnedMesh components does not working [`#195`](https://github.com/anatawa12/AvatarOptimizer/pull/195)
- Error with removed modified property in PrefabSafeSet Editor [`#196`](https://github.com/anatawa12/AvatarOptimizer/pull/196)
- Apply on Play may not work [`#198`](https://github.com/anatawa12/AvatarOptimizer/pull/198)

## [0.4.1] - 2023-05-23
### Changed
- Reimplemented Animation Mapping System Completely [`#168`](https://github.com/anatawa12/AvatarOptimizer/pull/168)
  - This should fixes problem with objects/components at same place.
- Improve Animation Mapping System [`#172`](https://github.com/anatawa12/AvatarOptimizer/pull/172)
  - This should reduce build time
- Disable animating `m_Enabled` of source SkinnedMeshRenderer [`#190`](https://github.com/anatawa12/AvatarOptimizer/pull/190)
  - Animating `m_Enabled` of source SkinnedMeshRenderer now doesn't affects merged SkinnedMeshRenderer
  - If you actually want to enable/disable merged SkinnedMeshRenderer, 
    animate `m_Enabled` of merged SkinnedMeshRenderer instead.

### Fixed
- Error is not cleared on build [`#170`](https://github.com/anatawa12/AvatarOptimizer/pull/170)
- Merge PhysBone is not working [`#177`](https://github.com/anatawa12/AvatarOptimizer/pull/177)
  - Previously, values are not copied correctly
- The help box for description of components without description were shown [`#178`](https://github.com/anatawa12/AvatarOptimizer/pull/178)
- Name of Is Animated and Parameter field are not correct [`#183`](https://github.com/anatawa12/AvatarOptimizer/pull/183)
- We cannot set override setting of Colliders to Copy [`#183`](https://github.com/anatawa12/AvatarOptimizer/pull/183)
- Error with MergeToonLit [`#185`](https://github.com/anatawa12/AvatarOptimizer/pull/185)
- Poor word choice in Japanese Translation [`#174`](https://github.com/anatawa12/AvatarOptimizer/pull/174)
- Localization is not applied for some fields [`#186`](https://github.com/anatawa12/AvatarOptimizer/pull/186)

## [0.4.0] - 2023-05-20
### Added
- Japanese Translation: 日本語化 [`#152`](https://github.com/anatawa12/AvatarOptimizer/pull/152)

### Changed
- Save format for MergePhysBone [`#166`](https://github.com/anatawa12/AvatarOptimizer/pull/166)
  - Previously used backed PhysBone component for override values are removed.
  - There are no changes in behaviour. Just migrate your assets.

### Removed
- Delete GameObject feature [`#153`](https://github.com/anatawa12/AvatarOptimizer/pull/153)
  - Use `EditorOnly` tag instead

### Fixed
- Error with Animator at non-root GameObject [`#164`](https://github.com/anatawa12/AvatarOptimizer/pull/164)
- Error with copied MergePhysBone component [`#165`](https://github.com/anatawa12/AvatarOptimizer/pull/165)

## [0.3.5] - 2023-05-15
### Changed
- Internal Errors not relates to any Object are now reported [`#160`](https://github.com/anatawa12/AvatarOptimizer/pull/160)

### Fixed
- Error if there are multiple GameObjects with same path [`#159`](https://github.com/anatawa12/AvatarOptimizer/pull/159)

## [0.3.4] - 2023-05-15
### Fixed
- Reference to Component will become None [`#156`](https://github.com/anatawa12/AvatarOptimizer/pull/156)
- BlendShapes for Eyelids can be broken with FreezeBlendShape [`#154`](https://github.com/anatawa12/AvatarOptimizer/pull/154)

## [0.3.3] - 2023-05-14
## [0.3.2] - 2023-05-14
### Added
- Error Reporting System [`#124`](https://github.com/anatawa12/AvatarOptimizer/pull/124)
  - This adds window shows errors on build
  - This is based on Modular Avatar's Error Reporting Window. thanks `@bdunderscore` 
- Website for AvatarOptimizer [`#139`](https://github.com/anatawa12/AvatarOptimizer/pull/139)
  - Available at <https://vpm.anatawa12.com/avatar-optimizer/>
- Manual Bake Avatar [`#147`](https://github.com/anatawa12/AvatarOptimizer/pull/147)
  - Left click the avatar and click `[AvatarOptimizer] Manual Bake Avatar`

### Changed
- Improved & reimplemented Animation (re)generation system [`#137`](https://github.com/anatawa12/AvatarOptimizer/pull/137)
  - This is completely internal changes. Should not break your project
  - In previous implementation, animations for GameObjects moved by MergeBone, MergePhysBone or else doesn't work well
  - This reimplementation should fix this problem

### Fixed
- Migration fails with scenes/prefabs in read-only packages [`#136`](https://github.com/anatawa12/AvatarOptimizer/pull/136)
  - Now, migration process doesn't see any scenes/prefabs in read-only packages.

## [0.3.1] - 2023-05-05
### Fixed
- Can't remove SkinnedMeshRenderer error [`#133`](https://github.com/anatawa12/AvatarOptimizer/pull/133)
  - This error should do nothing bad but it confuses everyone
- Bad behaviour with VRCFury on build [`#134`](https://github.com/anatawa12/AvatarOptimizer/pull/134)

## [0.3.0] - 2023-05-04
### Added
- Make Children of Me [`#100`](https://github.com/anatawa12/AvatarOptimizer/pull/100)
  - As a alternative of feature removal in same pull request
- UnusedBonesByReferencesTool [`#112`](https://github.com/anatawa12/AvatarOptimizer/pull/112)
  - This is port of [UnusedBonesByReferencesTool by Narazaka][UnusedBonesByReferencesTool]
- Support for VRCSDK 3.2.0! [`#117`](https://github.com/anatawa12/AvatarOptimizer/pull/117)
  - This includes support for PhysBone Versions and PhysBone 1.1

[UnusedBonesByReferencesTool]: https://narazaka.booth.pm/items/3831781

### Changed
- Use IEditorOnly instead of mokeypatching VRCSDK [`#102`](https://github.com/anatawa12/AvatarOptimizer/pull/102)
- Move the toggle for Override and the setting of the value after Override closer together. [`#105`](https://github.com/anatawa12/AvatarOptimizer/pull/105)
  - With this changes, the merged PhysBone is now hidden.
  - The merged PhysBone will be shown in Play mode.
- Now we can Copy (instead of Override) `Pull`, `Gravity`, `Immobile` properties even if `Integration Type` is overriden. [`#105`](https://github.com/anatawa12/AvatarOptimizer/pull/105)
  - During migration, if `Integration Type` (previously called `Force`) is configured to be Override, `Pull`, `Gravity`, `Immobile` will be configured to be Override.
  - This is **BREAKING** changes.
- Now we can Copy / Override `Immobile Type` and `Immobile` (strength) separately. [`#105`](https://github.com/anatawa12/AvatarOptimizer/pull/105)
  - Previously, if you override `Immobile Type`, you also required to override `Immobile` but no longer required.
  - This is **BREAKING** changes in the semantics of `immobile` property.
- Upgrade CL4EE to 1.0.0 [`#121`](https://github.com/anatawa12/AvatarOptimizer/pull/121)

### Removed
- **BREAKING** Removed Prefab Safe List [`#95`](https://github.com/anatawa12/AvatarOptimizer/pull/95)
- **BREAKING** Removed RootTransform feature from MergePhysBone [`#100`](https://github.com/anatawa12/AvatarOptimizer/pull/100)
  - See [this issue comment][about-root-transform] for more datails.
- Removed support for VRCSDK 3.1.x. [`#117`](https://github.com/anatawa12/AvatarOptimizer/pull/117)
  - Dropping VRCSDK support is a **BREAKING** changes.
  - However, we may drop old VRCSDK support in the minor releases of AvatarOptimizer in the feature.
  - In the other hand, we promise we'll never drop old VRCSDK support in the patch releases.
  - Notice: in the 0.x.y release, y is a minor releases in this project.

[about-root-transform]: https://github.com/anatawa12/AvatarOptimizer/issues/62#issuecomment-1512586282

### Fixed
- ShouldIgnoreComponentPatch cases compilation error [`#108`](https://github.com/anatawa12/AvatarOptimizer/pull/108)
- Merge PhysBone is working even if parents are differ [`#129`](https://github.com/anatawa12/AvatarOptimizer/pull/129)

## [0.2.8] - 2023-04-19
### Fixed
- NullReferenceException with prefabs in editor for PrefabSafeSet [`#92`](https://github.com/anatawa12/AvatarOptimizer/pull/92)

## [0.2.7] - 2023-04-01
### Added
- Support for VRCSDK 3.1.12 and 3.1.13

## [0.2.6] - 2023-03-31
### Added
- Internationalization support [`#77`](https://github.com/anatawa12/AvatarOptimizer/pull/77)
  - This adds way to translate editor elements. 
  - However, no other language translation than English is not added yet. 
  - Please feel free to make PullRequest if you can maintain the translation.

### Fixed
- Remove Empty Renderer Object is not shown on the inspector [`#76`](https://github.com/anatawa12/AvatarOptimizer/pull/76)
- normal vector and tangent vector might not be unit length [`#81`](https://github.com/anatawa12/AvatarOptimizer/pull/81)
  - This can be problem with FreezeBlendShape.
- Assertion does not work well [`#85`](https://github.com/anatawa12/AvatarOptimizer/pull/85)
  - This can make invalid mesh
- Mesh is broken if more than 65536 vertices are exists [`#87`](https://github.com/anatawa12/AvatarOptimizer/pull/87)
  - Because we didn't check for vertices count and index format, vertex index can be overflow before. 
- Generated assets are invisible for a while [`#88`](https://github.com/anatawa12/AvatarOptimizer/pull/88)

## [0.2.5] - 2023-03-24
### Added
- Show SaveVersion internal property on editor. [`#71`](https://github.com/anatawa12/AvatarOptimizer/pull/71)
  - This makes it easier to make it easier to see prefab overrides

### Changed
- reduce unnecessary modification in PrefabSafeSet/List [`#64`](https://github.com/anatawa12/AvatarOptimizer/pull/64)
  - Previously PrefabSafeSet/List will always generates array size change modification.
  - Now, array size change will be generated when added/removed elements from the collection.
- use ExecuteAlways instead of ExecuteInEditMode [`#72`](https://github.com/anatawa12/AvatarOptimizer/pull/72)

### Fixed
- save version is not saved again [`#69`](https://github.com/anatawa12/AvatarOptimizer/pull/69)
- None is added/removed on the prefab modifications [`#73`](https://github.com/anatawa12/AvatarOptimizer/pull/73)

## [0.2.4] - 2023-03-22
### Changed
- make Accessing v1 error [`#61`](https://github.com/anatawa12/AvatarOptimizer/pull/61)
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
### Fixed
- Migration failed if some renderer is None [`#49`](https://github.com/anatawa12/AvatarOptimizer/pull/49)

## [0.2.0] - 2023-03-19
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

### Fixed
- Fixed IndexOutOfError if there are more bones than bindposes [`#30`](https://github.com/anatawa12/AvatarOptimizer/pull/30)
- SkinnedMeshRenderers without bones will break mesh [`#35`](https://github.com/anatawa12/AvatarOptimizer/pull/35)
- We may forget checking components on disable objects [`#46`](https://github.com/anatawa12/AvatarOptimizer/pull/46)

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

[Unreleased]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.4...HEAD
[0.4.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.3...v0.4.4
[0.4.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.2...v0.4.3
[0.4.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.1...v0.4.2
[0.4.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.0...v0.4.1
[0.4.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.5...v0.4.0
[0.3.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.4...v0.3.5
[0.3.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.3...v0.3.4
[0.3.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.2...v0.3.3
[0.3.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.1...v0.3.2
[0.3.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.8...v0.3.0
[0.2.8]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.7...v0.2.8
[0.2.7]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.6...v0.2.7
[0.2.6]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.5...v0.2.6
[0.2.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.4...v0.2.5
[0.2.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.3...v0.2.4
[0.2.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.2...v0.2.3
[0.2.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.1...v0.2.2
[0.2.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.4...v0.2.0
[0.1.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.3...v0.1.4
[0.1.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.2...v0.1.3
[0.1.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.0.2...v0.1.0
[0.0.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.0.1...v0.0.2
[0.0.1]: https://github.com/anatawa12/AvatarOptimizer/releases/tag/v0.0.1
