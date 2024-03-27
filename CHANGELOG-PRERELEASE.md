# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog].

[Keep a Changelog]: https://keepachangelog.com/en/1.1.0/

## [Unreleased]
### Added
- Animator Optimizer `#854`
  - Most features of Animator Optimizer is not available in Unity 2019.
  - Animator Optimizer optimizes your Animator Controller without behaviour Changes
  - Current Optimizer includes the following optimization
    - Remove meaningless properties `#854`
    - Converts Entry / Exit to 1D BlendTree `#854` `#867` `#927`
    - Merges multiple Direct BlendTree to single Direct BlendTree `#870`
    - Removes meaningless Animator Layers `#870`
- Asset Description `#847`
  - Asset Description is the file to provide information of your assets for Avatar Optimizer.
  - Please see documentation for more details.
- Warning for material animation in Merge Skinned Mesh `#769`
  - Merge Skinned Mesh does not support animating material properties differently. (In other words, it can be broken.)
  - Since this version, AAO will warn for such a case.
  - If you animated all materials from same animations, your animation will not be warned.
- API for declaring dependency relationship to the name of the component `#943`
  - You can use this API to not change the name of the GameObject.
- Configuring `Clamp BlendShapes (Deprecated)` `#957`
  - Since VRCSDK 3.5.1, VRCSDK sets `Clamp BlendShapes (Deprecated)` to true on assembly reload.
  - This is not a good setting for AAO in EditMode since AAO does not support clamping BlendShapes.
  - That's why AAO now configures `Clamp BlendShapes (Deprecated)` to false in edit mode and true in play mode.
  - PlayMode is usually used for testing the avatar behavior so it's better to have the same setting as VRChat client.
  - If you want not to change this setting, please disable `Tools/Avatar Optimizer/Configure Clamp BlendShape Weight`.

### Changed
- MergePhysBone now corrects curve settings `#775`
- MergePhysBone now warns if chain length are not same `#775`
- MergePhysBone with only one source is now error `#775`
  - It was not working well and not a error by a bug.
- PhysBone that swings no bones are now removed `#864`
  - I found such a PhysBone on Lime so I added this feature.
- Switched Localization system to NDMF from CL4EE `#873`
  - Since this release, Avatar Optimizer is no longer depends on CL4EE.
  - Because VCC doesn't remove unused packages, CL4EE may still be installed on your project.
  - If you want to remove CL4EE, please remove it manually.
- Suppressed animated BlendShape warning of FreezeBlendShape if it's animated to a few constants `#881`
  - Modern models have tons of BlendShapes to change their face shape but emotion animation of some of them animates such a BlendShapes to constant (default value).
  - That's unnecessary (incorrect I think) and force users to remove or change the clip when user wants to face shape.
  - I see AAO users use `FreezeBlendShapes` for overriding such a BlendShapes on twitter.
  - I think using this way is reasonable enough so I suppressed the warning if AAO detected such a usage.
- Minimum VRCSDK to 3.3.0 `#882`
  - VRCSDK 3.3.0 is required for stable NDMF-VRCSDK compatibility.
- Endpoint Position settings for newly created MergePhysBone is now Copy instead of Clear `#945`
  - The Clear settings will increase the number of PhysBone Transforms so it's not better as a default settings.
- Improved activeness animation warning in Merge Skinned Mesh `#948`
  - Reduced false-positive warnings
    - Previously, AAO warns if activeness warning is applied to different GameObjects.
    - However, this can be false-positive if animation is applied to different GameObjects with same timing.
    - Since this version, AAO will not warn if the activeness is animated in same animation clip with same curve.
  - Combined warning per Merge Skinned Mesh component.
    - Previously, AAO warns for each source Renderers.
    - Since this version, AAO creates one warning for each Merge Skinned Mesh component.
- An error is occured with Cloth component in Merge Skinned Mesh component `#949`
  - The Cloth component is not supported by Merge Skinned Mesh component.
  - In previous versions, AAO will keep the source Skinned Mesh Renderer if it's with Cloth component by bug.
  - Since this version, AAO will make an error if the source Skinned Mesh Renderer is with Cloth component.
