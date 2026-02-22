#if AAO_VRCSDK3_AVATARS
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Profiling;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;

class AutoMergeCompatiblePhysBone: TraceAndOptimizePass<AutoMergeCompatiblePhysBone>
{
    public override string DisplayName => "T&O: Auto Merge Compatible PhysBone";
    protected override bool Enabled(TraceAndOptimizeState state) => state.MergePhysBones;

    protected override void Execute(BuildContext context, TraceAndOptimizeState state)
    {
        var physBonesByKey = new Dictionary<PbInfo, List<VRCPhysBone>>();
        foreach (var physBone in context.GetComponents<VRCPhysBone>()
                     .GroupBy(x => x.GetTarget())
                     .Where(x => x.Count() == 1) // if multiple PhysBones share the same target, they cannot be merged; skip them
                     .Where(x => x.Key.IsChildOf(context.AvatarRootTransform))
                     .Select(x => x.First())
                 )
        {
            // Animated physbones cannot be merged
            if (context.GetAnimationComponent(physBone).GetAllFloatProperties().Any(x => !x.node.IsEmpty))
                continue;
            
            // find toggle root GameObject
            var toggleRoot = FindToggleRoot(context, physBone.gameObject);

            // If the physBone is toggled by toggling the GameObject, it cannot be merged.
            if (toggleRoot == physBone.gameObject)
                continue;

            var key = new PbInfo(physBone, toggleRoot);

            // Merge is prohibited due to settings
            if (!key.CanMergePhysBone())
                continue;

            // VRCConstraints and VRCPhysBones placement effects execution order, so
            // we cannot merge PhysBones that affect Transforms with Constraints.
            if (physBone.GetAffectedTransforms()
                .Any(t => (Component)t.GetComponent<IConstraint>() || t.GetComponent<VRCConstraintBase>()))
                continue;

            // large physBones should not be merged.
            // VRCSDK recommends keeping affected bones under 128.
            if (physBone.GetAffectedTransforms().Count() >= 100)
                continue;

            if (!physBonesByKey.TryGetValue(key, out var list))
                physBonesByKey.Add(key, list = new List<VRCPhysBone>());

            list.Add(physBone);
        }

        var index = 0;
        foreach (var (pbInfo, list) in physBonesByKey)
        {
            if (list.Count < 2) continue;

            var parent = pbInfo.ToggleRoot ?? context.AvatarRootObject;

            foreach (var groups in Utils.Partition(list.Select(pb => (pb.GetAffectedTransforms().Count(), pb)), 128))
            {
                if (groups.Count == 1) continue; // no need to merge if only one physbone in the group
                var mergePBGameObject = new GameObject($"$$$$$AutoMergedPhysBone_{index++}$$$$");
                mergePBGameObject.transform.SetParent(parent.transform, worldPositionStays: false);
                var mergePB = mergePBGameObject.AddComponent<MergePhysBone>();
                mergePB.endpointPositionConfig.@override = MergePhysBone.EndPointPositionConfigStruct.Override.Copy;
                mergePB.componentsSet.AddRange(groups.Select(g => g.data));
                context.Extension<GCComponentInfoContext>().NewComponent(mergePBGameObject.transform);
                context.Extension<GCComponentInfoContext>().NewComponent(mergePB);
            }
        }
    }

    private static GameObject? FindToggleRoot(BuildContext context, GameObject go)
    {
        foreach (var transform in go.transform.ParentEnumerable(root: context.AvatarRootTransform, includeMe: true))
        {
            if (context.GetAnimationComponent(transform.gameObject).IsAnimatedFloat(Props.IsActive))
                return transform.gameObject;
        }

        return null;
    }

