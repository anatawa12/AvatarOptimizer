#if AAO_VRCSDK3_AVATARS
using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;
using VRC.Dynamics;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class OptimizePhysBone : TraceAndOptimizePass<OptimizePhysBone>
    {
        public override string DisplayName => "T&O: Optimize PhysBone";

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            if (!state.OptimizePhysBone) return;

            if (!state.SkipIsAnimatedOptimization)
                IsAnimatedOptimization(context);
            
            if (!state.SkipMergePhysBoneCollider)
                MergePhysBoneColliders(context);
            
            if (!state.SkipReplaceEndBoneWithEndpointPosition)
                ConfigureReplaceEndBoneWithEndpointPosition(context, state);
        }

        private void MergePhysBoneColliders(BuildContext context)
        {
            var collidersByTransform = new Dictionary<(
                Transform rootTransformAnimated, 
                VRCPhysBoneColliderBase.ShapeType shapeType
                ), List<VRCPhysBoneColliderBase>>();

            foreach (var collider in context.GetComponents<VRCPhysBoneColliderBase>())
            {
                // if any of the property is animated, we do not merge the collider.
                if (PhysBoneColliderProperties.Any(context.GetAnimationComponent(collider).IsAnimatedFloat))
                    continue;

                var rootTransform = collider.GetRootTransform();
                var transform = rootTransform;
                while (transform != null && transform != context.AvatarRootTransform && !IsAnimated())
                    transform = transform.parent;
                if (transform == null) continue; // it's PhysBone about the bone itself

                bool IsAnimated()
                {
                    if (TransformProperties.Any(context.GetAnimationComponent(transform).IsAnimatedFloat)) return true;
                    if (context.GetAnimationComponent(transform.gameObject).IsAnimatedFloat(Props.IsActive)) return true;
                    return false;
                }

                var key = (transform, collider.shapeType);
                if (!collidersByTransform.TryGetValue(key, out var list))
                    collidersByTransform.Add(key, list = new List<VRCPhysBoneColliderBase>());

                list.Add(collider);
            }

            var mergedColliders = new Dictionary<VRCPhysBoneColliderBase, VRCPhysBoneColliderBase>();

            foreach (var ((_, shapeType), colliders) in collidersByTransform)
            {
                if (colliders.Count <= 1) continue;

                switch (shapeType)
                {
                    case VRCPhysBoneColliderBase.ShapeType.Sphere:
                        MergeColliders(colliders, mergedColliders, collider =>
                        {
                            var rootTransform = collider.GetRootTransform();
                            var scale = PhysBoneScale(rootTransform);
                            var radius = scale * collider.radius;
                            var centerPosition = rootTransform.TransformPoint(collider.position);
                            return (RoundError.Float(radius), RoundError.Vector3(centerPosition));
                        });
                        break;
                    case VRCPhysBoneColliderBase.ShapeType.Capsule:
                        MergeColliders(colliders, mergedColliders, collider =>
                        {
                            var rootTransform = collider.GetRootTransform();
                            var scale = PhysBoneScale(rootTransform);
                            var radius = scale * collider.radius;
                            var height = scale * collider.height;
                            var rotation = rootTransform.rotation * collider.rotation;
                            var centerPosition = rootTransform.TransformPoint(collider.position);

                            // rotation is hard to round floating point error so use position instead
                            var offset = rotation * Vector3.up * height / 2;
                            if (offset.x < 0) offset = -offset;
                            var headPosition = centerPosition + offset;
                            var tailPosition = centerPosition - offset;

                            return (RoundError.Float(radius), RoundError.Vector3(headPosition), RoundError.Vector3(tailPosition));
                        });
                        break;
                    case VRCPhysBoneColliderBase.ShapeType.Plane:
                        MergeColliders(colliders, mergedColliders, collider =>
                        {
                            var rootTransform = collider.GetRootTransform();
                            var rotation = rootTransform.rotation * collider.rotation;
                            // TODO: for planes, position don't have to be same, just y-position at origin
                            var centerPosition = rootTransform.TransformPoint(collider.position);
                            if (rotation == Quaternion.identity)
                            {
                                // currently this feature is intended for floor colliders so
                                // special optimization for floor colliders
                                centerPosition.x = 0;
                                centerPosition.z = 0;
                            }
                            return (RoundError.Quaternion(rotation), RoundError.Vector3(centerPosition));
                        });
                        break;
                    default:
                        BuildLog.LogWarning("TraceAndOptimize:OptimizePhysBone:UnknownPhysBoneColliderShape", shapeType.ToString(), colliders);
                        break;
                }
            }

            // nothing to be merged
            if (mergedColliders.Count == 0) return;

            foreach (var physBone in context.GetComponents<VRCPhysBoneBase>())
                for (var i = 0; i < physBone.colliders.Count; i++)
                    if (physBone.colliders[i])
                        if (mergedColliders.TryGetValue(physBone.colliders[i], out var mergedTo))
                            physBone.colliders[i] = mergedTo;

            foreach (var colliderBase in mergedColliders.Keys.ToList())
                DestroyTracker.DestroyImmediate(colliderBase);
        }
        
        void MergeColliders<TKey>(IEnumerable<VRCPhysBoneColliderBase> colliders,
            Dictionary<VRCPhysBoneColliderBase, VRCPhysBoneColliderBase> colliderMapping,
            Func<VRCPhysBoneColliderBase, TKey> colliderKey)
        {
            foreach (var grouping in colliders.GroupBy(colliderKey))
            {
                var asList = grouping.ToList();
                if (asList.Count == 1) continue;
                // Congratulation! We can merge those colliders!

                var mergeTo = asList[0];
                foreach (var mapped in asList.Skip(1))
                    colliderMapping.Add(mapped, mergeTo);
            }
        }

        private float PhysBoneScale(Transform transform)
        {
            var lossyScale = transform.lossyScale;
            return Mathf.Max(Mathf.Max(lossyScale.x, lossyScale.y), lossyScale.z);
        }

        private void IsAnimatedOptimization(BuildContext context)
        {
            foreach (var physBone in context.GetComponents<VRCPhysBoneBase>())
            {
                using (ErrorReport.WithContextObject(physBone))
                {
                    var isAnimated = physBone.GetAffectedTransforms()
                        .Select(transform => context.GetAnimationComponent(transform))
                        .Any(animation => IsAnimatedExternally(physBone, animation))
                        || ParentsScaleIsAnimated(context, physBone);
                    if (physBone.isAnimated && !isAnimated)
                    {
                        physBone.isAnimated = false;
                        Debug.Log($"Optimized IsAnimated for {physBone.name}", physBone);
                    }
                    // TODO: add warning if physBone.isAnimated is false and isAnimated is true?                    
                }
            }
        }

        // Due to bug (or possibly poor documentation) in VRChat PhysBone,
        // If the scale of the PhysBone component was zero on enable and then changed to non-zero,
        // the Angle value (and possibly other values) will be frozen to 0.5.
        // Therefore, we need to check if the scale is animated.
        // This previously broke one of avatar.
        // See thread: https://twitter.com/anatawa12_vrc/status/1918997361096786164
        // TODO: when upstream issue is fixed, remove this code.
        // TODO: Add link to the canny (upstream issue)
        public static bool ParentsScaleIsAnimated(BuildContext context, VRCPhysBoneBase physBone)
        {
            foreach (var parents in physBone.transform.ParentEnumerable(context.AvatarRootTransform))
            {
                var animation = context.GetAnimationComponent(parents);
                foreach (var transformProperty in ScaleProperties)
                {
                    var property = animation.GetFloatNode(transformProperty);
                    if (property.SourceComponents.Any(sourceComponent => sourceComponent != physBone)) return true;
                }
            }

            return false;
        }

        private static bool IsAnimatedExternally(VRCPhysBoneBase physBone, AnimationComponentInfo<PropertyInfo> animation)
        {
            foreach (var transformProperty in TransformProperties)
            {
                var property = animation.GetFloatNode(transformProperty);
                if (property.SourceComponents.Any(sourceComponent => sourceComponent != physBone)) return true;
            }

            return false;
        }

        private static readonly string[] ScaleProperties =
        {
            "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z",
            // Animator Window won't create the following properties, but generated by some scripts and works in runtime
            "localScale.x", "localScale.y", "localScale.z",
        };

        private static readonly string[] TransformProperties =
        {
            "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w",
            "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", 
            "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z", 
            // Animator Window won't create the following properties, but generated by some scripts and works in runtime
            "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w",
            "localPosition.x", "localPosition.y", "localPosition.z", 
            "localScale.x", "localScale.y", "localScale.z", 
            "localEulerAnglesRaw.x", "localEulerAnglesRaw.y", "localEulerAnglesRaw.z"
        };
        
        private static readonly string[] PhysBoneColliderProperties =
        {
            Props.EnabledFor(typeof(VRCPhysBoneColliderBase)),
            "shapeType",
            "insideBounds",
            "radius",
            "height",
            "position",
            "rotation",
            "bonesAsSpheres",
        };

        private void ConfigureReplaceEndBoneWithEndpointPosition(BuildContext context, TraceAndOptimizeState state)
        {
            var allPhysBones = context.GetComponents<VRCPhysBoneBase>();
            if (allPhysBones.Length == 0) return;
                
            var overlappedPhysBones = ReplaceEndBoneWithEndpointPositionProcessor.GetOverlappedPhysBones(allPhysBones);
            var validPhysBones = allPhysBones.Where(physbone => !overlappedPhysBones.Contains(physbone));

            var componentInfos = context.Extension<GCComponentInfoContext>();
            var entrypointMap = DependantMap.CreateEntrypointsMap(context);

            // ReplaceEndBoneWithEndpointPosition component affects all physbones attached to the gameobject it is attached to.
            var physbonesByGameObjects = validPhysBones
                .GroupBy(physbone => physbone.gameObject)
                .ToArray();

            foreach (var physbonesByGameObject in physbonesByGameObjects)
            {
                var gameObject = physbonesByGameObject.Key;
                var physbones = physbonesByGameObject;

                if (state.Exclusions.Contains(gameObject)) continue;
                if (gameObject.TryGetComponent<ReplaceEndBoneWithEndpointPosition>(out _)) continue;

                if (physbones.All(physbone => ShouldReplace(physbone, state, entrypointMap, componentInfos)))
                {
                    var component = gameObject.AddComponent<ReplaceEndBoneWithEndpointPosition>();
                    component.kind = ReplaceEndBoneWithEndpointPositionKind.Average;
                }
            }
        }

        private static bool ShouldReplace(VRCPhysBoneBase physbone, TraceAndOptimizeState state, DependantMap entrypointMap, GCComponentInfoContext componentInfos)
        {
            var leafBones = physbone.GetAffectedLeafBones().ToHashSet();
            var boneLengthChange = ReplaceEndBoneWithEndpointPositionProcessor.IsBoneLengthChange(physbone);

            if (leafBones.Count == 0) return false;
            if (!ValidatePhysBone(physbone, leafBones)) return false;

            if (!HasApproximatelyEqualLocalPosition(leafBones, out var localPosition)) return false;

            return leafBones.All(ValidateLeafBone);

            bool ValidatePhysBone(VRCPhysBoneBase physbone, HashSet<Transform> leafBones)
            {
                if (physbone.endpointPosition != Vector3.zero) return false; // alreday used
                if (!ReplaceEndBoneWithEndpointPositionProcessor.IsSafeMultiChild(physbone, leafBones)) return false;
                return true;
            }

            bool ValidateLeafBone(Transform leafBone)
            {
                if (state.Exclusions.Contains(leafBone.gameObject)) return false;
                if (leafBone.GetComponents<Component>().Length != 1) return false; // except transform

                // The endpoint position cannot replicate the "stretch" or "squish" behavior of the leaf bone, because with stretch or squish enabled, the transform's position of the leaf bone can actually move.
                // Therefore, replacement should not be performed when these options are enabled.
                // However, if the leaf bone does not have a valid usage, replacement can still be performed even if stretch or squish are enabled.
                if (boneLengthChange)
                {
                    var mergedUsages = entrypointMap.MergedUsages(componentInfos.GetInfo(leafBone));
                    const GCComponentInfo.DependencyType allowedUsages = GCComponentInfo.DependencyType.ComponentToTransform | GCComponentInfo.DependencyType.PhysBone;
                    var anyUsage = (mergedUsages &~ allowedUsages) != 0;
                    if (anyUsage) return false;
                }

                return true;
            }
        }

        private static bool HasApproximatelyEqualLocalPosition(IEnumerable<Transform> transforms, out Vector3 localPosition)
        {
            var localPositions = transforms.Select(x => x.localPosition);
            localPosition = ReplaceEndBoneWithEndpointPositionProcessor.GetAvaragePosition(localPositions);
            return ReplaceEndBoneWithEndpointPositionProcessor.AreApproximatelyEqualPosition(localPositions, localPosition);
        }
    }
}
#endif
