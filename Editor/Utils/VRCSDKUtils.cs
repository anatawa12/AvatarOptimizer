#if AAO_VRCSDK3_AVATARS

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace Anatawa12.AvatarOptimizer
{
    internal static partial class VRCSDKUtils
    {
        public static Transform GetTarget(this VRCPhysBoneBase physBoneBase) =>
            physBoneBase.rootTransform ? physBoneBase.rootTransform : physBoneBase.transform;

        public static IEnumerable<Transform> GetAffectedTransforms(this VRCPhysBoneBase physBoneBase)
        {
            var ignores = new HashSet<Transform>(physBoneBase.ignoreTransforms);
            var queue = new Queue<Transform>();
            queue.Enqueue(physBoneBase.GetTarget());

            while (queue.Count != 0)
            {
                var transform = queue.Dequeue();
                yield return transform;

                foreach (var child in transform.DirectChildrenEnumerable())
                    if (!ignores.Contains(child))
                        queue.Enqueue(child);
            }
        }

        public static IEnumerable<Transform> GetAffectedLeafBones(this VRCPhysBoneBase physBoneBase)
        {
            var ignores = new HashSet<Transform>(physBoneBase.ignoreTransforms);
            var rootBone = physBoneBase.GetTarget();
            var queue = new Queue<Transform>();
            queue.Enqueue(rootBone);

            while (queue.Count != 0)
            {
                var transform = queue.Dequeue();

                var children = transform.DirectChildrenEnumerable().Where(t => !ignores.Contains(t));
                if (children.Count() == 0 && transform != rootBone)
                    yield return transform;

                foreach (var child in children)
                    queue.Enqueue(child);
            }
        }

        // https://creators.vrchat.com/avatars/#proxy-animations
        public static bool IsProxy(this AnimationClip clip) => clip.name.StartsWith("proxy_", StringComparison.Ordinal);

        public static int BoneChainLength(this VRCPhysBoneBase physBoneBase)
        {
            var length = physBoneBase.maxBoneChainIndex;
            if (physBoneBase.endpointPosition != Vector3.zero)
                length++;
            return length;
        }

        public static VRCAvatarDescriptor.AnimLayerType? ToAnimLayerType(
            this VRC_PlayableLayerControl.BlendableLayer layer)
        {
            switch (layer)
            {
                case VRC_PlayableLayerControl.BlendableLayer.Action:
                    return VRCAvatarDescriptor.AnimLayerType.Action;
                case VRC_PlayableLayerControl.BlendableLayer.FX:
                    return VRCAvatarDescriptor.AnimLayerType.FX;
                case VRC_PlayableLayerControl.BlendableLayer.Gesture:
                    return VRCAvatarDescriptor.AnimLayerType.Gesture;
                case VRC_PlayableLayerControl.BlendableLayer.Additive:
                    return VRCAvatarDescriptor.AnimLayerType.Additive;
                default:
                    return null;
            }
        }

        public static VRCAvatarDescriptor.AnimLayerType? ToAnimLayerType(
            this VRC_AnimatorLayerControl.BlendableLayer layer)
        {
            switch (layer)
            {
                case VRC_AnimatorLayerControl.BlendableLayer.Action:
                    return VRCAvatarDescriptor.AnimLayerType.Action;
                case VRC_AnimatorLayerControl.BlendableLayer.FX:
                    return VRCAvatarDescriptor.AnimLayerType.FX;
                case VRC_AnimatorLayerControl.BlendableLayer.Gesture:
                    return VRCAvatarDescriptor.AnimLayerType.Gesture;
                case VRC_AnimatorLayerControl.BlendableLayer.Additive:
                    return VRCAvatarDescriptor.AnimLayerType.Additive;
                default:
                    return null;
            }
        }
    }
}

#endif
