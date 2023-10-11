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
- AnimatorOverrideController may not be proceed correctly `#567`
- Unclear behaviour if we merged meshes with and normals without that `#569`

### Security

## [1.5.3-beta.1] - 2023-10-10
### Fixed
- AnimatorController with Synced can be broken [`#564`](https://github.com/anatawa12/AvatarOptimizer/pull/564)

## [1.5.2] - 2023-10-10
## [1.5.2-beta.3] - 2023-10-10
### Fixed
- New version notice remains after updating AAO without restarting UnityEditor [`#559`](https://github.com/anatawa12/AvatarOptimizer/pull/559)
- Freeze BlendShape may break Visame with MergeSkinnedMesh [`#561`](https://github.com/anatawa12/AvatarOptimizer/pull/561)

## [1.5.2-beta.2] - 2023-10-10
### Added
- More MMD BlendShapes are registered [`#552`](https://github.com/anatawa12/AvatarOptimizer/pull/552)
  - New English Translation BlendShapes are compatible with AAO!
- Check for update [`#554`](https://github.com/anatawa12/AvatarOptimizer/pull/554)

### Changed
- You now cannot key any of AvatarOptimizer Components [`#551`](https://github.com/anatawa12/AvatarOptimizer/pull/551)
  - Previously you can key AvatarOptimizer Coponent but it was meaningless.

### Fixed
- GC Debug doesn't include inactive objects [`#546`](https://github.com/anatawa12/AvatarOptimizer/pull/546)
- EditMode Preview of RemoveMeshInBox is not correct [`#550`](https://github.com/anatawa12/AvatarOptimizer/pull/550)
- Avatar Standard Colliders can be removed [`#553`](https://github.com/anatawa12/AvatarOptimizer/pull/553)

## [1.5.2-beta.1] - 2023-10-09
### Added
- Feature for debugging GC Objects [`#543`](https://github.com/anatawa12/AvatarOptimizer/pull/543)

## [1.5.1] - 2023-10-08
## [1.5.1-beta.1] - 2023-10-08
### Fixed
- MergePhysBone component may be shown as unknown components [`#541`](https://github.com/anatawa12/AvatarOptimizer/pull/541)
- MergeBone may break Fur [`#542`](https://github.com/anatawa12/AvatarOptimizer/pull/542)

## [1.5.0] - 2023-10-07
## [1.5.0-rc.13] - 2023-10-07
### Changed
- Change Japanese Translation of "BlendShape" [`#535`](https://github.com/anatawa12/AvatarOptimizer/pull/535)

## [1.5.0-rc.12] - 2023-10-07
## [1.5.0-rc.11] - 2023-10-05
### Fixed
- Viseme may be broken [`#527`](https://github.com/anatawa12/AvatarOptimizer/pull/527)

## [1.5.0-rc.10] - 2023-10-03
### Added
- Significant Performance Improvements with small code changes [`#523`](https://github.com/anatawa12/AvatarOptimizer/pull/523)

## [1.5.0-rc.9] - 2023-09-28
### Fixed
- Editor of EditSkinnedMesh components may not work well if the object is inactive [`#518`](https://github.com/anatawa12/AvatarOptimizer/pull/518)

## [1.5.0-rc.8] - 2023-09-25
### Added
- Warning Dialog for Legacy Modular Avatar [`#509`](https://github.com/anatawa12/AvatarOptimizer/pull/509)

### Changed
- Internal: ErrorReporting is now on NDMF [`#511`](https://github.com/anatawa12/AvatarOptimizer/pull/511)
- Declare compatible with VRCSDK 3.4.x [`#513`](https://github.com/anatawa12/AvatarOptimizer/pull/513)

### Fixed
- (legacy) Animation Component Disappears [`#512`](https://github.com/anatawa12/AvatarOptimizer/pull/512)
- VRC_SpatialAudioSource Disappears [`#512`](https://github.com/anatawa12/AvatarOptimizer/pull/512)
- Unknown component warning for AvatarActivator of NDMF [`#512`](https://github.com/anatawa12/AvatarOptimizer/pull/512)
- Avoid problematic material slot in MergeSkinnedMesh [`#508`](https://github.com/anatawa12/AvatarOptimizer/pull/508)
  - This avoids [Unity's bug in 2019][unity-bug-material]. In Unity 2022, this is no longer needed.

[unity-bug-material]: https://issuetracker.unity3d.com/issues/material-is-applied-to-two-slots-when-applying-material-to-a-single-slot-while-recording-animation

## [1.5.0-rc.7] - 2023-09-24
### Added
- Support for [NDMF](https://ndmf.nadena.dev) integration [`#375`](https://github.com/anatawa12/AvatarOptimizer/pull/375)

### Removed
- internal ApplyOnPlay framework [`#504`](https://github.com/anatawa12/AvatarOptimizer/pull/504)

## [1.5.0-rc.6] - 2023-09-24
### Fixed
- AvatarMask broken with many cases [`#502`](https://github.com/anatawa12/AvatarOptimizer/pull/502)

## [1.5.0-rc.5] - 2023-09-23
### Added
- Full EditMode Preview of RemoveMesh Components [`#500`](https://github.com/anatawa12/AvatarOptimizer/pull/500)

### Fixed
- Remove unused bone references [`#498`](https://github.com/anatawa12/AvatarOptimizer/pull/498)
- MergeBone will loose some other transform information with extreamly small parent scale [`#499`](https://github.com/anatawa12/AvatarOptimizer/pull/499)

## [1.5.0-rc.4] - 2023-09-21
### Fixed
- Left eye disappears [`#493`](https://github.com/anatawa12/AvatarOptimizer/pull/493)
- MergeBone will loose transform information with extreamly small parent scale [`#495`](https://github.com/anatawa12/AvatarOptimizer/pull/495)
- Manually configured MergeBone is removed / disabled by GC Objects [`#496`](https://github.com/anatawa12/AvatarOptimizer/pull/496)

## [1.5.0-rc.3] - 2023-09-19
### Fixed
- Eyelid bones disappears with Automatic MergeBone [`#487`](https://github.com/anatawa12/AvatarOptimizer/pull/487)

## [1.5.0-rc.2] - 2023-09-15
### Added
- Automatically merge unnecessary activeness animated GameObject [`#476`](https://github.com/anatawa12/AvatarOptimizer/pull/476)

## [1.5.0-rc.1] - 2023-09-14
### Fixed
- StaticRenderer is not removed with MergeSkinnedMesh [`#473`](https://github.com/anatawa12/AvatarOptimizer/pull/473)

## [1.5.0-beta.14] - 2023-09-14
### Added
- Avoid Name Conflict in MergeBone [`#467`](https://github.com/anatawa12/AvatarOptimizer/pull/467)

### Fixed
- Light disappears [`#466`](https://github.com/anatawa12/AvatarOptimizer/pull/466)
- Automatic MergeBone may break Animation by conflictng GameObject name [`#467`](https://github.com/anatawa12/AvatarOptimizer/pull/467)

## [1.5.0-beta.13] - 2023-09-13
### Added
- Feature for debugging GC Objects [`#464`](https://github.com/anatawa12/AvatarOptimizer/pull/464)

### Fixed
- Collider disappears [`#463`](https://github.com/anatawa12/AvatarOptimizer/pull/463)
- Behaviour can disappears if initially disabled [`#465`](https://github.com/anatawa12/AvatarOptimizer/pull/465)

## [1.5.0-beta.12] - 2023-09-13
### Fixed
- Transform animation broken [`#461`](https://github.com/anatawa12/AvatarOptimizer/pull/461)

## [1.5.0-beta.11] - 2023-09-13
### Fixed
- Tangent is broken with MergeBone [`#457`](https://github.com/anatawa12/AvatarOptimizer/pull/457)
- VRCStation disappears [`#460`](https://github.com/anatawa12/AvatarOptimizer/pull/460)

## [1.5.0-beta.10] - 2023-09-12
### Fixed
- Error if we merge bone recursively [`#456`](https://github.com/anatawa12/AvatarOptimizer/pull/456)

## [1.5.0-beta.9] - 2023-09-12
### Fixed
- Automatic MergeBone doesn't think about animating `m_IsActive` of GameObject [`#454`](https://github.com/anatawa12/AvatarOptimizer/pull/454)
- MergeBone may make some bone inactive to active if bone being merged is inactive [`#454`](https://github.com/anatawa12/AvatarOptimizer/pull/454)

## [1.5.0-beta.8] - 2023-09-11
### Fixed
- MergeBone will break Normal and Tangent [`#448`](https://github.com/anatawa12/AvatarOptimizer/pull/448)
- PhysBone for Animator Parameter disappears [`#452`](https://github.com/anatawa12/AvatarOptimizer/pull/452)
- RemoveMeshByBlendShape on the SkinnedMeshRenderer with MergeSkinnedMesh not working [`#451`](https://github.com/anatawa12/AvatarOptimizer/pull/451)

## [1.5.0-beta.7] - 2023-09-08
### Fixed
- GC Objects will remove VRC Contact Components [`#438`](https://github.com/anatawa12/AvatarOptimizer/pull/438)
- Error if all vertices of some BlendShape is removed by RemoveMeshByBlendShape or RemoveMeshInBox [`#440`](https://github.com/anatawa12/AvatarOptimizer/pull/440)
- Inactivating parent GameObject or GameObject of component is not accounted in GC Objects [`#441`](https://github.com/anatawa12/AvatarOptimizer/pull/441)

## [1.5.0-beta.6] - 2023-09-07
### Added
- Automatic MergeBone in Remove Unused Objects [`#433`](https://github.com/anatawa12/AvatarOptimizer/pull/433)
- Preserve end bones in Remove Unused Objects [`#430`](https://github.com/anatawa12/AvatarOptimizer/pull/430)
  - This does same thing as `Preserve end bones` in UnusedBonesByReferenceTool.

### Deprecated
- UnusedBonesByReferenceTool component is now obsolete [`#430`](https://github.com/anatawa12/AvatarOptimizer/pull/430)
  - Newly introduced algorithm of`Remove Unused Objects` does same thing!
  - You can migrate to `Remove Unused Objects` only with one click!

### Fixed
- Unknown type warning is not correctly rendered [`#427`](https://github.com/anatawa12/AvatarOptimizer/pull/427)
- MergeBone with uneven scale is supported if all children are merged [`#426`](https://github.com/anatawa12/AvatarOptimizer/pull/426)
- MakeChidlren are detected as Unknown Component [`#431`](https://github.com/anatawa12/AvatarOptimizer/pull/431)

## [1.5.0-beta.5] - 2023-09-06
### Added
- Pre-building validation for MergeBone [`#417`](https://github.com/anatawa12/AvatarOptimizer/pull/417)
  - There are some (rare) cases that are not supported by MergeBone. This adds warning for such case.
- Validation error for self recursive MergeSkinnedMesh [`#418`](https://github.com/anatawa12/AvatarOptimizer/pull/418)
- Advanced Settings Section for Trace and Optimize [`#419`](https://github.com/anatawa12/AvatarOptimizer/pull/419)
  - Moved `Use Advanced Animator Parser` to there
  - Added `Exclusions` for exclude some GameObjects from optimization
  - Added `Use Legacy GC` to use legacy algotythm for Remove Unused Objects

### Changed
- Performance: Share MeshInfo2 between SkinnedMesh processing and MergeBone [`#421`](https://github.com/anatawa12/AvatarOptimizer/pull/421)

### Fixed
- Unknown Type Error is not localized [`#410`](https://github.com/anatawa12/AvatarOptimizer/pull/410)
- Crash with Unity 2022 [`#423`](https://github.com/anatawa12/AvatarOptimizer/pull/423)
  - [Due to bug in Unity Editor 2022.3 or later][unity-bug], Avatar Optimizer was not compatible with Unity 2022.
- worldUpObject is not proceed in GC Objects [`#424`](https://github.com/anatawa12/AvatarOptimizer/pull/424)

[unity-bug]: https://issuetracker.unity3d.com/issues/crash-on-gettargetassemblybyscriptpath-when-a-po-file-in-the-packages-directory-is-not-under-an-assembly-definition

## [1.5.0-beta.4] - 2023-09-05
### Changed
- Merged changes in 1.4.3 [`#409`](https://github.com/anatawa12/AvatarOptimizer/pull/409)

## [1.4.3] - 2023-09-05
## [1.4.3-beta.1] - 2023-09-05
### Fixed
- Mesh broken with BlendShape Frame with weight 0 [`#408`](https://github.com/anatawa12/AvatarOptimizer/pull/408)

## [1.5.0-beta.3] - 2023-09-05
### Fixed
- Unable to upload avatars with VRCSDK 3.2.x [`#407`](https://github.com/anatawa12/AvatarOptimizer/pull/407)

## [1.5.0-beta.2] - 2023-09-04
### Fixed
- Animator Component Disappears [`#404`](https://github.com/anatawa12/AvatarOptimizer/pull/404)
- MeshFilter Component Disappears with MeshRenderer [`#405`](https://github.com/anatawa12/AvatarOptimizer/pull/405)

## [1.5.0-beta.1] - 2023-09-04
### Changed
- Improved 'Remove Unused Objects' [`#401`](https://github.com/anatawa12/AvatarOptimizer/pull/401)
  - Remove Unused Objects now removes unnecessary Components & Bones!

## [1.4.2] - 2023-09-04
### Fixed
- Components/GameObjects can falsely detected as always disabled / inactive. [`#403`](https://github.com/anatawa12/AvatarOptimizer/pull/403)

## [1.4.1] - 2023-09-02
### Fixed
- RootBone become None with Merge SkinedMesh [`#399`](https://github.com/anatawa12/AvatarOptimizer/pull/399)

## [1.4.0] - 2023-09-02
## [1.4.0-rc.4] - 2023-09-01
## [1.4.0-rc.3] - 2023-09-01
### Changed
- Declare compatible with VRCSDK 3.3.x [`#395`](https://github.com/anatawa12/AvatarOptimizer/pull/395)
- Understandable Error if there are Missing Script Component [`#398`](https://github.com/anatawa12/AvatarOptimizer/pull/398)
  - Why VRCSDK doesn't have such a error system?

## [1.4.0-rc.2] - 2023-08-29
### Added
- Remove Mesh By BlendShape Editor now can set blendshape weights to 0/100 [`#389`](https://github.com/anatawa12/AvatarOptimizer/pull/389)

### Fixed
- Clear Endpoint Position may not work well with ignore transforms [`#390`](https://github.com/anatawa12/AvatarOptimizer/pull/390)
- Clear Endpoint Position doesn't support Undo [`#390`](https://github.com/anatawa12/AvatarOptimizer/pull/390)

## [1.4.0-rc.1] - 2023-08-27
### Added
- Multi Pass Rendering of Last SubMesh support [`#384`](https://github.com/anatawa12/AvatarOptimizer/pull/384)

### Fixed
- Unclear Error with Mesh with Read/Write off [`#386`](https://github.com/anatawa12/AvatarOptimizer/pull/386)

## [1.4.0-beta.1] - 2023-08-26
### Added
- Support for Multi Frame BlendShapes [`#333`](https://github.com/anatawa12/AvatarOptimizer/pull/333)
- Add link to help page [`#382`](https://github.com/anatawa12/AvatarOptimizer/pull/382)
- Advanced Animator Parser [`#343`](https://github.com/anatawa12/AvatarOptimizer/pull/343)
  - This is new AnimatorController parser to collect animated properties
  - This parser understands AnimatorLayers, so with this parser,
    AAO can freeze BlendShapes which are always finally animated to a constant value.
  - This also understands Additive Layer and BlendTree, so extremely rare problem in previous Animator Parser
    with Additive Layer or BlendTree will be fixed with this parser.

### Changed
- Auto FreezeBlendShape now freezes meaningless BlendShape [`#334`](https://github.com/anatawa12/AvatarOptimizer/pull/334)
  - If you removed some vertices with RemoveMeshInBox or RemoveMeshWithBlendShape, some BlendShape may transform no vertices
  - Auto FreeseBlendShae now freezez such a BlendShapes
- Auto FreezeBlendShape now freezes vertices even if already FreezeBlendShape is configured. [`#334`](https://github.com/anatawa12/AvatarOptimizer/pull/334)
- Meshes generated by AAO now have name [`#371`](https://github.com/anatawa12/AvatarOptimizer/pull/371)
  - This will improve compatibility with UniVRM.
- VPM Package now doesn't include Test code [`#372`](https://github.com/anatawa12/AvatarOptimizer/pull/372) [`#373`](https://github.com/anatawa12/AvatarOptimizer/pull/373)
- Better error infomation for MeshInfo2 error [`#381`](https://github.com/anatawa12/AvatarOptimizer/pull/381)

### Fixed
- MergeBone not working well with non-restpose bones [`#379`](https://github.com/anatawa12/AvatarOptimizer/pull/379)

## [1.3.4] - 2023-08-22
### Changed
- Internal implementation of Trace and Optimize [`#361`](https://github.com/anatawa12/AvatarOptimizer/pull/361)
- Documentation Improvements [`#366`](https://github.com/anatawa12/AvatarOptimizer/pull/366) [`#365`](https://github.com/anatawa12/AvatarOptimizer/pull/365)

## [1.3.3] - 2023-08-21
### Changed
- BlendShape Weight mismatch warning is now build-time warning instad of validate time warning [`#359`](https://github.com/anatawa12/AvatarOptimizer/pull/359)
  - Thanks to FreeseBlendShape by TraceAndOptimize, most pre-build this warning is false positive. So this warning is moved to build-time only.

### Fixed
- ClearEndpointPosition is not applied for non-first PhysBones on the GameObject [`#357`](https://github.com/anatawa12/AvatarOptimizer/pull/357)
- Incompatbile with Reload Scene disabaled [`#358`](https://github.com/anatawa12/AvatarOptimizer/pull/358)

## [1.3.2] - 2023-08-20
## [1.3.2-beta.3] - 2023-08-20
### Fixed
- No error context in Multi Pass Rendering error [`#348`](https://github.com/anatawa12/AvatarOptimizer/pull/348)

## [1.3.2-beta.2] - 2023-08-20
### Fixed
- Multi Passs Rendering not supported error doesn't have location info [`#347`](https://github.com/anatawa12/AvatarOptimizer/pull/347)

## [1.3.2-beta.1] - 2023-08-20
### Fixed
- Children of IsActive animated object is not considered [`#342`](https://github.com/anatawa12/AvatarOptimizer/pull/342)

## [1.3.1] - 2023-08-19
### Fixed
- Unity Editor may freezes when there are circular dependency [`#329`](https://github.com/anatawa12/AvatarOptimizer/pull/329)
- Network ID is not assigned for newly created PBs [`#331`](https://github.com/anatawa12/AvatarOptimizer/pull/331)
- Internally assigned animator controller is not skipped for default choosen playable layer in Trace and Optimize [`#332`](https://github.com/anatawa12/AvatarOptimizer/pull/332)
- VRCSDK assigned default animators are not considered in Trace and Optimize [`#332`](https://github.com/anatawa12/AvatarOptimizer/pull/332)
  - This bug doesn't create bad behavior for now but will does in the feature.
- Humanoid Animation are not considered in Trace and Optimize [`#332`](https://github.com/anatawa12/AvatarOptimizer/pull/332)
  - This bug doesn't create bad behavior for now but will does in the feature.
- Material Slot with null material is created if there are more SubMesh than Material Slots [`#337`](https://github.com/anatawa12/AvatarOptimizer/pull/337)
- AAO silently ignored multi pass rendering [`#337`](https://github.com/anatawa12/AvatarOptimizer/pull/337)
  - For now, multi pass rendering of last SubMesh is not (yet) supported so now cause error but will be supported.
- There is no warning about BlendShape weight difference [`#336`](https://github.com/anatawa12/AvatarOptimizer/pull/336)

## [1.3.0] - 2023-08-12
### Changed
- ApplyOnPlayGlobalActivator is no longer added for scens without avatars [`#318`](https://github.com/anatawa12/AvatarOptimizer/pull/318)

## [1.3.0-rc.2] - 2023-08-11
### Fixed
- Apply On Play may not working well [`#305`](https://github.com/anatawa12/AvatarOptimizer/pull/305)
- Some components unexpectedly can be added multiple times [`#306`](https://github.com/anatawa12/AvatarOptimizer/pull/306)

## [1.3.0-rc.1] - 2023-08-10
### Added
- Remove always disabled objects [`#278`](https://github.com/anatawa12/AvatarOptimizer/pull/278)
- The new Remove Mesh By Blend Shape component removes mesh data based on blend shapes. [`#275`](https://github.com/anatawa12/AvatarOptimizer/pull/275)
- Option to process Make Children before modular avatar [`#296`](https://github.com/anatawa12/AvatarOptimizer/pull/296)

### Changed
- Use UnityEditor api to compress texture [`#276`](https://github.com/anatawa12/AvatarOptimizer/pull/276)
  - This also adds some supported texture formats.
- Every component have `AAO` prefix in their name now [`#290`](https://github.com/anatawa12/AvatarOptimizer/pull/290)
  - The official shorthand for this tools is `AAO`!
- `Automatic Configuration` component has been renamed to `Trace And Optimize` [`#295`](https://github.com/anatawa12/AvatarOptimizer/pull/295)

### Fixed
- UnusedBonesByReferenceTool error with SMR without mesh [`#280`](https://github.com/anatawa12/AvatarOptimizer/pull/280)
- MergeSkinnedMesh doesn't work well with eyelids [`#284`](https://github.com/anatawa12/AvatarOptimizer/pull/284)
- Animating Behaviour.m_Enabled not working [`#287`](https://github.com/anatawa12/AvatarOptimizer/pull/287)
- Error Report Window may not refreshed after build error [`#299`](https://github.com/anatawa12/AvatarOptimizer/pull/299)

## [1.2.0] - 2023-07-26
### Added
- Support for material swapping animation in MergeSkinnedMesh [`#274`](https://github.com/anatawa12/AvatarOptimizer/pull/274)

## [1.2.0-rc.1] - 2023-07-24
### Fixed
- Breaks mesh without tangent [`#271`](https://github.com/anatawa12/AvatarOptimizer/pull/271)

## [1.2.0-beta.1] - 2023-07-17
### Added
- Automatic bounds computation in MergeSkinnedMesh [`#264`](https://github.com/anatawa12/AvatarOptimizer/pull/264)
- Automatic Configuration System [`#265`](https://github.com/anatawa12/AvatarOptimizer/pull/265)
  - Currently FreezeBlendShape can be automatically configured.

### Changed
- Support newly activated avatars in play mode for apply on play [`#263`](https://github.com/anatawa12/AvatarOptimizer/pull/263)

## [1.1.2-beta.1] - 2023-07-17
This release is mistake.

## [1.1.1] - 2023-07-14
### Changed
- Avatar GameObject marked as EditorOnly no longer be removed [`#261`](https://github.com/anatawa12/AvatarOptimizer/pull/261)
  - Previously, if avatar GameObject is marked as EditorOnly, whole avatar is removed and this confuses users.

### Fixed
- Name of failed ApplyOnPlayCallback is not included in error message [`#260`](https://github.com/anatawa12/AvatarOptimizer/pull/260)
- Entering play mode can be extremely slow if you have many avatar on the scene [`#262`](https://github.com/anatawa12/AvatarOptimizer/pull/262)

## [1.1.0] - 2023-07-13
### Changed
- Do not compress MergeToonLit generated texture on play [`#258`](https://github.com/anatawa12/AvatarOptimizer/pull/258)

## [1.1.0-rc.1] - 2023-07-10
## [1.1.0-beta.2] - 2023-07-08
### Fixed
- Asset files are not generated with Manual Bake [`#255`](https://github.com/anatawa12/AvatarOptimizer/pull/255)
- Merge Toon Lit duplicates vertex too many [`#256`](https://github.com/anatawa12/AvatarOptimizer/pull/256)
  - This could causes huge increase in avatar size. this is now fixed.

## [1.1.0-beta.1] - 2023-07-07
### Added
- Now we can choose texture format for Merge Toon Lit Material [`#251`](https://github.com/anatawa12/AvatarOptimizer/pull/251)
  - This includes one tiny **BREAKING CHANGES**.
  - Previously MergeToonLit uses ARGB32 as texture format but for now, it use ASTC 6x6 or DXT5 by default based on platform.

### Changed
- Move Components into `Avatar Optimizer` folder [`#247`](https://github.com/anatawa12/AvatarOptimizer/pull/247)
  - Previously they are `Optimizer` folder
- Completely rewrite apply on play system [`#249`](https://github.com/anatawa12/AvatarOptimizer/pull/249)
  - This replaces way to awake modular-avatar by bdunderscore.
  - The framework for this changes will be published as separated framework when ready.
- Use binary form of asset file in avatar optimizer output [`#252`](https://github.com/anatawa12/AvatarOptimizer/pull/252)

### Fixed
- Manual bake not working with avatars with invalid file name chars [`#253`](https://github.com/anatawa12/AvatarOptimizer/pull/253)

## [1.0.0] - 2023-06-27
## [1.0.0-beta.5] - 2023-06-26
**If you're using v0.3.x or older, Please upgrade to v0.4.x before upgrading v1.x.x!**

**もし v0.3.x 以前を使用しているのであれば, v1.x.xに更新する前に v0.4.x に更新してください!**

### Changed
- Merged changes in 0.4.12

## [0.4.12] - 2023-06-22
### Changed
- MergePhysBone without ClearEndpointPosition [`#239`](https://github.com/anatawa12/AvatarOptimizer/pull/239)
  - Instead of ClearEndpointPosition, you can use original value, or override Endpoint Position.

## [1.0.0-beta.4] - 2023-06-19
**If you're using v0.3.x or older, Please upgrade to v0.4.x before upgrading v1.x.x!**

**もし v0.3.x 以前を使用しているのであれば, v1.x.xに更新する前に v0.4.x に更新してください!**

### Changed
- Merged changes in 0.4.8, 0.4.9, 0.4.10, and 0.4.11

## [0.4.11] - 2023-06-19
### Changed
- Show error with user friendly message if blendshape for eyelids are removed / frozen. [`#253`](https://github.com/anatawa12/AvatarOptimizer/pull/253) 

### Fixed
- eyelids BlendShape settings are mapped even if it's disabled [`#235`](https://github.com/anatawa12/AvatarOptimizer/pull/235)
  - This fixes error if internally eyelids BlendShape are frozen.

## [0.4.10] - 2023-06-17
## [0.4.10-beta.1] - 2023-06-17
### Fixed
- PrefabSafesSet's prefab modifications on latest layer are invisible on inspector [`#229`](https://github.com/anatawa12/AvatarOptimizer/pull/229)

## [0.4.9] - 2023-06-16
### Fixed
- NullReferenceException if window is in background [`#226`](https://github.com/anatawa12/AvatarOptimizer/pull/226)

## [0.4.8] - 2023-06-16
## [1.0.0-beta.3] - 2023-06-13
**If you're using v0.3.x or older, Please upgrade to v0.4.x before upgrading v1.x.x!**

**もし v0.3.x 以前を使用しているのであれば, v1.x.xに更新する前に v0.4.x に更新してください!**

### Added
- Merged changes in 0.4.7 [`#224`](https://github.com/anatawa12/AvatarOptimizer/pull/224)

## [0.4.7] - 2023-06-13
### Fixed
- Dropping PhysBone to MergePhysBone is not working [`#221`](https://github.com/anatawa12/AvatarOptimizer/pull/221)
- First box will be size of zero [`#223`](https://github.com/anatawa12/AvatarOptimizer/pull/223)

## [1.0.0-beta.2] - 2023-06-10
**If you're using v0.3.x or older, Please upgrade to v0.4.x before upgrading v1.x.x!**

**もし v0.3.x 以前を使用しているのであれば, v1.x.xに更新する前に v0.4.x に更新してください!**

### Added
- Merged changes in 0.4.5 and 0.4.6 [`#218`](https://github.com/anatawa12/AvatarOptimizer/pull/218)

## [0.4.6] - 2023-06-10
### Changed
- Improve ErrorReport window on build error [`#216`](https://github.com/anatawa12/AvatarOptimizer/pull/216)

## [0.4.5] - 2023-06-06
### Fixed
- Error in MergeSkinnedMeshProcessor with RecordMoveProperty [`#214`](https://github.com/anatawa12/AvatarOptimizer/pull/214)

## [1.0.0-beta.1] - 2023-06-05
**If you're using v0.3.x or older, Please upgrade to v0.4.x before upgrading v1.x.x!**

**もし v0.3.x 以前を使用しているのであれば, v1.x.xに更新する前に v0.4.x に更新してください!**

### Removed
- Save format migration system [`#199`](https://github.com/anatawa12/AvatarOptimizer/pull/199)
  - We no longer see save data in format of v0.3.x or older.
  - Please migrate to v0.4.x format before installing v1.0.0.

## [0.4.5-beta.1] - 2023-06-05
## [0.4.4] - 2023-06-04
## [0.4.4-rc.1] - 2023-06-04
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
- Disable animating `m_Enabled` of source SkinnedMeshRenderer [`#190`](https://github.com/anatawa12/AvatarOptimizer/pull/190)
  - Animating `m_Enabled` of source SkinnedMeshRenderer now doesn't affects merged SkinnedMeshRenderer
  - If you actually want to enable/disable merged SkinnedMeshRenderer,
    animate `m_Enabled` of merged SkinnedMeshRenderer instead.

## [0.4.1-rc.3] - 2023-05-22
### Fixed
- Name of Is Animated and Parameter field are not correct [`#183`](https://github.com/anatawa12/AvatarOptimizer/pull/183)
- We cannot set override setting of Colliders to Copy [`#183`](https://github.com/anatawa12/AvatarOptimizer/pull/183)
- Error with MergeToonLit [`#185`](https://github.com/anatawa12/AvatarOptimizer/pull/185)
- Poor word choice in Japanese Translation [`#174`](https://github.com/anatawa12/AvatarOptimizer/pull/174)
- Localization is not applied for some fields [`#186`](https://github.com/anatawa12/AvatarOptimizer/pull/186)

## [0.4.1-rc.2] - 2023-05-22
### Fixed
- Errors in Animation Mapping System [`#176`](https://github.com/anatawa12/AvatarOptimizer/pull/176)
  - Error with removed property
  - Error with Property moved twice
- Merge PhysBone is not working [`#177`](https://github.com/anatawa12/AvatarOptimizer/pull/177)
  - Previously, values are not copied correctly
- The help box for description of components without description were shown [`#178`](https://github.com/anatawa12/AvatarOptimizer/pull/178)

## [0.4.1-rc.1] - 2023-05-21
### Changed
- Improve Animation Mapping System [`#172`](https://github.com/anatawa12/AvatarOptimizer/pull/172)
  - This should reduce build time

## [0.4.1-beta.1] - 2023-05-20
### Changed
- Reimplemented Animation Mapping System Completely [`#168`](https://github.com/anatawa12/AvatarOptimizer/pull/168)
  - This should fixes problem with objects/components at same place.

### Fixed
- Error is not cleared on build [`#170`](https://github.com/anatawa12/AvatarOptimizer/pull/170)

## [0.4.0] - 2023-05-20
## [0.4.0-rc.2] - 2023-05-19
### Fixed
- Error when we opened Editor of MergePhysBone component [`#167`](https://github.com/anatawa12/AvatarOptimizer/pull/167)

## [0.4.0-rc.1] - 2023-05-19
### Changed
- Save format for MergePhysBone [`#166`](https://github.com/anatawa12/AvatarOptimizer/pull/166)
  - Previously used backed PhysBone component for override values are removed.
  - There are no changes in behaviour. Just migrate your assets.

### Fixed
- Error with Animator at non-root GameObject [`#164`](https://github.com/anatawa12/AvatarOptimizer/pull/164)
- Error with copied MergePhysBone component [`#165`](https://github.com/anatawa12/AvatarOptimizer/pull/165)

## [0.4.0-beta.1] - 2023-05-16
### Added
- Japanese Translation: 日本語化 [`#152`](https://github.com/anatawa12/AvatarOptimizer/pull/152)

### Removed
- Delete GameObject feature [`#153`](https://github.com/anatawa12/AvatarOptimizer/pull/153)
  - Use `EditorOnly` tag instead

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
- Manual Bake Avatar [`#147`](https://github.com/anatawa12/AvatarOptimizer/pull/147)
  - Left click the avatar and click `[AvatarOptimizer] Manual Bake Avatar`

## [0.3.2-beta.2] - 2023-05-12
### Added
- Website for AvatarOptimizer [`#139`](https://github.com/anatawa12/AvatarOptimizer/pull/139)
  - Will be available at <https://vpm.anatawa12.com/avatar-optimizer/>
  - For now, beta site is only available at <https://vpm.anatawa12.com/avatar-optimizer/beta>

## [0.3.2-beta.1] - 2023-05-09
### Added
- Error Reporting System [`#124`](https://github.com/anatawa12/AvatarOptimizer/pull/124)
  - This adds window shows errors on build
  - This is based on Modular Avatar's Error Reporting Window. thanks `@bdunderscore`

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
### Fixed
- Parent differ error is gone [`#129`](https://github.com/anatawa12/AvatarOptimizer/pull/129)

## [0.3.0-rc.2] - 2023-05-02
### Fixed
- Max Squish is not shown if we're using PhysBone 1.0 [`#127`](https://github.com/anatawa12/AvatarOptimizer/pull/127)

## [0.3.0-rc.1] - 2023-04-30
### Changed
- Upgrade CL4EE to 1.0.0 [`#121`](https://github.com/anatawa12/AvatarOptimizer/pull/121)

## [0.3.0-beta.3] - 2023-04-28
### Added
- UnusedBonesByReferencesTool [`#112`](https://github.com/anatawa12/AvatarOptimizer/pull/112)
  - This is port of [UnusedBonesByReferencesTool by Narazaka][UnusedBonesByReferencesTool]
- Support for VRCSDK 3.2.0! [`#117`](https://github.com/anatawa12/AvatarOptimizer/pull/117)
  - This includes support for PhysBone Versions and PhysBone 1.1

[UnusedBonesByReferencesTool]: https://narazaka.booth.pm/items/3831781

### Removed
- Removed support for VRCSDK 3.1.x. [`#117`](https://github.com/anatawa12/AvatarOptimizer/pull/117)
  - Dropping VRCSDK support is a **BREAKING** changes.
  - However, we may drop old VRCSDK support in the minor releases of AvatarOptimizer in the feature.
  - In the other hand, we promise we'll never drop old VRCSDK support in the patch releases.
  - Notice: in the 0.x.y release, y is a minor releases in this project.

## [0.3.0-beta.2] - 2023-04-26
### Fixed
- ShouldIgnoreComponentPatch cases compilation error [`#108`](https://github.com/anatawa12/AvatarOptimizer/pull/108)
- Not working on the build time [`#114`](https://github.com/anatawa12/AvatarOptimizer/pull/114)

## [0.3.0-beta.1] - 2023-04-24
### Added
- Make Children of Me [`#100`](https://github.com/anatawa12/AvatarOptimizer/pull/100)
  - As a alternative of feature removal in same pull request

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

### Removed
- **BREAKING** Removed Prefab Safe List [`#95`](https://github.com/anatawa12/AvatarOptimizer/pull/95)
- **BREAKING** Removed RootTransform feature from MergePhysBone [`#100`](https://github.com/anatawa12/AvatarOptimizer/pull/100)
  - See [this issue comment][about-root-transform] for more datails.
- **BREAKING** Dropped support for VRCSDK 3.1.12 or older [`#101`](https://github.com/anatawa12/AvatarOptimizer/pull/101)
  - Now, we require VRCSDK since 3.1.13 (including) until 3.2.0 (excluding)

[about-root-transform]: https://github.com/anatawa12/AvatarOptimizer/issues/62#issuecomment-1512586282

## [0.2.8] - 2023-04-19
## [0.2.8-rc.1] - 2023-04-19
### Fixed
- NullReferenceException with prefabs in editor for PrefabSafeSet [`#92`](https://github.com/anatawa12/AvatarOptimizer/pull/92)

## [0.2.7] - 2023-04-01
## [0.2.7-beta.1] - 2023-04-01
### Added
- Support for VRCSDK 3.1.12 and 3.1.13

## [0.2.6] - 2023-03-31
## [0.2.6-rc.4] - 2023-03-30
### Fixed
- Mesh is broken if more than 65536 vertices are exists [`#87`](https://github.com/anatawa12/AvatarOptimizer/pull/87)
  - Because we didn't check for vertices count and index format, vertex index can be overflow before.
- Generated assets are invisible for a while [`#88`](https://github.com/anatawa12/AvatarOptimizer/pull/88)

## [0.2.6-rc.3] - 2023-03-29
### Fixed
- Assertion does not work well [`#85`](https://github.com/anatawa12/AvatarOptimizer/pull/85)
  - This can make invalid mesh

## [0.2.6-rc.2] - 2023-03-29
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

[Unreleased]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.3-beta.1...HEAD
[1.5.3-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.2...v1.5.3-beta.1
[1.5.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.2-beta.3...v1.5.2
[1.5.2-beta.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.2-beta.2...v1.5.2-beta.3
[1.5.2-beta.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.2-beta.1...v1.5.2-beta.2
[1.5.2-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.1...v1.5.2-beta.1
[1.5.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.1-beta.1...v1.5.1
[1.5.1-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0...v1.5.1-beta.1
[1.5.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-rc.13...v1.5.0
[1.5.0-rc.13]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-rc.12...v1.5.0-rc.13
[1.5.0-rc.12]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-rc.11...v1.5.0-rc.12
[1.5.0-rc.11]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-rc.10...v1.5.0-rc.11
[1.5.0-rc.10]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-rc.9...v1.5.0-rc.10
[1.5.0-rc.9]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-rc.8...v1.5.0-rc.9
[1.5.0-rc.8]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-rc.7...v1.5.0-rc.8
[1.5.0-rc.7]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-rc.6...v1.5.0-rc.7
[1.5.0-rc.6]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-rc.5...v1.5.0-rc.6
[1.5.0-rc.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-rc.4...v1.5.0-rc.5
[1.5.0-rc.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-rc.3...v1.5.0-rc.4
[1.5.0-rc.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-rc.2...v1.5.0-rc.3
[1.5.0-rc.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-rc.1...v1.5.0-rc.2
[1.5.0-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-beta.14...v1.5.0-rc.1
[1.5.0-beta.14]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-beta.13...v1.5.0-beta.14
[1.5.0-beta.13]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-beta.12...v1.5.0-beta.13
[1.5.0-beta.12]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-beta.11...v1.5.0-beta.12
[1.5.0-beta.11]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-beta.10...v1.5.0-beta.11
[1.5.0-beta.10]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-beta.9...v1.5.0-beta.10
[1.5.0-beta.9]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-beta.8...v1.5.0-beta.9
[1.5.0-beta.8]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-beta.7...v1.5.0-beta.8
[1.5.0-beta.7]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-beta.6...v1.5.0-beta.7
[1.5.0-beta.6]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-beta.5...v1.5.0-beta.6
[1.5.0-beta.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-beta.4...v1.5.0-beta.5
[1.5.0-beta.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.3...v1.5.0-beta.4
[1.4.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.3-beta.1...v1.4.3
[1.4.3-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.2...v1.4.3-beta.1
[1.5.0-beta.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-beta.2...v1.5.0-beta.3
[1.5.0-beta.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0-beta.1...v1.5.0-beta.2
[1.5.0-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.2...v1.5.0-beta.1
[1.4.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.1...v1.4.2
[1.4.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.0...v1.4.1
[1.4.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.0-rc.4...v1.4.0
[1.4.0-rc.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.0-rc.3...v1.4.0-rc.4
[1.4.0-rc.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.0-rc.2...v1.4.0-rc.3
[1.4.0-rc.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.0-rc.1...v1.4.0-rc.2
[1.4.0-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.0-beta.1...v1.4.0-rc.1
[1.4.0-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.4...v1.4.0-beta.1
[1.3.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.3...v1.3.4
[1.3.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.2...v1.3.3
[1.3.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.2-beta.3...v1.3.2
[1.3.2-beta.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.2-beta.2...v1.3.2-beta.3
[1.3.2-beta.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.2-beta.1...v1.3.2-beta.2
[1.3.2-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.1...v1.3.2-beta.1
[1.3.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.0...v1.3.1
[1.3.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.0-rc.2...v1.3.0
[1.3.0-rc.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.0-rc.1...v1.3.0-rc.2
[1.3.0-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.2.0...v1.3.0-rc.1
[1.2.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.2.0-rc.1...v1.2.0
[1.2.0-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.2.0-beta.1...v1.2.0-rc.1
[1.2.0-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.1.2-beta.1...v1.2.0-beta.1
[1.1.2-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.1.1...v1.1.2-beta.1
[1.1.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.1.0-rc.1...v1.1.0
[1.1.0-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.1.0-beta.2...v1.1.0-rc.1
[1.1.0-beta.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.1.0-beta.1...v1.1.0-beta.2
[1.1.0-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.0.0...v1.1.0-beta.1
[1.0.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.0.0-beta.5...v1.0.0
[1.0.0-beta.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.0.0-beta.4...v1.0.0-beta.5
[1.0.0-beta.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.0.0-beta.3...v1.0.0-beta.4
[1.0.0-beta.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.0.0-beta.2...v1.0.0-beta.3
[1.0.0-beta.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.0.0-beta.1...v1.0.0-beta.2
[1.0.0-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.12...v1.0.0-beta.1
[0.4.12]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.11...v0.4.12
[0.4.11]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.10...v0.4.11
[0.4.10]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.10-beta.1...v0.4.10
[0.4.10-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.9...v0.4.10-beta.1
[0.4.9]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.8...v0.4.9
[0.4.8]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.7...v0.4.8
[0.4.7]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.6...v0.4.7
[0.4.6]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.5...v0.4.6
[0.4.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.5-beta.1...v0.4.5
[0.4.5-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.4...v0.4.5-beta.1
[0.4.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.4-rc.1...v0.4.4
[0.4.4-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.3...v0.4.4-rc.1
[0.4.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.2...v0.4.3
[0.4.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.1...v0.4.2
[0.4.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.1-rc.3...v0.4.1
[0.4.1-rc.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.1-rc.2...v0.4.1-rc.3
[0.4.1-rc.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.1-rc.1...v0.4.1-rc.2
[0.4.1-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.1-beta.1...v0.4.1-rc.1
[0.4.1-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.0...v0.4.1-beta.1
[0.4.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.0-rc.2...v0.4.0
[0.4.0-rc.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.0-rc.1...v0.4.0-rc.2
[0.4.0-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.0-beta.1...v0.4.0-rc.1
[0.4.0-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.5...v0.4.0-beta.1
[0.3.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.4...v0.3.5
[0.3.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.3...v0.3.4
[0.3.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.2...v0.3.3
[0.3.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.2-beta.2...v0.3.2
[0.3.2-beta.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.2-beta.1...v0.3.2-beta.2
[0.3.2-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.1...v0.3.2-beta.1
[0.3.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.0-rc.2...v0.3.0
[0.3.0-rc.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.0-rc.1...v0.3.0-rc.2
[0.3.0-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.0-beta.3...v0.3.0-rc.1
[0.3.0-beta.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.0-beta.2...v0.3.0-beta.3
[0.3.0-beta.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.0-beta.1...v0.3.0-beta.2
[0.3.0-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.8...v0.3.0-beta.1
[0.2.8]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.8-rc.1...v0.2.8
[0.2.8-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.7...v0.2.8-rc.1
[0.2.7]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.7-beta.1...v0.2.7
[0.2.7-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.6...v0.2.7-beta.1
[0.2.6]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.6-rc.4...v0.2.6
[0.2.6-rc.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.6-rc.3...v0.2.6-rc.4
[0.2.6-rc.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.6-rc.2...v0.2.6-rc.3
[0.2.6-rc.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.6-rc.1...v0.2.6-rc.2
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