- Remove Unused Objects now removes PhysBones and Contact Receivers with parameters defined but not used by Animator Controllers `#959`
  - Previously, AAO did not remove PhysBones and Contact Receivers if they are defined in Animator Controllers whether they are used or not.
  - I thought such a PhysBones on the base body are rare but my friend told me there is Manuka has such a PhysBone so I added this feature.
- Dropping GameObject to PrefabSafeSet adds the All components on the GameObject to the PrefabSafeSet `#960`
  - You can add all PhysBones on the GameObject by dropping the GameObject to the MergePhysBone component.

### Deprecated

### Removed
- Compatibility with VRCQuestTools v1.x `#847`
  - Please use VRCQuestTools v2.x, which has compatibility with AAO.

### Fixed
- Inspector of ComponentTypePair (GCDebug) is broken `#846`
- Bones swung by unused PhysBones (which will be removed by AAO) are not merged `#850`
  - Note that To fix this problem, AnimatorParser is almost completely rewritten.
  - It's not expected to have behavior change, but if you found some, please report it.
- Re-fix Nested Constraint can be broken with Trace and Optimize `#880`
- Fix non-VRChat project support `#884`
- Fix VRM support `#892`
- ArgumentNullException in Edit-mode Remove Mesh Preview `#942`
- Bad behavior if EditMode preview is enabled when entering play mode `#956`

### Security

