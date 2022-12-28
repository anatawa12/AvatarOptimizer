using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Anatawa12.Merger
{
    [AddComponentMenu("Anatawa12/Merge PhysBone")]
    [RequireComponent(typeof(VRCPhysBone))]
    [DisallowMultipleComponent]
    internal class MergePhysBone : MonoBehaviour
    {
        public VRCPhysBoneBase mergedComponent;

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

        [FormerlySerializedAs("component")] public VRCPhysBoneBase[] components;

        void OnEnable()
        {
            if (mergedComponent == null)
                mergedComponent = GetComponent<VRCPhysBoneBase>();
        }


        public HashSet<string> CollectDifferentProps()
        {
            var differ = new HashSet<string>();
            foreach (var (a, b) in components.ZipWithNext())
                FindDifferentPartsSamePhysBone(a, b, differ);
            return differ;
        }

        private static bool Eq<T>(T a, T b) => a.Equals(b);
        private static bool Eq(float a, float b) => Mathf.Abs(a - b) < 0.00001f;
        private static bool Eq(Vector3 a, Vector3 b) => (a - b).magnitude < 0.00001f;
        private static bool Eq(AnimationCurve a, AnimationCurve b) => a.Equals(b);
        private static bool Eq(float aFloat, AnimationCurve aCurve, float bFloat, AnimationCurve bCurve) => 
            Eq(aFloat, bFloat) && Eq(aCurve, bCurve);
        private static bool SetEq<T>(IEnumerable<T> a, IEnumerable<T> b) => 
            new HashSet<T>(a).SetEquals(b);

        private void FindDifferentPartsSamePhysBone(VRCPhysBoneBase a, VRCPhysBoneBase b, HashSet<string> differ)
        {
            // === Transforms ===
            // Root Transform: ignore: we'll merge them
            // Ignore Transforms: ignore: we'll merge them
            // Endpoint position: ignore: we'll replace with zero and insert end bone instead
            // Multi Child Type: ignore: Must be 'Ignore'
            // == Forces ==
            if (!forces)
            {
                if (!Eq(a.integrationType, b.integrationType)) differ.Add("Integration Type");
                if (!pull && !Eq(a.pull, a.pullCurve, b.pull, b.pullCurve))  differ.Add("Pull");
                if (!spring && !Eq(a.spring, a.springCurve, b.spring, b.springCurve))  differ.Add("Spring");
                if (!stiffness && !Eq(a.stiffness, a.stiffnessCurve, b.stiffness, b.stiffnessCurve))
                    differ.Add("Stiffness");
                if (!gravity && !Eq(a.gravity, a.gravityCurve, b.gravity, b.gravityCurve)) differ.Add("Gravity");
                if (!gravityFalloff && !Eq(a.gravityFalloff, a.gravityFalloffCurve, b.gravityFalloff,
                        b.gravityFalloffCurve)) differ.Add("Gravity FallOff");
                if (!immobile)
                {
                    if (!Eq(a.immobileType, b.immobileType)) differ.Add("Immobile Type");
                    if (!Eq(a.immobile, a.immobileCurve, b.immobile, b.immobileCurve)) differ.Add("Immobile");
                }
            }

            // == Limits ==
            if (!limits)
            {
                if (a.limitType != b.limitType)
                    differ.Add("Limit Type");
                switch (a.limitType)
                {
                    case VRCPhysBoneBase.LimitType.None:
                        break;
                    case VRCPhysBoneBase.LimitType.Angle:
                    case VRCPhysBoneBase.LimitType.Hinge:
                        if (!maxAngleX && !Eq(a.maxAngleX, a.maxAngleXCurve, b.maxAngleX, b.maxAngleXCurve))
                            differ.Add("Max Angle");
                        //if (!Eq(a.maxAngleZ, a.maxAngleZCurve, b.maxAngleZ, b.maxAngleZCurve)) return false;
                        if (!limitRotation)
                        {
                            if (!Eq(a.limitRotation, b.limitRotation)) differ.Add("Limit Rotation");
                            if (!Eq(a.limitRotationXCurve, b.limitRotationXCurve)) differ.Add("Limit Rotation Curve X");
                            if (!Eq(a.limitRotationYCurve, b.limitRotationYCurve)) differ.Add("Limit Rotation Curve Y");
                            if (!Eq(a.limitRotationZCurve, b.limitRotationZCurve)) differ.Add("Limit Rotation Curve Z");
                        }

                        break;
                    case VRCPhysBoneBase.LimitType.Polar:
                        if (!maxAngleX && !Eq(a.maxAngleX, a.maxAngleXCurve, b.maxAngleX, b.maxAngleXCurve))
                            differ.Add("Max Angle X");
                        if (!maxAngleZ && !Eq(a.maxAngleZ, a.maxAngleZCurve, b.maxAngleZ, b.maxAngleZCurve))
                            differ.Add("Max Angle Z");
                        if (!limitRotation)
                        {
                            if (!Eq(a.limitRotation, b.limitRotation)) differ.Add("Limit Rotation");
                            if (!Eq(a.limitRotationXCurve, b.limitRotationXCurve)) differ.Add("Limit Rotation Curve X");
                            if (!Eq(a.limitRotationYCurve, b.limitRotationYCurve)) differ.Add("Limit Rotation Curve Y");
                            if (!Eq(a.limitRotationZCurve, b.limitRotationZCurve)) differ.Add("Limit Rotation Curve Z");
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // == Collision ==
            if (!radius && !Eq(a.radius, a.radiusCurve, b.radius, b.radiusCurve)) differ.Add("Radius");
            if (!allowCollision && !Eq(a.allowCollision, b.allowCollision)) differ.Add("Allow Collision");
            if (colliders != CollidersSettings.Copy && !SetEq(a.colliders, b.colliders)) differ.Add("Colliders");
            // == Grab & Pose ==
            if (!allowGrabbing && !Eq(a.allowGrabbing, b.allowGrabbing)) differ.Add("Allow Grabbing");
            if (!allowPosing && !Eq(a.allowPosing, b.allowPosing)) differ.Add("Allow Posing");
            if (!grabMovement && !Eq(a.grabMovement, b.grabMovement)) differ.Add("Grab Movement");
            if (!maxStretch && !Eq(a.maxStretch, a.maxStretchCurve, b.maxStretch, b.maxStretchCurve)) differ.Add("Max Stretch");
            // == Options ==
            // Parameter: ignore: must be empty
            // Is Animated: ignore: we can merge them.
            // Gizmos: ignore: it should not affect actual behaviour
        }
    }

    public enum CollidersSettings
    {
        Copy,
        Merge,
        Override,
    }
}
