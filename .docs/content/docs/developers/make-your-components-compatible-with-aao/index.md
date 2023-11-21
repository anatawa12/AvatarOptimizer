---
title: Make your components compatible with Avatar Optimizer
---

# Make your components compatible with Avatar Optimizer

This page describes the following two things.

- When can components be incompatible with Avatar Optimizer?
- How to improve the compatibility?

If you have some question, please feel free to ask [`@anatawa12@misskey.niri.la` on fediverse][fediverse].

## When can components be incompatible with Avatar Optimizer? {#when-incompatible}

If your components are on the avatar and still exist when Avatar Optimizer processes it, 
your components can be incompatible with Avatar Optimizer.

Since Avatar Optimizer has Garbage Collection system for Components and others, Avatar Optimizer has to 
know about all existing components in the Avatar at the optimization.

To avoid problem with unknown components, Avatar Optimizer currently assumes unknown components
- have some side-effects.
- will have dependency relationship to all components referenced in the component.
  (They can be changed in the future.)

However, the assumption can be incorrect, so Avatar Optimizer will generate the following warning.

![unknown-component-warning](unknown-component-warning.png)

## How to improve the compatibility? {#improve-compatibility}

Please remove your components before Avatar Optimizer processes as much as possible.
If you cannot remove some components, please register them to Avatar Optimizer.

### Removing your components {#remove-component}

You can remove your components with several ways.

If your tool is a non-destructive tool based on NDMF[^NDMF], removing your components before the Optimization phase
or before `com.anatawa12.avatar-optimizer` plugin (with [`BeforePlugin`][ndmf-BeforePlugin]) 
in the Optimization phase is recommended.

If your tool is a non-destructive tool not based on NDMF[^NDMF], removing your components before 
the NDMF's Optimization phase is recommended.
In this case, current NDMF executes Optimization phase in order `-1025`, which is JUST before VRCSDK's `RemoveAvatarEditorOnly`
callback, so your tool should remove components with `IVRCSDKPreprocessAvatarCallback` with smaller `callbackOrder`.

If your components holds some information for your tool and has no meaning on the build time,
removing your components before Avatar Optimizer processes with `IVRCSDKPreprocessAvatarCallback` is recommended.
See above for the ordering of `IVRCSDKPreprocessAvatarCallback`.

### Registering your components {#register-component}

If your tool wants to keep your component after Avatar Optimizer processes,
you can register your components to Avatar Optimizer to tell about your components.

First, to call APIs of Avatar Optimizer, please make an assembly definition file[^asmdef] if your tool doesn't have.

Next, add `com.anatawa12.avatar-optimizer.api.editor` to assembly references in asmdef file.\
If your tool doesn't want to depends on Avatar Optimizer, please use [Version Defines].
Because Avatar Optimizer didn't have public API prior to 1.6.0 and will break in 2.0.0, 
it's recommended to add version range like `[1.6,2.0)`
(or stricter like `[1.7,2.0)` when you need new APIs that can be available in the future).

![version-defines.png](version-defines.png)

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
In `CollectDependency`, you should register build-time or run-time dependencies of your component.\
Please refer xmldoc and method name for more datails.

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
