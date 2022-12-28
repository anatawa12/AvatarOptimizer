using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Anatawa12.Merger
{
    [AddComponentMenu("Anatawa12/Merge PhysBone")]
    [RequireComponent(typeof(VRCPhysBone))]
    [DisallowMultipleComponent]
    internal class MergePhysBone : MonoBehaviour
    {
        public VRCPhysBone mergedComponent;

        // == Forces ==
        public bool integrationType;
        public bool pull;
        public bool spring;
        public bool stiffness;
        public bool gravity;
        public bool gravityFalloff;
        public bool immobile;
        // == Limits ==
        public bool limitType;
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

        [FormerlySerializedAs("component")] public VRCPhysBone[] components;

        void OnEnable()
        {
            if (mergedComponent == null)
                mergedComponent = GetComponent<VRCPhysBone>();
        }
    }

    public enum CollidersSettings
    {
        Copy,
        Merge,
        Override,
    }
}
