#if AAO_VRCSDK3_AVATARS

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
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
                if (!children.Any() && transform != rootBone)
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

        public static AnimatorLayerMap<RuntimeAnimatorController> GetAvatarLayerControllers(
            VRCAvatarDescriptor descriptor)
        {
            var useDefaultLayers = !descriptor.customizeAnimationLayers;
            var controllers = new AnimatorLayerMap<RuntimeAnimatorController>();

            foreach (var layer in AnimatorLayerMap.ValidLayerTypes)
            {
                ref var loader = ref DefaultLayers[layer];
                var controller = loader.Value;
                if (controller == null)
                    throw new InvalidOperationException($"default controller for {layer} not found");
                controllers[layer] = controller;
            }

            foreach (var layer in descriptor.specialAnimationLayers.Concat(descriptor.baseAnimationLayers))
                controllers[layer.type] = GetPlayableLayerController(layer, useDefaultLayers)!;

            return controllers;
        }

        public static RuntimeAnimatorController? GetPlayableLayerController(VRCAvatarDescriptor.CustomAnimLayer layer,
            bool useDefault = false)
        {
            if (!useDefault && !layer.isDefault && layer.animatorController)
            {
                return layer.animatorController;
            }

            if (!AnimatorLayerMap.IsValid(layer.type)) return null;
            ref var loader = ref DefaultLayers[layer.type];
            var controller = loader.Value;
            if (controller == null)
                throw new InvalidOperationException($"default controller for {layer.type} not found");
            return controller;
        }

        private static readonly AnimatorLayerMap<CachedGuidLoader<AnimatorController>> DefaultLayers =
            new AnimatorLayerMap<CachedGuidLoader<AnimatorController>>
            {
                // vrc_AvatarV3LocomotionLayer
                [VRCAvatarDescriptor.AnimLayerType.Base] = "4e4e1a372a526074884b7311d6fc686b",
                // vrc_AvatarV3IdleLayer
                [VRCAvatarDescriptor.AnimLayerType.Additive] = "573a1373059632b4d820876efe2d277f",
                // vrc_AvatarV3HandsLayer
                [VRCAvatarDescriptor.AnimLayerType.Gesture] = "404d228aeae421f4590305bc4cdaba16",
                // vrc_AvatarV3ActionLayer
                [VRCAvatarDescriptor.AnimLayerType.Action] = "3e479eeb9db24704a828bffb15406520",
                // vrc_AvatarV3FaceLayer
                [VRCAvatarDescriptor.AnimLayerType.FX] = "d40be620cf6c698439a2f0a5144919fe",
                // vrc_AvatarV3SittingLayer
                [VRCAvatarDescriptor.AnimLayerType.Sitting] = "1268460c14f873240981bf15aa88b21a",
                // vrc_AvatarV3UtilityTPose
                [VRCAvatarDescriptor.AnimLayerType.TPose] = "00121b5812372b74f9012473856d8acf",
                // vrc_AvatarV3UtilityIKPose
                [VRCAvatarDescriptor.AnimLayerType.IKPose] = "a9b90a833b3486e4b82834c9d1f7c4ee"
            };
    }
}

#endif
