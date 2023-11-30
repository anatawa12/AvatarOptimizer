#if AAO_VRCSDK3_AVATARS
using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsers;
using Anatawa12.AvatarOptimizer.ErrorReporting;
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
                if (PhysBoneColliderProperties.Any(context.GetAnimationComponent(collider).ContainsFloat))
                    continue;

                var rootTransform = collider.GetRootTransform();
                var transform = rootTransform;
                while (transform != null && transform != context.AvatarRootTransform && !IsAnimated())
                    transform = transform.parent;
                if (transform == null) continue; // it's PhysBone about the bone itself

                bool IsAnimated()
                {
                    if (TransformProperties.Any(context.GetAnimationComponent(transform).ContainsFloat)) return true;
                    if (context.GetAnimationComponent(transform.gameObject).ContainsFloat("m_IsActive")) return true;
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
                            return (radius, centerPosition);
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
                            return (radius, height, rotation, centerPosition);
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
                            return (rotation, centerPosition);
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
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
                Object.DestroyImmediate(colliderBase);
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
            BuildReport.ReportingObjects(context.GetComponents<VRCPhysBoneBase>(), physBone =>
            {
                var isAnimated = physBone.GetAffectedTransforms()
                    .Select(transform => context.GetAnimationComponent(transform))
                    .Any(animation => IsAnimatedExternally(physBone, animation));
                if (physBone.isAnimated && !isAnimated)
                {
                    physBone.isAnimated = false;
                    Debug.Log($"Optimized IsAnimated for {physBone.name}", physBone);
                }
                // TODO: add warning if physBone.isAnimated is false and isAnimated is true?
            });
        }

        private static bool IsAnimatedExternally(VRCPhysBoneBase physBone, AnimationComponentInfo animation)
        {
            foreach (var transformProperty in TransformProperties)
            {
                if (!animation.TryGetFloat(transformProperty, out var property)) continue;
                foreach (var modificationSource in property.Sources)
                    if (!(modificationSource is ComponentAnimationSource componentSource) ||
                        componentSource.Component != physBone)
                        return true;
            }

            return false;
        }

        private static readonly string[] TransformProperties =
        {
            "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w",
            "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", 
            "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z", 
            "localEulerAnglesRaw.x", "localEulerAnglesRaw.y", "localEulerAnglesRaw.z"
        };
        
        private static readonly string[] PhysBoneColliderProperties =
        {
            "m_Enabled",
            "shapeType",
            "insideBounds",
            "radius",
            "height",
            "position",
            "rotation",
            "bonesAsSpheres",
        };
    }
}
#endif
