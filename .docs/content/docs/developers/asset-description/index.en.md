---
title: Asset Description
---

# Asset Description

Asset Description is the file to provide Avatar Optimizer information of  your assets.

## Why Asset Description is needed

Avatar Optimizer needs to know all components in your avatar to remove unnecessary components.\
Avatar Optimizer v1.6.0 added [document to make your components compatible with AAO][make-component-compatible] and API for it, but
for tools that is not a non-destructive and does not process during build, 
it's a little bit complicated to remove components by `IVRCSDKPreprocessAvatarCallback` instead of Avatar Optimizer.
Therefore, Asset Description was added in 1.7.0 as a simple mechanism to specify components that should be ignored by Avatar Optimizer at build time.

For non-destructive tools, we still recommend you continue to remove components in `IVRCSDKPreprocessAvatarCallback` to prevent AvatarOptimizer from accidentally removing components when the execution order is incorrect.

[make-component-compatible]: ../make-your-components-compatible-with-aao

## Create Asset Description {#create-asset-description}

To create Asset Description, select `Create/Avatar Optimizer/Asset Description` from the Project window right-click menu.\
Avatar Optimizer searches from all files, so the name and location of Asset Description are free.

## Editing Asset Description {#edit-asset-description}

![asset-description-inspector](asset-description-inspector.png)

### Comment {#comment}

The comment field is for writing notes.\
Avatar Optimizer ignores comments.

### Meaningless Components {#meaningless-components}

Meaningless Components is the list of component types that you want Avatar Optimizer to ignore.\
Please specify the Script Asset of the component.
Avatar Optimizer ignores the component of the specified Script Asset type or its subclass.

In Asset Description, as with the components in the Scene, types are stored in the form of guid and fileID of the Script Asset.\
Therefore, even if the class name is changed, the specification in Asset Description will work without any problem as long as the components in the Scene are not broken.