    readonly struct PbInfo : IEquatable<PbInfo>
    {
        public readonly bool IsActiveAndEnabled;

        public readonly VRCPhysBoneBase.Version Version;

        // transform
        public readonly Transform RootTransformParent;
        public readonly bool IgnoreOtherPhysBones;
        public readonly Vector3 EndpointPosition;
        public readonly VRCPhysBoneBase.MultiChildType MultiChildType;

        // force section
        public readonly VRCPhysBoneBase.IntegrationType IntegrationType;
        public readonly float Pull = 0.2f;
        public readonly AnimationCurve? PullCurve;
        public readonly float Spring = 0.2f;
        public readonly AnimationCurve? SpringCurve;
        public readonly float Stiffness = 0.2f;
        public readonly AnimationCurve? StiffnessCurve;
        public readonly float Gravity;
        public readonly AnimationCurve? GravityCurve;
        public readonly float GravityFalloff;
        public readonly AnimationCurve? GravityFalloffCurve;
        public readonly VRCPhysBoneBase.ImmobileType ImmobileType;
        public readonly float Immobile;
        public readonly AnimationCurve? ImmobileCurve;
        // limits
        public readonly VRCPhysBoneBase.LimitType LimitType;
        public readonly float MaxAngleX;
        public readonly AnimationCurve? MaxAngleXCurve;
        public readonly float MaxAngleZ;
        public readonly AnimationCurve? MaxAngleZCurve;
        public readonly Vector3 LimitRotation;
        public readonly AnimationCurve? LimitRotationXCurve;
        public readonly AnimationCurve? LimitRotationYCurve;
        public readonly AnimationCurve? LimitRotationZCurve;

        // collision
        public readonly float Radius;
        public readonly AnimationCurve? RadiusCurve;
        public readonly bool AllowCollisionSelf;
        public readonly bool AllowCollisionOthers;
        public readonly ImmutableHashSet<VRCPhysBoneColliderBase> Colliders;

        // stretch and squish
        public readonly float StretchMotion;
        public readonly AnimationCurve? StretchMotionCurve;
        public readonly float MaxStretch;
        public readonly AnimationCurve? MaxStretchCurve;
        public readonly float MaxSquish;
        public readonly AnimationCurve? MaxSquishCurve;

        // grab and posing
        public readonly bool AllowGrabbingSelf;
        public readonly bool AllowGrabbingOthers;
        public readonly bool AllowPosingSelf;
        public readonly bool AllowPosingOthers;
        public readonly bool SnapToHand;
        public readonly float GrabMovement;

        // options
        public readonly bool IsAnimated;
        public readonly bool ResetWhenDisabled;
        public readonly string Parameter;

        // others
        public readonly int ChainLength;
        public readonly GameObject? ToggleRoot;

        public PbInfo(VRCPhysBone physBone, GameObject? toggleRoot)
        {
            IsActiveAndEnabled = physBone.isActiveAndEnabled;
            Version = physBone.version;
            ToggleRoot = toggleRoot;
            // transform
            var rootTransform = physBone.rootTransform;
            if (rootTransform == null) rootTransform = physBone.transform;
            RootTransformParent = rootTransform.parent;
#if AAO_VRCSDK3_AVATARS_IGNORE_OTHER_PHYSBONE
            IgnoreOtherPhysBones = physBone.ignoreOtherPhysBones;
#else
            IgnoreOtherPhysBones = false;
#endif
            EndpointPosition = physBone.endpointPosition;
            MultiChildType = physBone.multiChildType;
            // force
            IntegrationType = physBone.integrationType;
            Pull = physBone.pull;
            PullCurve = NormalizeCurve(physBone.pullCurve, Pull);
            Spring = physBone.spring;
            SpringCurve = NormalizeCurve(physBone.springCurve, Spring);
            Stiffness = physBone.stiffness;
            StiffnessCurve = NormalizeCurve(physBone.stiffnessCurve, Stiffness);
            Gravity = physBone.gravity;
            GravityCurve = NormalizeCurve(physBone.gravityCurve, Gravity);
            if (Gravity != 0)
            {
                GravityFalloff = physBone.gravityFalloff;
                GravityFalloffCurve = NormalizeCurve(physBone.gravityFalloffCurve, GravityFalloff);
            }
            else
            {
                GravityFalloff = 0;
                GravityFalloffCurve = null;
            }
            ImmobileType = physBone.immobileType;
            Immobile = physBone.immobile;
            ImmobileCurve = NormalizeCurve(physBone.immobileCurve, Immobile);
            // limits
            LimitType = physBone.limitType;
            MaxAngleX = physBone.maxAngleX;
            MaxAngleXCurve = NormalizeCurve(physBone.maxAngleXCurve, MaxAngleX);
            MaxAngleZ = physBone.maxAngleZ;
            MaxAngleZCurve = NormalizeCurve(physBone.maxAngleZCurve, MaxAngleZ);
            LimitRotation = physBone.limitRotation;
            LimitRotationXCurve = NormalizeCurve(physBone.limitRotationXCurve, LimitRotation.x);
            LimitRotationYCurve = NormalizeCurve(physBone.limitRotationYCurve, LimitRotation.y);
            LimitRotationZCurve = NormalizeCurve(physBone.limitRotationZCurve, LimitRotation.z);
            // collision
            Radius = physBone.radius;
            RadiusCurve = NormalizeCurve(physBone.radiusCurve, Radius);
            (AllowCollisionSelf, AllowCollisionOthers) = PermissionFilter(physBone.allowCollision, physBone.collisionFilter);
            Colliders = physBone.colliders.ToImmutableHashSet();
            // stretch and squish
            StretchMotion = physBone.stretchMotion;
            StretchMotionCurve = NormalizeCurve(physBone.stretchMotionCurve, StretchMotion);
            MaxStretch = physBone.maxStretch;
            MaxStretchCurve = NormalizeCurve(physBone.maxStretchCurve, MaxStretch);
            MaxSquish = physBone.maxSquish;
            MaxSquishCurve = NormalizeCurve(physBone.maxSquishCurve, MaxSquish);
            // grab and posing
            (AllowGrabbingSelf, AllowGrabbingOthers) = PermissionFilter(physBone.allowGrabbing, physBone.grabFilter);
            (AllowPosingSelf, AllowPosingOthers) = PermissionFilter(physBone.allowPosing, physBone.poseFilter);
            SnapToHand = physBone.snapToHand;
            GrabMovement = physBone.grabMovement;
            // options
            IsAnimated = physBone.isAnimated;
            ResetWhenDisabled = physBone.resetWhenDisabled;
            Parameter = physBone.parameter ?? "";

            // any curve is not null, we need to consider chain length
            if (PullCurve != null || SpringCurve != null || StiffnessCurve != null ||
                GravityCurve != null || GravityFalloffCurve != null ||
                ImmobileCurve != null ||
                MaxAngleXCurve != null || MaxAngleZCurve != null ||
                LimitRotationXCurve != null || LimitRotationYCurve != null || LimitRotationZCurve != null ||
                RadiusCurve != null ||
                StretchMotionCurve != null || MaxStretchCurve != null || MaxSquishCurve != null)
            {
                ChainLength = ComputeChainLength(rootTransform, physBone.ignoreTransforms, EndpointPosition);
            }
            else
            {
                ChainLength = -1;
            }
        }

        private static AnimationCurve? NormalizeCurve(AnimationCurve? curve, float value)
        {
            // If the value is zerom, the curve is ignored.
            if (value == 0f) return null;
            if (curve == null || curve.length == 0) return null;
            return curve;
        }

        static (bool self, bool others) PermissionFilter(VRCPhysBoneBase.AdvancedBool allow,
            VRCPhysBoneBase.PermissionFilter filter) => allow switch
        {
            VRCPhysBoneBase.AdvancedBool.False => (false, false),
            VRCPhysBoneBase.AdvancedBool.True => (true, true),
            VRCPhysBoneBase.AdvancedBool.Other => (filter.allowSelf, filter.allowOthers),
            _ => throw new ArgumentOutOfRangeException(nameof(allow), allow, null)
        };

        private static int ComputeChainLength(Transform rootTransform, List<Transform> physBoneIgnoreTransforms,
            Vector3 endpointPosition)
        {
            var transfrom = CountChildTransformNestCount(rootTransform);
            if (endpointPosition != Vector3.zero) transfrom += 1;
            return transfrom;
            int CountChildTransformNestCount(Transform transform)
            {
                int maxChildCount = 0;
                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (physBoneIgnoreTransforms.Contains(child)) continue;
                    var childCount = CountChildTransformNestCount(child);
                    if (childCount > maxChildCount)
                        maxChildCount = childCount;
                }
                return maxChildCount + 1;
            }
        }

