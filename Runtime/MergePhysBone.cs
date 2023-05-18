using System;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using CustomLocalization4EditorExtension;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Merge PhysBone")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    internal class MergePhysBone : AvatarTagComponent, IStaticValidated
    {
        public VRCPhysBoneBase Merged => merged;

        [FormerlySerializedAs("mergedComponent")]
        [SerializeField]
        private VRCPhysBoneBase merged;

        [Obsolete("v2 legacy", true)]
        public Transform rootTransform;

        [CL4EELocalized("MergePhysBone:prop:makeParent", "MergePhysBone:tooltip:makeParent")]
        public bool makeParent;

        public bool version;
        // == Forces ==
        [FormerlySerializedAs("force")]
        [FormerlySerializedAs("forces")]
        public bool integrationType;
        public bool pull;
        public bool spring;
        public bool stiffness;
        public bool gravity;
        public bool gravityFalloff;
        public bool immobileType;
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
        public bool stretchMotion;
        public bool maxStretch;
        public bool maxSquish;
        public bool snapToHand;
        // == Others ==
        // public bool overrideParameter; Always
        public bool isAnimated;
        public bool resetWhenDisabled;

        [Obsolete("legacy v1", true)] [FormerlySerializedAs("component")]
        public VRCPhysBoneBase[] components;
        [CL4EELocalized("MergePhysBone:prop:components")]
        public PrefabSafeSet.VRCPhysBoneBaseSet componentsSet;

        public MergePhysBone()
        {
            componentsSet = new PrefabSafeSet.VRCPhysBoneBaseSet(this);
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (merged == null)
            {
                merged = gameObject.AddComponent<VRCPhysBone>();
                UnityEditor.EditorUtility.SetDirty(this);
            }
            merged.hideFlags |= HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        }

        void OnValidate()
        {
            if (merged.gameObject != gameObject)
            {
                merged = null;
                UnityEditor.EditorUtility.SetDirty(this);
            }
            if (merged == null)
            {
                merged = gameObject.AddComponent<VRCPhysBone>();
                UnityEditor.EditorUtility.SetDirty(this);
            }
            if (merged != null)
                merged.hideFlags |= HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        }
#endif
    }

    public enum CollidersSettings
    {
        Copy,
        Merge,
        Override,
    }
}
