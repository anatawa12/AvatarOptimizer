---
title: OSC Gimmick Compatibility
---

# OSC Gimmick Compatibility

This page is for authors of avatar gimmicks that use OSC tools to read PhysBone or Contact Receiver parameters at runtime.

## TL;DR {#tldr}

- Create an [Asset Description] and add every parameter your OSC tool **reads** to `Parameters Read By External Tools`.
- If your OSC tool also **writes** parameters, add those to `Parameters Changed By External Tools`.
- Please include the Asset Description file in your gimmick package so users get the correct configuration automatically.

## Why This Is Needed {#why}

### Parameters read by your OSC tool {#why-read}

PhysBone and Contact Receiver components expose parameters that OSC tools can read directly.
These parameters are accessible to external tools even when they are **not** declared in the avatar's Animator Controller or Expression Parameters.

Because of this, Avatar Optimizer cannot tell whether such parameters are genuinely unused or intentionally read by an OSC tool.
To stay safe and avoid breaking most avatars, Avatar Optimizer assumes parameters that are not referenced from an Animator Controller or Expression Parameters are unused, and removes the corresponding PhysBone / Contact Receiver components.

If your gimmick relies on an OSC tool reading those parameters, the components will be silently removed and the gimmick will stop working.

See also: [FAQ — PhysBones / Contact Receivers that are used in the OSC-based gimmick are not working](../../faq/#physbones-contact-receivers-that-are-used-in-the-osc-based-gimmick-are-not-working)

### Parameters written by your OSC tool {#why-write}

Avatar Optimizer is also planned to optimize Animator Controllers by analyzing parameters that are never changed at runtime.
However, if an OSC tool or a VRCParameterDriver changes a parameter that Avatar Optimizer believes is constant, the optimization will break the gimmick.

Although Avatar Optimizer does not yet use `Parameters Changed By External Tools` for this optimization, please declare such parameters now so that your gimmick will remain compatible when the optimization is implemented in the future.

## What You Should Do {#what-to-do}

To prevent Avatar Optimizer from removing your components, please create an [Asset Description] and declare every parameter your OSC tool **reads** in the `Parameters Read By External Tools` list.

Note: Asset Description is a ScriptableObject that Avatar Optimizer reads at build time.
**It does not create a runtime dependency on Avatar Optimizer**, so distributing it with your gimmick is safe even for users who do not have Avatar Optimizer installed.

### Step-by-step guide {#step-by-step}

1. **Please identify all parameters your OSC tool reads.**\
   Please list every PhysBone or Contact Receiver parameter name that your external tool reads at runtime.
   These are the parameter names that appear in the OSC address (e.g. `/avatar/parameters/<ParameterName>`).

2. **Please create an Asset Description.**\
   In the Unity Project window, right-click and choose:\
   `Create > Avatar Optimizer > Asset Description`\
   The name and location of the file are free — Avatar Optimizer searches all files in the project.

3. **Please add the parameters to `Parameters Read By External Tools`.**\
   Open the Asset Description in the Inspector and add each parameter name from step 1 to the `Parameters Read By External Tools` list.\
   If there are many parameters that follow a naming pattern, a regular expression (regex) can be used to match them all at once.\
   See [Parameters Read By External Tools][asset-description-read] for details.

4. **Please distribute the Asset Description with your gimmick.**\
   Please include the Asset Description file in your product's package or prefab so that users automatically get the correct configuration when they install your gimmick.\
   See [Distributing Prefabs](../distributing-prefabs/) for general guidance on distributing assets.

### Parameters changed by your OSC tool {#parameters-changed}

If your OSC tool also **writes** (changes) avatar parameters, please declare those in `Parameters Changed By External Tools`.\
Note that Avatar Optimizer does **not** currently use this information, but plans to use it for future Animator Controller optimizations, and also for parameters changed by VRCParameterDriver.
Please declare changed parameters now so that your gimmick will remain compatible when those optimizations are implemented.

[Asset Description]: ../asset-description/
[asset-description-read]: ../asset-description/#parameters-read-by-external-tools
