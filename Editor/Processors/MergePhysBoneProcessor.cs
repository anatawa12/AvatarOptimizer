using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using Object = UnityEngine.Object;

namespace Anatawa12.Merger.Processors
{
    internal class MergePhysBoneProcessor
    {
        public void Process(MergerSession session)
        {
            foreach (var mergePhysBone in session.GetComponents<MergePhysBone>())
            {
                DoMerge(mergePhysBone, session);
            }
        }

        private static bool SetEq<T>(IEnumerable<T> a, IEnumerable<T> b) => 
            new HashSet<T>(a).SetEquals(b);

        internal static HashSet<string> CollectDifferentProps(MergePhysBone merge)
        {
            var differ = new HashSet<string>();
            foreach (var (a, b) in merge.components.ZipWithNext())
            {
                // process common properties
                {
                    var aSerialized = new SerializedObject(a);
                    var bSerialized = new SerializedObject(b);

                    ProcessProperties(merge, a.limitType, property =>
                    {
                        if (!SerializedProperty.DataEquals(aSerialized.FindProperty(property),
                                bSerialized.FindProperty(property)))
                            differ.Add(property);
                    });
                }

                // other props
                if (!merge.rootTransform && a.GetTarget().parent != b.GetTarget().parent)
                    differ.Add("Parent of target Transform");
                if (merge.colliders != CollidersSettings.Copy && !SetEq(a.colliders, b.colliders)) differ.Add("colliders");
            }
            return differ;
        }

