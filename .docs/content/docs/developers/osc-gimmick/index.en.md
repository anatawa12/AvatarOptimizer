---
title: Make your OSC gimmicks compatible with Avatar Optimizer
---

# Make your OSC gimmicks compatible with Avatar Optimizer

This page is for authors of avatar gimmicks that use OSC tools to read or write PhysBone or Contact Receiver parameters at runtime.

## Why OSC Gimmicks can become incompatible with Avatar Optimizer {#when}

### Parameters Read by Your OSC Tool {#why-read}

PhysBone and Contact Receiver components expose parameters that OSC tools can read directly.
These parameters are accessible to external tools even when they are not declared in the avatar's Animator Controller or Expression Parameters.
In addition, even when parameters are declared in avatar's Animator Controller or Expression Parameters, they might be removed by Avatar Optimizer if it's actually not used.[^fake-usage]

Because of this, Avatar Optimizer cannot tell whether such parameters are genuinely unused or intentionally read by an OSC tool.

Some modern avatars have gimmicks based on PhysBone or Contact Receiver components.
Users may forget to remove these components after removing a gimmick.
Avatar Optimizer assumes that parameters which are not used by an Animator Controller and are not defined as Synced Expression Parameters are unused, and may therefore remove the corresponding PhysBone or Contact Receiver components.

As a result, if your gimmick relies on an OSC tool reading these parameters, the components may be silently removed and the gimmick will stop working.

### Parameters Changed by Your OSC Tool {#why-change}

Avatar Optimizer is also planned to optimize Animator Controllers by analyzing parameters that are never changed at runtime.

However, if an OSC tool or a VRCParameterDriver changes a parameter that Avatar Optimizer believes to be constant, this optimization could break the gimmick.

Although Avatar Optimizer does not implement this optimization yet, please declare such parameters so that your gimmick remains compatible when the optimization is implemented in the future.

## What You Should Do {#what-to-do}

To prevent Avatar Optimizer from incorrectly optimizing your gimmick, we ask that you create an [Asset Description] and declare every parameter your OSC tool reads from or writes to in the `Parameters Read By External Tools` or `Parameters Changed By External Tools` list, respectively.

Please distribute the Asset Description along with your avatar or gimmick. Having an Asset Description in a project without Avatar Optimizer installed does not cause any problems, so distributing it with your gimmick does not make your gimmick depend on Avatar Optimizer.[^missing-asset]

### Step-by-step Guide {#step-by-step}

1. Please identify all parameters your OSC tool reads from or writes to.

   List every Avatar Parameter name that your external tool reads from or writes to at runtime.

   The Asset Description supports regular expressions, so exact names are not necessary. For example, you can specify a pattern that matches all parameters with a prefix such as `FooHaptics/OSC/`.

2. Please create an Asset Description.

   In the Unity Project window, right-click and choose `Create > Avatar Optimizer > Asset Description`.

   The name and location of the file are up to you.

3. Please add the parameters to `Parameters Read By External Tools` and `Parameters Changed By External Tools`.

   Open the Asset Description in the Inspector and add each parameter from step 1 to the appropriate list.

   Please note that this mechanism cannot detect whether your gimmick is installed or not, so the configuration applies to every avatar in the project.

   When using regular expressions, please make your parameter definitions as specific as possible.

4. Please distribute the Asset Description with your gimmick.

   Include the Asset Description file in your product's package so that users automatically get the correct configuration when they install your gimmick.

   Distributing an Asset Description with your gimmick does not make your gimmick depend on Avatar Optimizer, so users do not need to install Avatar Optimizer.

For more details on Asset Description, please refer to the [Asset Description] page.

[Asset Description]: ../asset-description/
[asset-description-read]: ../asset-description/#parameters-read-by-external-tools

[^fake-usage]: Even when you tries to add animation that looks like have effects but actually not, like animating dummy GameObject, we might remove such GameObject and animation in the future. Therefore, please do not try trick Avatar Optimizer.

[^missing-asset][^1]: An Asset Description is only problematic if it is used by a poorly designed tool that does not handle unknown Asset Descriptions correctly. As far as we know, no such tools currently exist. Please let us know if you encounter any compatibility issues.
