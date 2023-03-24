using System;
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
        [FormerlySerializedAs("mergedComponent")] public VRCPhysBoneBase merged;

        public Transform rootTransform;

        // == Forces ==
        [FormerlySerializedAs("force")] public bool forces;
        public bool pull;
        public bool spring;
        public bool stiffness;
        public bool gravity;
        public bool gravityFalloff;
        public bool immobile;
        // == Limits ==
        public bool limits;
        public bool maxAngleX;
        public bool maxAngleZ;
        public bool limitRotation;
        // == Collision ==
        public bool radius;
        public bool allowCollision;
        public CollidersSettings colliders;
        // == Grab & Pose ==
        public bool allowGrabbing;
        public bool grabMovement;
        public bool allowPosing;
        public bool maxStretch;
        // == Others ==
        // public bool overrideParameter; Always
        public bool isAnimated;

        [Obsolete("legacy v1", true)]
        [FormerlySerializedAs("component")] public VRCPhysBoneBase[] components;
        public PrefabSafeSet.VRCPhysBoneBaseSet componentsSet;

        public MergePhysBone()
        {
            componentsSet = new PrefabSafeSet.VRCPhysBoneBaseSet(this);
        }

        void OnEnable()
        {
            if (merged == null)
                merged = GetComponent<VRCPhysBoneBase>();
        }
    }

    public enum CollidersSettings
    {
        Copy,
        Merge,
        Override,
    }
}
