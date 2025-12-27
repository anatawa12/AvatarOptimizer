---
title: Make your components compatible with Avatar Optimizer
---

# Make your components compatible with Avatar Optimizer

This page describes the following two things.

- When can components be incompatible with Avatar Optimizer?
- How to improve the compatibility?

If you have some question, please feel free to ask on [Fediverse (Misskey / Mastodon)][fediverse] or [NDMF Discord].

## When can components be incompatible with Avatar Optimizer? {#when-incompatible}

If your components are on the avatar and still exist when Avatar Optimizer processes it,
your components can be incompatible with Avatar Optimizer.

Avatar Optimizer is designed to process at the last of the build process,
components unknown to Avatar Optimizer is not supported.

For example, Avatar Optimizer has Garbage Collection system for Components and others.
To correctly remove unused components, and correctly keep used components,
Avatar Optimizer has to know about all existing components in the Avatar at the optimization.

To avoid problem with unknown components, Avatar Optimizer currently assumes unknown components
- have to be kept if the component can be enabled and active at runtime.
  - This is because Avatar Optimizer assumes unknown components are runtime components.
- will have dependency relationship to all components referenced in the component.

(Those assumptions above can be changed in the future.)

However, those assumptions can be incorrect, so Avatar Optimizer will generate a warning like below.

![unknown-component-warning](unknown-component-warning.png)

## How to improve the compatibility? {#improve-compatibility}

To improve the compatibility, you may implement one of the following methods.

1. Remove your components before Avatar Optimizer processes as much as possible.

   If your component is not working at runtime, (in other words, it's a build-time or edit mode only component),
   it's mostly better for your tool to process avatar before Avatar Optimizer processes,
   and remove your components before Avatar Optimizer processes.

   Please refer [section below](#remove-component) for more details.

   Avatar Optimizer internally uses this method for most Avatar Optimizer components, 
   that will be processed before Trace and Optimize.

2. Register your components to Avatar Optimizer with API

   If your component is working at runtime, or your tool actually wants to keep your components for processing avatar after Avatar Optimizer processes,
   you can register your components to Avatar Optimizer to tell about your components.

   Please refer [section below](#register-component) for more details.

   Avatar Optimizer internally uses this method to keep some components that are processed after Trace and Optimize, 
   and components from Unity, VRCSDK, and other avatar platform components.

3. Register your components as no problems to remove with Asset Description.

   Since Avatar Optimizer v1.7.0, you can use [Asset Description] to register components only for holding data
   for edit-mode tools, that doesn't affects on build or at runtime.
   If your tool process nothing at build time or runtime, you can use this to register your components instead of
   removing your components before Avatar Optimizer processes.

   Please refer [Asset Description] for more details.

   If your tool process something at build time or runtime, registering with Asset Description is not recommended.
   If you use Asset Description for components that process something at build time or runtime, it may cause unexpectedly
   removing your components and your tool not working properly when the execution order is incorrect or unexpectedly changed.

   Avatar Optimizer internally uses this method to keep compatibility with well-known edit-mode tools.

### Removing your components {#remove-component}

There are several ways to process and remove your components from avatar before Avatar Optimizer processes on build. You can use [`DestroyImmediate`][DestroyImmediate] method for removing your components.

If your tool is a non-destructive tool based on NDMF[^NDMF], you can remove your components before the phases
prior to the Optimizing phase of NDMF or before `com.anatawa12.avatar-optimizer` plugin
(with [`BeforePlugin`][ndmf-BeforePlugin]) in the Optimizing phase.
If your tool removes your components in Optimizing phase, it's highly recommended to specify [`BeforePlugin`][ndmf-BeforePlugin]
even if your default callback order is before `com.anatawa12.avatar-optimizer` plugin.

If your tool is a non-destructive tool not based on NDMF[^NDMF], removing your components before
the NDMF's Optimizing phase is recommended.
In this case, current NDMF executes Optimizing phase in order `-1025`, which is JUST before VRCSDK's `RemoveAvatarEditorOnly`
callback, so your tool should remove components with `IVRCSDKPreprocessAvatarCallback` with `callbackOrder` smaller than `-1025`.

If your components is only for holding data for your edit-mode tool and doesn't affects on build or at runtime,
you can remove your components in `IVRCSDKPreprocessAvatarCallback` as described above, or
you can simply use [Asset Description] to register your components as safe-to-remove components.

[DestroyImmediate]: https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Object.DestroyImmediate.html

### Registering your components {#register-component}

If you want to keep your component after Avatar Optimizer processes,
you can register your components to Avatar Optimizer to tell about your components.

First, to call APIs of Avatar Optimizer, please make an assembly definition file[^asmdef] if your tool doesn't have.

Next, add `com.anatawa12.avatar-optimizer.api.editor` to assembly references in asmdef file.\
If your tool doesn't want to depend on Avatar Optimizer, please use [Version Defines].
Because Avatar Optimizer didn't have public API prior to 1.6.0 and will break in 2.0.0,
it's recommended to add version range like `[1.6,2.0)`
(or stricter like `[1.7,2.0)` when you need new APIs that can be available in the future).

![version-defines.png](../version-defines.png)

Then, define `ComponentInformation` for your component in your assembly.

```csharp
#if AVATAR_OPTIMIZER && UNITY_EDITOR

[ComponentInformation(typeof(YourComponent))]
internal class YourComponentInformation : ComponentInformation<YourComponent>
{
    protected override void CollectMutations(YourComponent component, ComponentMutationsCollector collector)
    {
        // call methods on the collector to tell about the component
    }

    protected override void CollectDependency(YourComponent component, ComponentDependencyCollector collector)
    {
        // call methods on the collector to tell about the component
    }
}

#endif
```

In `CollectMutations`, you should register any mutation your component may do.\
In `CollectDependency`, you should register build-time or run-time dependencies and related information of your component.\
Please read xmldoc of each methods for more details.

### Use Asset Description {#asset-description}

Please refer [Asset Description] for more details.

[fediverse]: https://misskey.niri.la/@anatawa12
[ndmf-BeforePlugin]: https://ndmf.nadena.dev/api/nadena.dev.ndmf.fluent.Sequence.html#nadena_dev_ndmf_fluent_Sequence_BeforePlugin_System_String_System_String_System_Int32_
[register-component]: #register-component

[^asmdef]: The file defines assembly other than Assembly-CSharp. Please refer [unity docs](https://docs.unity3d.com/2019.4/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html).
[^NDMF]: [NDMF], Non-Destructive Modular Framework, is a framework for running non-destructive build plugins when
building avatars by bdunderscore. Avatar Optimizer uses that framework for compatibility
with many non-destructive tools based on NDMF.

[NDMF]: https://ndmf.nadena.dev/
[modular-avatar]: https://modular-avatar.nadena.dev/
[Version Defines]: https://docs.unity3d.com/2019.4/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html#define-symbols
[Asset Description]: ../asset-description
[NDMF Discord]: https://discord.gg/dV4cVpewmM