        public bool CanMergePhysBone() => this is
        {
            IsActiveAndEnabled: true, // disabled physbone should not be merged; will be removed by optimizer later
            MultiChildType: VRCPhysBoneBase.MultiChildType.Ignore,
            AllowGrabbingOthers: false,
            AllowGrabbingSelf: false,
            Parameter: "",
        };

        public bool Equals(PbInfo other)
        {
            return IsActiveAndEnabled == other.IsActiveAndEnabled && 
                   Version == other.Version && 
                   ToggleRoot == other.ToggleRoot &&
                   RootTransformParent.Equals(other.RootTransformParent) &&
                   IgnoreOtherPhysBones == other.IgnoreOtherPhysBones &&
                   EndpointPosition.Equals(other.EndpointPosition) && 
                   MultiChildType == other.MultiChildType &&
                   IntegrationType == other.IntegrationType && 
                   Pull.Equals(other.Pull) &&
                   Equals(PullCurve, other.PullCurve) && 
                   Spring.Equals(other.Spring) &&
                   Equals(SpringCurve, other.SpringCurve) && 
                   Stiffness.Equals(other.Stiffness) &&
                   Equals(StiffnessCurve, other.StiffnessCurve) && 
                   Gravity.Equals(other.Gravity) &&
                   Equals(GravityCurve, other.GravityCurve) && 
                   GravityFalloff.Equals(other.GravityFalloff) &&
                   Equals(GravityFalloffCurve, other.GravityFalloffCurve) && 
                   ImmobileType == other.ImmobileType &&
                   Immobile.Equals(other.Immobile) && 
                   Equals(ImmobileCurve, other.ImmobileCurve) &&
                   LimitType == other.LimitType && 
                   MaxAngleX.Equals(other.MaxAngleX) &&
                   Equals(MaxAngleXCurve, other.MaxAngleXCurve) && 
                   MaxAngleZ.Equals(other.MaxAngleZ) &&
                   Equals(MaxAngleZCurve, other.MaxAngleZCurve) && 
                   LimitRotation.Equals(other.LimitRotation) &&
                   Equals(LimitRotationXCurve, other.LimitRotationXCurve) &&
                   Equals(LimitRotationYCurve, other.LimitRotationYCurve) &&
                   Equals(LimitRotationZCurve, other.LimitRotationZCurve) && 
                   Radius.Equals(other.Radius) &&
                   Equals(RadiusCurve, other.RadiusCurve) && 
                   AllowCollisionSelf == other.AllowCollisionSelf &&
                   AllowCollisionOthers == other.AllowCollisionOthers && 
                   Colliders.SetEquals(other.Colliders) &&
                   StretchMotion.Equals(other.StretchMotion) && 
                   Equals(StretchMotionCurve, other.StretchMotionCurve) &&
                   MaxStretch.Equals(other.MaxStretch) && 
                   Equals(MaxStretchCurve, other.MaxStretchCurve) &&
                   MaxSquish.Equals(other.MaxSquish) && 
                   Equals(MaxSquishCurve, other.MaxSquishCurve) &&
                   AllowGrabbingSelf == other.AllowGrabbingSelf && 
                   AllowGrabbingOthers == other.AllowGrabbingOthers &&
                   AllowPosingSelf == other.AllowPosingSelf && 
                   AllowPosingOthers == other.AllowPosingOthers &&
                   SnapToHand == other.SnapToHand && 
                   GrabMovement.Equals(other.GrabMovement) &&
                   IsAnimated == other.IsAnimated && 
                   ResetWhenDisabled == other.ResetWhenDisabled &&
                   Parameter == other.Parameter && 
                   ChainLength == other.ChainLength;
        }

