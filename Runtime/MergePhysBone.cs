using System;
using CustomLocalization4EditorExtension;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Merge PhysBone")]
    [RequireComponent(typeof(VRCPhysBone))]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    internal class MergePhysBone : AvatarTagComponent
    {
        [Obsolete("v2 legacy", true)]
        public Transform rootTransform;

        [CL4EELocalized("MergePhysBone:prop:makeParent", "MergePhysBone:tooltip:makeParent")]
        public bool makeParent;

        // == Forces ==
        [FormerlySerializedAs("force")] [CL4EELocalized("MergePhysBone:prop:forces")]
        public bool forces;
        [CL4EELocalized("MergePhysBone:prop:pull")]
        public bool pull;
        [CL4EELocalized("MergePhysBone:prop:spring")]
        public bool spring;
        [CL4EELocalized("MergePhysBone:prop:stiffness")]
        public bool stiffness;
        [CL4EELocalized("MergePhysBone:prop:gravity")]
        public bool gravity;
        [CL4EELocalized("MergePhysBone:prop:gravityFalloff")]
        public bool gravityFalloff;
        [CL4EELocalized("MergePhysBone:prop:immobileType")]
        public bool immobileType;
        [CL4EELocalized("MergePhysBone:prop:immobile")]
        public bool immobile;
        // == Limits ==
        [CL4EELocalized("MergePhysBone:prop:limits")]
        public bool limits;
        [CL4EELocalized("MergePhysBone:prop:maxAngleX")]
        public bool maxAngleX;
        [CL4EELocalized("MergePhysBone:prop:maxAngleZ")]
        public bool maxAngleZ;
        [CL4EELocalized("MergePhysBone:prop:limitRotation")]
        public bool limitRotation;
        // == Collision ==
        [CL4EELocalized("MergePhysBone:prop:radius")]
        public bool radius;
        [CL4EELocalized("MergePhysBone:prop:allowCollision")]
        public bool allowCollision;
        [CL4EELocalized("MergePhysBone:prop:colliders")]
        public CollidersSettings colliders;
        // == Grab & Pose ==
        [CL4EELocalized("MergePhysBone:prop:allowGrabbing")]
        public bool allowGrabbing;
        [CL4EELocalized("MergePhysBone:prop:grabMovement")]
        public bool grabMovement;
        [CL4EELocalized("MergePhysBone:prop:allowPosing")]
        public bool allowPosing;
        [CL4EELocalized("MergePhysBone:prop:maxStretch")]
        public bool maxStretch;
        [CL4EELocalized("MergePhysBone:prop:snapToHand")]
        public bool snapToHand;
        // == Others ==
        // public bool overrideParameter; Always
        [CL4EELocalized("MergePhysBone:prop:isAnimated")]
        public bool isAnimated;
        [CL4EELocalized("MergePhysBone:prop:resetWhenDisabled")]
        public bool resetWhenDisabled;

        [Obsolete("legacy v1", true)] [FormerlySerializedAs("component")]
        public VRCPhysBoneBase[] components;
        [CL4EELocalized("MergePhysBone:prop:components")]
        public PrefabSafeSet.VRCPhysBoneBaseSet componentsSet;

        public MergePhysBone()
        {
            componentsSet = new PrefabSafeSet.VRCPhysBoneBaseSet(this);
        }
    }

    public enum CollidersSettings
    {
        Copy,
        Merge,
        Override,
    }
}
