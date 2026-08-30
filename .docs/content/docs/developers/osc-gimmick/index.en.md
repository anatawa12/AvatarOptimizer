---
title: Make your OSC gimmicks compatible with Avatar Optimizer
---

# Make your OSC gimmicks compatible with Avatar Optimizer

This page is for authors of avatar gimmicks that use OSC tools to read or write PhysBone or Contact Receiver parameters at runtime.

## Why can OSC Gimmicks be incompatible with Avatar Optimizer? {#why}

### Parameters Read by Your OSC Tool {#why-read}

Some modern avatars have their own gimmicks based on PhysBone / Contact Receiver components, so those components will be forgotten to remove in most cases.\
Therefore, Avatar Optimizer is designed to remove such components if they does not effect the avatar through Animators or Expression Parameters.[^fake-usage]

However, PhysBone and Contact Receiver components expose parameters that OSC tools can read directly.\
Avatar Optimizer cannot recognize whether such parameters are truly unused or intended to be read by an OSC tool, so Avatar Optimizer might remove those components that are intended for OSC Gimmicks interaction.

### Parameters Changed by Your OSC Tool {#why-change}

Avatar Optimizer is also planning to optimize Animator Controllers by analyzing parameters that are never changed at runtime.\
However, if an OSC tool or a VRCParameterDriver changes a parameter that Avatar Optimizer believes to be constant, this optimization could break the gimmick.

Although Avatar Optimizer does not implement this optimization yet, please declare such parameters so that your gimmick remains compatible when the optimization is implemented in the future.

## What You Should Do {#what-to-do}

To prevent Avatar Optimizer from incorrectly optimizing your gimmick, please create an [Asset Description] and declare every parameter your OSC tool reads from or writes to in the `Parameters Read By External Tools` or `Parameters Changed By External Tools` list, respectively.

Then, please distribute the Asset Description along with your avatar or gimmick.\
Having an Asset Description file in a project without Avatar Optimizer installed does not cause any problems, so distributing it with your gimmick does not make your gimmick depend on Avatar Optimizer.[^missing-asset]

### Step-by-step Guide {#step-by-step}

1. Please identify all parameters your OSC tool reads from or writes to.

   List every parameter name that your external tool reads from or writes to at runtime.\
   Asset Description supports regular expressions, so exact names are not always required.\
   For example, you can specify a pattern that matches all parameters with a prefix such as `FooHaptics/OSC/`.

2. Please create an Asset Description.

   In the Unity Project window, right-click and choose `Create > Avatar Optimizer > Asset Description`.\
   The name and location of the file are up to you.

3. Please add the parameters to `Parameters Read By External Tools` and/or `Parameters Changed By External Tools`.

   Open the Asset Description in the Inspector and add each parameter from step 1 to the appropriate list.\
   Please note that this mechanism cannot detect whether your gimmick is installed or not, so the configuration applies to every avatar in the project.

   When using regular expressions, please make your parameter definitions as specific as possible.

4. Please distribute the Asset Description with your gimmick.

   Include the Asset Description file in your product's package so that users automatically get the correct configuration when they install your gimmick.\
   Distributing an Asset Description file with your gimmick does not make your gimmick depend on Avatar Optimizer, so users do not need to install Avatar Optimizer.

For more details on Asset Description, please refer to the [Asset Description] page.

[Asset Description]: ../asset-description/
[asset-description-read]: ../asset-description/#parameters-read-by-external-tools

[^fake-usage]: Even when you tries to add animations that looks like have effects but actually not, like animating some dummy GameObjects, we might remove such GameObjects and animations in the future. Therefore, please do not try trick Avatar Optimizer.

[^missing-asset]: An Asset Description is only problematic if it is used by a poorly implemented tool that does not handle unknown Scriptable Objects correctly. As far as we know, no such tools currently exist. Please let us know if you encounter any compatibility issues.
