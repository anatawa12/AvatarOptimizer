---
title: OSC Gimmick Compatibility
---

# OSC Gimmick Compatibility

This page is for authors of avatar gimmicks that use OSC tools to read PhysBone or Contact Receiver parameters at runtime.

## The Problem {#the-problem}

PhysBone and Contact Receiver components expose parameters that OSC tools can read directly.
These parameters are accessible to external tools even when they are **not** declared in the avatar's Animator Controller or Expression Parameters.

Because of this, Avatar Optimizer cannot tell whether such parameters are genuinely unused or intentionally read by an OSC tool.
To stay safe and avoid breaking most avatars, Avatar Optimizer assumes parameters that are not referenced from an Animator Controller or Expression Parameters are unused, and removes the corresponding PhysBone / Contact Receiver components.

If your gimmick relies on an OSC tool reading those parameters, the components will be silently removed and the gimmick will stop working.

See also: [FAQ — PhysBones / Contact Receivers that are used in the OSC-based gimmick are not working](../../faq/#physbones-contact-receivers-that-are-used-in-the-osc-based-gimmick-are-not-working)

## What You Need To Do {#what-to-do}

To prevent Avatar Optimizer from removing your components, create an [Asset Description] and declare every parameter your OSC tool **reads** in the `Parameters Read By External Tools` list.

### Step-by-step guide {#step-by-step}

1. **Identify all parameters your OSC tool reads.**\
   List every PhysBone or Contact Receiver parameter name that your external tool reads at runtime.
   These are the parameter names that appear in the OSC address (e.g. `/avatar/parameters/<ParameterName>`).

2. **Create an Asset Description.**\
   In the Unity Project window, right-click and choose:\
   `Create > Avatar Optimizer > Asset Description`\
   The name and location of the file are free — Avatar Optimizer searches all files in the project.

3. **Add the parameters to `Parameters Read By External Tools`.**\
   Open the Asset Description in the Inspector and add each parameter name from step 1 to the `Parameters Read By External Tools` list.\
   See [Parameters Read By External Tools][asset-description-read] for details.

4. **Distribute the Asset Description with your gimmick.**\
   Include the Asset Description file in your product's package or prefab so that users automatically get the correct configuration when they install your gimmick.\
   See [Distributing Prefabs](../distributing-prefabs/) for general guidance on distributing assets.

### Parameters changed by your OSC tool {#parameters-changed}

If your OSC tool also **writes** (changes) avatar parameters, you can declare those in `Parameters Changed By External Tools`.\
Note that Avatar Optimizer does **not** currently use this information, but plans to use it for future optimizations.
Declaring changed parameters now means your gimmick will remain compatible when that optimization is implemented.

## Bundling Asset Description with AAO {#bundling}

If your gimmick is published or sold commercially, the Avatar Optimizer maintainer would like to bundle your Asset Description directly with Avatar Optimizer to improve out-of-the-box compatibility.

If you are interested, please contact the maintainer on [GitHub], [NDMF Discord], [Fediverse (Misskey / Mastodon)][Fediverse], or [Twitter].

[Asset Description]: ../asset-description/
[asset-description-read]: ../asset-description/#parameters-read-by-external-tools
[GitHub]: https://github.com/anatawa12/AvatarOptimizer
[NDMF Discord]: https://discord.gg/dV4cVpewmM
[Fediverse]: https://misskey.niri.la/@anatawa12
[Twitter]: https://twitter.com/anatawa12_vrc