## [1.6.9] - 2024-03-27
## [1.6.9-beta.3] - 2024-03-24
### Fixed
- ContextHolder become unknown component in NDMF 1.4.0 [`#946`](https://github.com/anatawa12/AvatarOptimizer/pull/946)

## [1.6.9-beta.2] - 2024-03-22
### Added
- Support for VRCSDK 3.5.2-beta.2 [`#935`](https://github.com/anatawa12/AvatarOptimizer/pull/935)

## [1.6.9-beta.1] - 2024-03-16
### Added
- Support for VRCSDK 3.5.2-beta.1 [`#926`](https://github.com/anatawa12/AvatarOptimizer/pull/926)

## [1.6.8] - 2024-03-12
### Fixed
- If some component refers external component, internal error [`#921`](https://github.com/anatawa12/AvatarOptimizer/pull/921)

## [1.6.7] - 2024-02-28
## [1.6.7-beta.1] - 2024-02-28
### Fixed
- Compilation Error due to VRCImposterSettings with VRCSDK 3.2.x [`#905`](https://github.com/anatawa12/AvatarOptimizer/pull/905)
- Skinned Mesh Renderers with None mesh will become Mesh with no polygons [`#906`](https://github.com/anatawa12/AvatarOptimizer/pull/906)
  - This may affects bounds of Performance Rank in VRChat
- Exclusions not working with Automatically Remove Zero Sized Polygons [`#907`](https://github.com/anatawa12/AvatarOptimizer/pull/907)
- Fix non-VRChat project support [`#884`](https://github.com/anatawa12/AvatarOptimizer/pull/884) (backport in `#909`)
- Merge Toonlit with uv tiling is broken [`#911`](https://github.com/anatawa12/AvatarOptimizer/pull/911)

## [1.6.6] - 2024-01-31
### Fixed
- Some features are not working well if `Trace and Optimize` is not attached [`#876`](https://github.com/anatawa12/AvatarOptimizer/pull/876)

## [1.6.5] - 2024-01-29
### Changed
- use stable version of NDMF 1.3.0 or later [`#821`](https://github.com/anatawa12/AvatarOptimizer/pull/821)

## [1.6.5-rc.3] - 2024-01-27
### Changed
- Reverted NDMF to 1.3.0-rc.3 [`#858`](https://github.com/anatawa12/AvatarOptimizer/pull/858)
  - NDMF 1.3.0-rc.4 or later does not execute Optimizing phase on entering play mode.
  - See [this issue](https://github.com/bdunderscore/ndmf/issues/129) for more details.

### Fixed
- Nested Constraint can be broken with Trace and Optimize [`#857`](https://github.com/anatawa12/AvatarOptimizer/pull/857)

## [1.6.5-rc.2] - 2024-01-24
### Changed
- Project is slightly renamed to AAO: Avatar Optimizer [`#830`](https://github.com/anatawa12/AvatarOptimizer/pull/830)
  - The term `AAO` and `Avatar Optimizer` are not changed, but display name on the VCC is changed to `AAO: Avatar Optimizer`
- Upgrades NDMF to 1.3.0-rc.4 [`#853`](https://github.com/anatawa12/AvatarOptimizer/pull/853)

### Fixed
- Humanoid Bones may be removed by Trace and Optimize [`#831`](https://github.com/anatawa12/AvatarOptimizer/pull/831)
- Add `license`, `documentationUrl`, and `changelogUrl` to package.json [`#851`](https://github.com/anatawa12/AvatarOptimizer/pull/851)

## [1.6.5-rc.1] - 2024-01-07
### Changed
- Upgrades NDMF to 1.3.0-rc.1 [`#815`](https://github.com/anatawa12/AvatarOptimizer/pull/815)
  - Use Title instead of Description since substation in Title is implemented

### Fixed
- Reference to meshes will be merged is removed [`#808`](https://github.com/anatawa12/AvatarOptimizer/pull/808)
- VRM: Fix MergeSkinnedMesh breaking BlendShapeClip / VRM10Expression [`#810`](https://github.com/anatawa12/AvatarOptimizer/pull/810)
- Unknown component warning were shown multiple time for one type [`#818`](https://github.com/anatawa12/AvatarOptimizer/pull/818)
  - In addition, location of the unknown components are shown on the error report.
- AvatarOptimizer didn't register modification to ObjectRegistry [`#815`](https://github.com/anatawa12/AvatarOptimizer/pull/815)
- Empty Armature trick broken [`#819`](https://github.com/anatawa12/AvatarOptimizer/pull/819)
- Added workaround for `Array index (n) is out of bounds (size=m)` error

## [1.6.5-beta.1] - 2023-12-28
### Changed
- AvatarOptimizer now uses ErrorReporting API of NDMF instead of our own API [`#805`](https://github.com/anatawa12/AvatarOptimizer/pull/805)

### Fixed
- Fix support for UniVRM components [`#802`](https://github.com/anatawa12/AvatarOptimizer/pull/802)

## [1.6.4] - 2023-12-10
## [1.6.4-beta.1] - 2023-12-10
### Fixed
- Error with generic avatar in 2022 [`#794`](https://github.com/anatawa12/AvatarOptimizer/pull/794)
- Assertion Error in some rare case [`#795`](https://github.com/anatawa12/AvatarOptimizer/pull/795)

## [1.6.3] - 2023-12-09
### Added
- Support for VRCSDK 3.5.x [`#787`](https://github.com/anatawa12/AvatarOptimizer/pull/787)
  - Actually, previous version of AAO works well with VRCSDK 3.5.x / Unity 2022 with tiny bugs.
  - I've fixed some bugs in Unity 2022 in this release.
  - Since this version, package.json declares Avatar Optimizer is compatible with VRCSDK 3.5.x.
  - I was planned to release this changes while VRCSDK 3.5.0 is in beta.
  - However, VRCSDK 3.5.0 beta was only 3 hours so I could not.

### Fixed
- Fix NullReferenceException on Unity 2022 when extra Animator components are present [`#778`](https://github.com/anatawq12/AvatarOptimizer/pull/778)
- Fix Errors with Generic Avatars [`#779`](https://github.com/anatawa12/AvatarOptimizer/pull/779)
- Editing Prefabs with AAO Components in Unity 2022 will cause Error [`#782`](https://github.com/anatawa12/AvatarOptimizer/pull/782)
- Error if there are reference to Prefab Asset PhysBone Collider [`#783`](https://github.com/anatawa12/AvatarOptimizer/pull/783)
- Remove Mesh in Box editor broken if inspector is narrow [`#784`](https://github.com/anatawa12/AvatarOptimizer/pull/784)
- Errors for partially incorrectly configured avatars [`#786`](https://github.com/anatawa12/AvatarOptimizer/pull/786)
  - Since this release, instead of internal errors, warnings are shown

## [1.6.2] - 2023-11-30
## [1.6.2-rc.1] - 2023-11-30
### Fixed
- Path remapping for merge bone will not work well in some (relatively rare) case [`#764`](https://github.com/anatawa12/AvatarOptimizer/pull/764)
- Error due to PhysBone collider with root bone outside avatar [`#766`](https://github.com/anatawa12/AvatarOptimizer/pull/766)

## [1.6.1] - 2023-11-29
### Fixed
- Error if there are None colliders for PhysBone [`#758`](https://github.com/anatawa12/AvatarOptimizer/pull/758)
- BlendShapes can broken in extreamly rare cases [`#760`](https://github.com/anatawa12/AvatarOptimizer/pull/760)
  - It seems this is due to Unity bug.

## [1.6.0] - 2023-11-27
## [1.6.0-rc.4] - 2023-11-25
### Fixed
- Error if there are null in ingore transforms of PhysBone [`#749`](https://github.com/anatawa12/AvatarOptimizer/pull/749)

## [1.6.0-rc.3] - 2023-11-25
### Fixed
- MergeBone breaks `ignoreTransforms` of PhysBone [`#745`](https://github.com/anatawa12/AvatarOptimizer/pull/745)
- freeze meaningless may cause `FreezeBlendShape:warning:animation` warning [`#746`](https://github.com/anatawa12/AvatarOptimizer/pull/746)

## [1.6.0-rc.2] - 2023-11-22
### Fixed
- FinalIK Components will be removed [`#742`](https://github.com/anatawa12/AvatarOptimizer/pull/742)

## [1.6.0-rc.1] - 2023-11-21
### Fixed
- Remove Unused Object may break ParticleSystem [`#738`](https://github.com/anatawa12/AvatarOptimizer/pull/738)
  - Trigger Colliders can be disapper if you specify Transform instead of Collider instance.
  - Initially diabled particle system module will be ignored

## [1.6.0-beta.12] - 2023-11-21
### Added
- PhysBone Optimization [`#733`](https://github.com/anatawa12/AvatarOptimizer/pull/733)
  - Unnessesary isAnimated is now unconfigured
  - Floor Colliders with same configuration will be merged to one floor collider
- Minimum Support for FinalIK [`#735`](https://github.com/anatawa12/AvatarOptimizer/pull/735)

### Fixed
- Some missing components warnings [`#736`](https://github.com/anatawa12/AvatarOptimizer/pull/736)
  - warning for `ONSPAudioSource`, `VRCImpostorSettings`, and `RectTransform` are fixed

## [1.6.0-beta.11] - 2023-11-18
### Fixed
- False Positive warning for constant animation in Freeze BlendShape [`#722`](https://github.com/anatawa12/AvatarOptimizer/pull/722)
- Error if we merged Viseme BlendShapes [`#728`](https://github.com/anatawa12/AvatarOptimizer/pull/728)

## [1.5.11] - 2023-11-18
## [1.5.11-beta.1] - 2023-11-17
### Fixed
- Dynamic Bone support not working [`#727`](https://github.com/anatawa12/AvatarOptimizer/pull/727)

## [1.6.0-beta.10] - 2023-11-13
### Added
- Error for MergeBone on the Avatar Root [`#716`](https://github.com/anatawa12/AvatarOptimizer/pull/716)
- Warning for freezing animated BlendShapes [`#719`](https://github.com/anatawa12/AvatarOptimizer/pull/719)

### Fixed
- Compatibility with transform moving plugins [`#715`](https://github.com/anatawa12/AvatarOptimizer/pull/715)
  - Remove Mesh in Box was not working well with [FloorAdjuster]

[FloorAdjuster]: https://github.com/Narazaka/FloorAdjuster
## [1.6.0-beta.9] - 2023-11-12
### Fixed
- Humanoid of Avatar Root Animator broken [`#714`](https://github.com/anatawa12/AvatarOptimizer/pull/714)

## [1.6.0-beta.8] - 2023-11-11
### Fixed
- Animator of AvatarRoot diesappears [`#711`](https://github.com/anatawa12/AvatarOptimizer/pull/711)

## [1.6.0-beta.7] - 2023-11-11
### Fixed
- MergeSMR broken [`#710`](https://github.com/anatawa12/AvatarOptimizer/pull/710)

## [1.6.0-beta.6] - 2023-11-11
### Changed
- Remove Unused Objects removes meaningless Animators and Renderers [`#709`](https://github.com/anatawa12/AvatarOptimizer/pull/709)
  - Renderers without Mesh and Animators without AnimatorController is meaningless

### Fixed
- Enablement mismatched renderers are merged instead of matched renderers [`#705`](https://github.com/anatawa12/AvatarOptimizer/pull/705)

## [1.6.0-beta.5] - 2023-11-08
### Fixed
- PPtr / Object animation not working [`#703`](https://github.com/anatawa12/AvatarOptimizer/pull/703)

## [1.6.0-beta.4] - 2023-11-08
### Fixed
- eyelids BlendShape Removed error for non-AAO avatars [`#696`](https://github.com/anatawa12/AvatarOptimizer/pull/696)
- bounds can be changed in apply on play if updateWhenOffscreen is true [`#697`](https://github.com/anatawa12/AvatarOptimizer/pull/697)
- Animations for most components under MergeBone is not mapped [`#700`](https://github.com/anatawa12/AvatarOptimizer/pull/700)

## [1.6.0-beta.3] - 2023-11-06
### Added
- Remove Zero Sized Polygons [`#659`](https://github.com/anatawa12/AvatarOptimizer/pull/659)
- Add support for UniVRM components [`#653`](https://github.com/anatawa12/AvatarOptimizer/pull/653)
- Support for Mesh Topologies other than Triangles [`#692`](https://github.com/anatawa12/AvatarOptimizer/pull/692)
- Skip enablement mismatched Renderers in Merge Skinned Mesh [`#670`](https://github.com/anatawa12/AvatarOptimizer/pull/670)
  - This is now enabled by default for newly added Merge Skinned Mesh.

### Changed
- When you're animating activeness/enablement of source renderers, warning is shown since this release [`#675`](https://github.com/anatawa12/AvatarOptimizer/pull/675)

### Fixed
- proxy animation can be modified [`#678`](https://github.com/anatawa12/AvatarOptimizer/pull/678)
- complex shader with SkinnedMeshRenderer without Bones Brokebn [`#694`](https://github.com/anatawa12/AvatarOptimizer/pull/694)

## [1.6.0-beta.2] - 2023-10-31
### Added
- Small performance improve [`#641`](https://github.com/anatawa12/AvatarOptimizer/pull/641)
- Ability to prevent changing enablement of component [`#668`](https://github.com/anatawa12/AvatarOptimizer/pull/668)

### Changed
- All logs passed to ErrorReport is now shown on the console log [`#643`](https://github.com/anatawa12/AvatarOptimizer/pull/643)
- Improved Behaviour with multi-material multi pass rendering [`#662`](https://github.com/anatawa12/AvatarOptimizer/pull/662)
  - Previously, multi-material multi pass rendering are flattened.
  - Since 1.6, flattened if component doesn't support that.
- BREAKING API CHANGES: Behaviour components are renamed to HeavyBehaviour [`#668`](https://github.com/anatawa12/AvatarOptimizer/pull/668)

### Removed
- Preventing removing `IEditorOnly` in callback order -1024 [`#658`](https://github.com/anatawa12/AvatarOptimizer/pull/658)
  - This is no longer needed since 1.5.0 but I forgot to remove so I removed in 1.6

### Fixed
- Prefab blinks when we see editor of PrefabSafeSet of prefab asset [`#645`](https://github.com/anatawa12/AvatarOptimizer/pull/645) [`#664`](https://github.com/anatawa12/AvatarOptimizer/pull/664)
- Fixes in 1.5.9 [`#654`](https://github.com/anatawa12/AvatarOptimizer/pull/654)

## [1.5.10] - 2023-11-04
### Fixed
- RigidBody Joint can be broken [`#683`](https://github.com/anatawa12/AvatarOptimizer/pull/683)

## [1.5.9] - 2023-10-29
## [1.5.9-rc.1] - 2023-10-28
### Fixed
- Animation clip length can be changed [`#647`](https://github.com/anatawa12/AvatarOptimizer/pull/647)

## [1.6.0-beta.1] - 2023-10-25
### Added
- Public API for registering component information [`#623`](https://github.com/anatawa12/AvatarOptimizer/pull/623)
- Documentation for developers about compatibility with Avatar Optimizer [`#623`](https://github.com/anatawa12/AvatarOptimizer/pull/623)
- Disabling PhysBone animation based on mesh renderer enabled animation [`#640`](https://github.com/anatawa12/AvatarOptimizer/pull/640)
  - If you toggles your clothes with simple toggle, PhysBones on the your avatar will also be toggled automatically!

### Removed
- Legacy GC [`#633`](https://github.com/anatawa12/AvatarOptimizer/pull/633)

### Fixed
- Improve support of newer Unity versions [`#608`](https://github.com/anatawa12/AvatarOptimizer/pull/608)
- Improve support of projects without VRCSDK [`#609`](https://github.com/anatawa12/AvatarOptimizer/pull/609) [`#625`](https://github.com/anatawa12/AvatarOptimizer/pull/625) [`#627`](https://github.com/anatawa12/AvatarOptimizer/pull/627)

## [1.5.8] - 2023-10-20
## [1.5.8-rc.1] - 2023-10-20
### Fixed
- warning about VRCTestMarker when Build & Test [`#628`](https://github.com/anatawa12/AvatarOptimizer/pull/628)

## [1.5.7] - 2023-10-19
## [1.5.7-beta.1] - 2023-10-19
### Added
- Add compatibility for VRCQuestTools [`#619`](https://github.com/anatawa12/AvatarOptimizer/pull/619)

### Fixed
- AutoFreezeBlendShape will freeze BlendShapes with editor value instead of animated constant [`#622`](https://github.com/anatawa12/AvatarOptimizer/pull/622)

## [1.5.6] - 2023-10-17
## [1.5.6-rc.1] - 2023-10-17
### Removed
- Error for Read/Write Mesh off Mesh [`#615`](https://github.com/anatawa12/AvatarOptimizer/pull/615)
  - Since AAO creates Mesh every time, no more error is required!

### Fixed
- BindPose Optimization may break mesh with scale 0 bone [`#612`](https://github.com/anatawa12/AvatarOptimizer/pull/612)
- Error from Preview System when opening inspector of GameObject without SkinnedMeshRenderer [`#613`](https://github.com/anatawa12/AvatarOptimizer/pull/613)

## [1.5.6-beta.2] - 2023-10-16
### Changed
- Make no-op as possible if no AAO component attached for your avatar [`#603`](https://github.com/anatawa12/AvatarOptimizer/pull/603)
- Error Report window is refreshed after exiting play mode [`#606`](https://github.com/anatawa12/AvatarOptimizer/pull/606)

### Fixed
- Update notice may show incorrect version [`#602`](https://github.com/anatawa12/AvatarOptimizer/pull/602)
- `Preview` button is not disabled even if mesh is none [`#605`](https://github.com/anatawa12/AvatarOptimizer/pull/605)

## [1.5.6-beta.1] - 2023-10-16
### Fixed
- Multi-frame BlendShape can be broken [`#601`](https://github.com/anatawa12/AvatarOptimizer/pull/601)

## [1.5.5] - 2023-10-15
## [1.5.5-rc.1] - 2023-10-15
### Fixed
- BlendShape can be broken with MergeBone Optimization [`#599`](https://github.com/anatawa12/AvatarOptimizer/pull/599)

## [1.5.5-beta.1] - 2023-10-15
### Fixed
- Constraints and Animations can be broken with Automatic MergeBone [`#594`](https://github.com/anatawa12/AvatarOptimizer/pull/594)
- NRE with SMR with None with preview system [`#596`](https://github.com/anatawa12/AvatarOptimizer/pull/596)
- Some Multi-Frame BlendShape broken [`#597`](https://github.com/anatawa12/AvatarOptimizer/pull/597)

## [1.5.4] - 2023-10-14
### Added
- Add compatibility for Satania's KiseteneEx [`#584`](https://github.com/anatawa12/AvatarOptimizer/pull/584)

### Changed
- Normal check is skipped for empty mesh [`#588`](https://github.com/anatawa12/AvatarOptimizer/pull/588)
- Meshes without Normal are shown on the normal existance mismatch warning [`#588`](https://github.com/anatawa12/AvatarOptimizer/pull/588)

### Fixed
- Error with MeshRenderer without MeshFilter [`#581`](https://github.com/anatawa12/AvatarOptimizer/pull/581)
- Preview not working with VRMConverter [`#582`](https://github.com/anatawa12/AvatarOptimizer/pull/582)
- AvatarMask about HumanoidBone broken [`#586`](https://github.com/anatawa12/AvatarOptimizer/pull/586)
- Unused Humanoid Bones can be removed [`#587`](https://github.com/anatawa12/AvatarOptimizer/pull/587)

## [1.5.3] - 2023-10-11
### Changed
- Ignore the warning instead of migration from 0.3.x or older [`#570`](https://github.com/anatawa12/AvatarOptimizer/pull/570)

### Fixed
- AnimatorOverrideController may not be proceed correctly [`#567`](https://github.com/anatawa12/AvatarOptimizer/pull/567)
- Unclear behaviour if we merged meshes with and without normals [`#569`](https://github.com/anatawa12/AvatarOptimizer/pull/569)

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
- Remove Mesh By BlendShape Editor now can set BlendShape weights to 0/100 [`#389`](https://github.com/anatawa12/AvatarOptimizer/pull/389)

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
- The new Remove Mesh By BlendShape component removes mesh data based on BlendShapes. [`#275`](https://github.com/anatawa12/AvatarOptimizer/pull/275)
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
- Show error with user friendly message if BlendShape for eyelids are removed / frozen. [`#253`](https://github.com/anatawa12/AvatarOptimizer/pull/253)

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

[Unreleased]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.9...HEAD
[1.6.9]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.9-beta.3...v1.6.9
[1.6.9-beta.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.9-beta.2...v1.6.9-beta.3
[1.6.9-beta.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.9-beta.1...v1.6.9-beta.2
[1.6.9-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.8...v1.6.9-beta.1
[1.6.8]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.7...v1.6.8
[1.6.7]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.7-beta.1...v1.6.7
[1.6.7-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.6...v1.6.7-beta.1
[1.6.6]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.5...v1.6.6
[1.6.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.5-rc.3...v1.6.5
[1.6.5-rc.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.5-rc.2...v1.6.5-rc.3
[1.6.5-rc.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.5-rc.1...v1.6.5-rc.2
[1.6.5-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.5-beta.1...v1.6.5-rc.1
[1.6.5-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.4...v1.6.5-beta.1
[1.6.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.4-beta.1...v1.6.4
[1.6.4-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.3...v1.6.4-beta.1
[1.6.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.2...v1.6.3
[1.6.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.2-rc.1...v1.6.2
[1.6.2-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.1...v1.6.2-rc.1
[1.6.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0...v1.6.1
[1.6.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-rc.4...v1.6.0
[1.6.0-rc.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-rc.3...v1.6.0-rc.4
[1.6.0-rc.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-rc.2...v1.6.0-rc.3
[1.6.0-rc.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-rc.1...v1.6.0-rc.2
[1.6.0-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-beta.12...v1.6.0-rc.1
[1.6.0-beta.12]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-beta.11...v1.6.0-beta.12
[1.6.0-beta.11]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-beta.10...v1.6.0-beta.11
[1.6.0-beta.10]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-beta.9...v1.6.0-beta.10
[1.6.0-beta.9]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-beta.8...v1.6.0-beta.9
[1.6.0-beta.8]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-beta.7...v1.6.0-beta.8
[1.6.0-beta.7]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-beta.6...v1.6.0-beta.7
[1.6.0-beta.6]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-beta.5...v1.6.0-beta.6
[1.6.0-beta.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-beta.4...v1.6.0-beta.5
[1.6.0-beta.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-beta.3...v1.6.0-beta.4
[1.6.0-beta.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-beta.2...v1.6.0-beta.3
[1.6.0-beta.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0-beta.1...v1.6.0-beta.2
[1.6.0-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.11...v1.6.0-beta.1
[1.5.11]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.11-beta.1...v1.5.11
[1.5.11-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.10...v1.5.11-beta.1
[1.5.10]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.9...v1.5.10
[1.5.9]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.9-rc.1...v1.5.9
[1.5.9-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.8...v1.5.9-rc.1
[1.5.8]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.8-rc.1...v1.5.8
[1.5.8-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.7...v1.5.8-rc.1
[1.5.7]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.7-beta.1...v1.5.7
[1.5.7-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.6...v1.5.7-beta.1
[1.5.6]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.6-rc.1...v1.5.6
[1.5.6-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.6-beta.2...v1.5.6-rc.1
[1.5.6-beta.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.6-beta.1...v1.5.6-beta.2
[1.5.6-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.5...v1.5.6-beta.1
[1.5.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.5-rc.1...v1.5.5
[1.5.5-rc.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.5-beta.1...v1.5.5-rc.1
[1.5.5-beta.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.4...v1.5.5-beta.1
[1.5.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.3...v1.5.4
[1.5.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.3-beta.1...v1.5.3
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
