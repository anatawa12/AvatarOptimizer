---
title: Asset Description
---

# Asset Description

Asset Description is the file to provide information of your assets for Avatar Optimizer.

## Why Asset Description is needed

Avatar Optimizer uses information from Asset Description to not excessively optimize user's avatar.

Current Asset Description can provide the following information:

- `Meaningless Components`\
  Components that should be ignored by Avatar Optimizer.
- `Parameters Read By External Tools`\
  Parameters that can be read by external tools, especially for OSC Tools.
- `Parameters Changed By External Tools`
  Parameters that can be changed by external tools, especially for OSC Tools.

We will describe why Asset Description is needed for each information below.

### Meaningless Components {#why-meaningless-components}

Avatar Optimizer has to know about all existing components in the Avatar to remove unnecessary ones.\
Avatar Optimizer v1.6.0 added [document to make your components compatible with AAO][make-component-compatible] and API for it, but
for in-place modification tools that do not process on build,
it's a little bit complicated to remove components by `IVRCSDKPreprocessAvatarCallback` instead of Avatar Optimizer.
Therefore, Asset Description was added in v1.7.0 as a simple mechanism to specify components that should be ignored by Avatar Optimizer at build time.

For non-destructive tools, we still recommend you to continue to remove components in `IVRCSDKPreprocessAvatarCallback` or your NDMF Pass to prevent Avatar Optimizer from accidentally removing components when the execution order is incorrect.

[make-component-compatible]: ../make-your-components-compatible-with-aao

### Parameters Read By Extenral Tools {#why-parameters-read-by-external-tools}

Components such as PhysBone and Contact Receiver will make parameters that can be read by OSC tools.
Such parameters are readable by the OSC tools without being defined on Animator Controller or Expression Parameter.\
Therefore, Avatar Optimizer is unable to determine whether those parameters are simply unused or intended to be read by the OSC tools.
Since undefined parameters are relatively rarely used by OSC Tools, components that make such parameters are removed if parameters are unused from anywhere on the avatar.

To prevent Avatar Optimizer from removing those parameters and components, you can specify the parameters that are read by OSC Tools in Asset Description.

### Parameters Changed By External Tools {#why-parameters-written-by-external-tools}

Currently this information is not actually used by Avatar Optimizer, but it is planned to be used in the future.

Avatar Optimizer is planned to optimize Animator Controller by analyzing unchanged parameters.
However, if the parameters are changed by external tools, this optimization will break effects of the external tools.

To prevent this, you can specify the parameters that are changed by external tools in Asset Description.

## Create Asset Description {#create-asset-description}

To create Asset Description, select `Create/Avatar Optimizer/Asset Description` from right-click menu in the Project window.\
Avatar Optimizer searches from all files, so the name and location of Asset Description are free.

## Editing Asset Description {#edit-asset-description}

![asset-description-inspector](asset-description-inspector.png)

### Comment {#comment}

The comment field is for writing notes.\
Avatar Optimizer ignores comments.

### Meaningless Components {#meaningless-components}

Meaningless Components is the list of component types that you want Avatar Optimizer to ignore.\
Please specify the Script Asset of the component.
Avatar Optimizer ignores the component of the specified Script Asset type and its subclass.

In Asset Description, as with the components in the Scene, types are stored in the form of guid and fileID of the Script Asset.\
Therefore, even if the class name is changed, the specification in Asset Description will work without any problems as long as the components in the Scene are not broken.

### Parameters Read By External Tools {#parameters-read-by-external-tools}

Specify the parameters that are read by external tools.

Please read [above](#why-parameters-read-by-external-tools) for more information.

### Parameters Changed By External Tools {#parameters-changed-by-external-tools}

Specify the parameters that are changed by external tools.

Please read [above](#why-parameters-written-by-external-tools) for more information.
