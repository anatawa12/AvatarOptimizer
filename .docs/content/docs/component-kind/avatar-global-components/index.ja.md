---
title: Avatar Global Components
weight: 10
---

# Avatar Global Components

![avatar-root.png](avatar-root.png)

Avatar Global Componentはアバターのルートに追加することでアバター全体に作用するコンポーネントです。
"アバターのルート"とは、アバターの最も上の階層にあるGameObjectのことです。
例えばVRChat用アバターの場合、VRC Avatar DescriptorコンポーネントがあるGameObjectが"アバターのルート"です。
Avatar Global Componentをアバターのルート以外に追加した場合、インスペクター上にエラーが表示され、コンポーネントは一切動作しません。

以下のコンポーネントがAvatar Global Componentです。

- [Trace And Optimize](../../reference/trace-and-optimize)
- [UnusedBonesByReferencesTool](../../reference/unused-bones-by-references-tool)
