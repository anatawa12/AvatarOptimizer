---
title: Asset Description
---

# Asset Description

Asset Description is the file to provide information of your assets for Avatar Optimizer.

## Why Asset Description is needed

Avatar Optimizer has to know about all existing components in the Avatar to remove unnecessary ones.\
Avatar Optimizer v1.6.0 added [document to make your components compatible with AAO][make-component-compatible] and API for it, but
for in-place modification tools that do not process on build,
it's a little bit complicated to remove components by `IVRCSDKPreprocessAvatarCallback` instead of Avatar Optimizer.
Therefore, Asset Description was added in v1.7.0 as a simple mechanism to specify components that should be ignored by Avatar Optimizer at build time.

For non-destructive tools, we still recommend you to continue to remove components in `IVRCSDKPreprocessAvatarCallback` to prevent Avatar Optimizer from accidentally removing components when the execution order is incorrect.

[make-component-compatible]: ../make-your-components-compatible-with-aao

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