        public override bool Equals(object? obj) => obj is PbInfo other && Equals(other);

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(IsActiveAndEnabled);
            hashCode.Add((int)Version);
            hashCode.Add(ToggleRoot);
            hashCode.Add(RootTransformParent);
            hashCode.Add(IgnoreOtherPhysBones);
            hashCode.Add(EndpointPosition);
            hashCode.Add((int)MultiChildType);
            hashCode.Add((int)IntegrationType);
            hashCode.Add(Pull);
            hashCode.Add(PullCurve);
            hashCode.Add(Spring);
            hashCode.Add(SpringCurve);
            hashCode.Add(Stiffness);
            hashCode.Add(StiffnessCurve);
            hashCode.Add(Gravity);
            hashCode.Add(GravityCurve);
            hashCode.Add(GravityFalloff);
            hashCode.Add(GravityFalloffCurve);
            hashCode.Add((int)ImmobileType);
            hashCode.Add(Immobile);
            hashCode.Add(ImmobileCurve);
            hashCode.Add((int)LimitType);
            hashCode.Add(MaxAngleX);
            hashCode.Add(MaxAngleXCurve);
            hashCode.Add(MaxAngleZ);
            hashCode.Add(MaxAngleZCurve);
            hashCode.Add(LimitRotation);
            hashCode.Add(LimitRotationXCurve);
            hashCode.Add(LimitRotationYCurve);
            hashCode.Add(LimitRotationZCurve);
            hashCode.Add(Radius);
            hashCode.Add(RadiusCurve);
            hashCode.Add(AllowCollisionSelf);
            hashCode.Add(AllowCollisionOthers);
            hashCode.Add(Colliders.GetSetHashCode());
            hashCode.Add(StretchMotion);
            hashCode.Add(StretchMotionCurve);
            hashCode.Add(MaxStretch);
            hashCode.Add(MaxStretchCurve);
            hashCode.Add(MaxSquish);
            hashCode.Add(MaxSquishCurve);
            hashCode.Add(AllowGrabbingSelf);
            hashCode.Add(AllowGrabbingOthers);
            hashCode.Add(AllowPosingSelf);
            hashCode.Add(AllowPosingOthers);
            hashCode.Add(SnapToHand);
            hashCode.Add(GrabMovement);
            hashCode.Add(IsAnimated);
            hashCode.Add(ResetWhenDisabled);
            hashCode.Add(Parameter);
            hashCode.Add(ChainLength);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(PbInfo left, PbInfo right) => left.Equals(right);
        public static bool operator !=(PbInfo left, PbInfo right) => !left.Equals(right);
    }
}

#endif