        internal static void DoMerge(MergePhysBone merge, MergerSession session)
        {
            if (merge.components.Length == 0) return;

            var differProps = CollectDifferentProps(merge);
            if (CollectDifferentProps(merge).Count != 0)
                throw new InvalidOperationException(
                    "MergePhysBone requirements is not met: " +
                    $"property differ: {string.Join(", ", differProps)}");

            var pb = merge.components[0];
            var merged = merge.merged;

            var root = merge.rootTransform
                ? merge.rootTransform
                : Utils.NewGameObject("PhysBoneRoot", pb.GetTarget().parent).transform;
            
            var additionalIgnoreTransforms = new List<Transform>();

            if (merge.rootTransform)
            {
                // for components with rootTransform, add _dummy objects and add more ignoreTransforms.

                if (merge.components.Any(physBone => !physBone.GetTarget().IsChildOf(merge.rootTransform)))
                    throw new InvalidOperationException("MergePhysBone requirements is not met: rootTransform");

                var transforms = new HashSet<Transform>(
                    from component in merge.components
                    from parent in component.GetTarget().ParentEnumerable().TakeWhile(x => x != root)
                    select parent);

                var physBoneTransforms = new HashSet<Transform>(merge.components.Select(Utils.GetTarget));

                merge.rootTransform.WalkChildren(child =>
                {
                    // it's PhysBone transform: do nothing 
                    if (physBoneTransforms.Contains(child))
                        return false;

                    if (!transforms.Contains(child))
                    {
                        // it's not on path for PhysBone: ignore transform
                        additionalIgnoreTransforms.Add(child);
                        return false;
                    }

                    // it's on path to PhysBone transform: make it ignored with multiChildType = Ignore.
                    var childPhysBones = child.DirectChildrenEnumerable().Count(x => !transforms.Contains(x));

                    if (childPhysBones == 1)
                        physBoneTransforms.Add(Utils.NewGameObject("_dummy", child).transform);

                    return true;
                });
            }
            else
            {
                foreach (var physBone in merge.components)
                    physBone.GetTarget().parent = root;
            }

            // clear endpoint position
            foreach (var physBone in merge.components)
                if (physBone.endpointPosition != Vector3.zero)
                    WalkChildrenAndSetEndpoint(physBone.GetTarget(), physBone);

            // copy common properties
            {
                var mergedSerialized = new SerializedObject(merged);
                var pbSerialized = new SerializedObject(pb);

                ProcessProperties(merge, pb.limitType,
                    property =>
                    {
                        mergedSerialized.FindProperty(property).CopyDataFrom(pbSerialized.FindProperty(property));
                    });
                mergedSerialized.ApplyModifiedProperties();
            }

            // copy other properties
            // === Transforms ===
            merged.rootTransform = root;
            merged.ignoreTransforms = merge.components.SelectMany(x => x.ignoreTransforms)
                .Concat(additionalIgnoreTransforms).Distinct().ToList();
            merged.endpointPosition = Vector3.zero;
            merged.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
            switch (merge.colliders)
            {
                case CollidersSettings.Copy:
                    merged.colliders = pb.colliders;
                    break;
                case CollidersSettings.Merge:
                    merged.colliders = merge.components.SelectMany(x => x.colliders).Distinct().ToList();
                    break;
                case CollidersSettings.Override:
                    // keep merged.colliders
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            // == Options ==
            merged.isAnimated = merge.isAnimated || merge.components.Any(x => x.isAnimated);

            foreach (var physBone in merge.components) session.Destroy(physBone);
            session.Destroy(merge);
        }

        internal delegate void PropertyCallback(string prop);

        internal static void ProcessProperties(MergePhysBone merge, VRCPhysBoneBase.LimitType limitType, PropertyCallback callback)
        {
            void Callback1(string arg1)
            {
                callback(arg1);
            }
            void Callback2(string arg1, string arg2)
            {
                callback(arg1);
                callback(arg2);
            }

            // == Forces ==
            if (!merge.forces)
            {
                Callback1("integrationType");
                if (!merge.pull) Callback2("pull", "pullCurve");
                if (!merge.spring) Callback2("spring", "springCurve");
                if (!merge.stiffness) Callback2("stiffness", "stiffnessCurve");
                if (!merge.gravity) Callback2("gravity", "gravityCurve");
                if (!merge.gravityFalloff) Callback2("gravityFalloff", "gravityFalloffCurve");
                if (!merge.immobile)
                {
                    Callback1("immobileType");
                    Callback2("immobile", "immobileCurve");
                }
            }

            // == Limits ==
            if (!merge.limits)
            {
                Callback1("limitType");
                switch (limitType)
                {
                    case VRCPhysBoneBase.LimitType.None:
                        break;
                    case VRCPhysBoneBase.LimitType.Angle:
                    case VRCPhysBoneBase.LimitType.Hinge:
                        if (!merge.maxAngleX) Callback2("maxAngleX", "maxAngleXCurve");
                        if (!merge.limitRotation)
                        {
                            Callback1("limitRotation");
                            Callback1("limitRotationXCurve");
                            Callback1("limitRotationYCurve");
                            Callback1("limitRotationZCurve");
                        }

                        break;
                    case VRCPhysBoneBase.LimitType.Polar:
                        if (!merge.maxAngleX) Callback2("maxAngleX", "maxAngleXCurve");
                        if (!merge.maxAngleZ) Callback2("maxAngleZ", "maxAngleZCurve");
                        if (!merge.limitRotation)
                        {
                            Callback1("limitRotation");
                            Callback1("limitRotationXCurve");
                            Callback1("limitRotationYCurve");
                            Callback1("limitRotationZCurve");
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // == Collision ==
            if (!merge.radius) Callback2("radius", "radiusCurve");
            if (!merge.allowCollision) Callback1("allowCollision");
            // colliders: There's no common part
            // == Grab & Pose ==
            if (!merge.allowGrabbing) Callback1("allowGrabbing");
            if (!merge.allowPosing) Callback1("allowPosing");
            if (!merge.grabMovement) Callback1("grabMovement");
            if (!merge.maxStretch) Callback2("maxStretch", "maxStretchCurve");
            // == Options ==
            // Parameter: ignore: must be empty
            // Is Animated: ignore: we can merge them.
            // Gizmos: ignore: it should not affect actual behaviour
        }

        internal static void WalkChildrenAndSetEndpoint(Transform target, VRCPhysBoneBase physBone)
        {
            if (physBone.ignoreTransforms.Contains(target))
                return;
            if (target.childCount == 0)
            {
                var go = new GameObject($"_EndPhysBone");
                go.transform.parent = target;
                go.transform.localPosition = physBone.endpointPosition;
                return;
            }
            for (var i = 0; i < target.childCount; i++)
                WalkChildrenAndSetEndpoint(target.GetChild(i), physBone);
        }
    }
}
