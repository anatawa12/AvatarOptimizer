using System;
using System.Collections.Generic;
using System.Linq;
using CustomLocalization4EditorExtension;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class MergePhysBoneProcessor
    {
        public void Process(OptimizerSession session)
        {
            foreach (var mergePhysBone in session.GetComponents<MergePhysBone>())
            {
                DoMerge(mergePhysBone, session);
            }
        }

        private static bool SetEq<T>(IEnumerable<T> a, IEnumerable<T> b) => 
            new HashSet<T>(a).SetEquals(b);

        internal static HashSet<string> CollectDifferentProps(MergePhysBone merge, IEnumerable<VRCPhysBoneBase> sources)
        {
            var differ = new HashSet<string>();
            var physBones = sources as VRCPhysBoneBase[] ?? sources.ToArray();
            // ReSharper disable once CoVariantArrayConversion
            var serializedObject = new SerializedObject(physBones);
            {
                // process common properties
                {
                    ProcessProperties(merge, physBones[0].limitType, property =>
                    {
                        if (serializedObject.FindProperty(property).hasMultipleDifferentValues)
                            differ.Add(property);
                    }, (advancedBool, otherValue) =>
                    {
                        var advancedBoolProp = serializedObject.FindProperty(advancedBool);
                        if (advancedBoolProp.hasMultipleDifferentValues)
                            differ.Add(advancedBool);
                        if (advancedBoolProp.enumValueIndex != 2) return;
                        if (serializedObject.FindProperty(otherValue).hasMultipleDifferentValues)
                            differ.Add(otherValue);
                    });
                }
            }

            foreach (var (a, b) in physBones.ZipWithNext())
            {
                // other props
                if (!merge.makeParent && a.GetTarget().parent != b.GetTarget().parent)
                    differ.Add("Parent of target Transform");
                if (merge.colliders == CollidersSettings.Copy &&
                    !SetEq(a.colliders, b.colliders)) differ.Add("colliders");
            }
            return differ;
        }

        internal static void DoMerge(MergePhysBone merge, OptimizerSession session)
        {
            var sourceComponents = merge.componentsSet.GetAsList();
            if (sourceComponents.Count == 0) return;

            var differProps = CollectDifferentProps(merge, sourceComponents);
            if (differProps.Count != 0)
                throw new InvalidOperationException(
                    "MergePhysBone requirements is not met: " +
                    $"property differ: {string.Join(", ", differProps)}");

            var pb = sourceComponents[0];
            var merged = merge.merged;

            // optimization: if All children of the parent is to be merged,
            //    reuse that parent GameObject instead of creating new one.
            Transform root;
            if (merge.makeParent)
            {
                root = merge.transform;

                if (root.childCount != 0)
                    throw new InvalidOperationException(CL4EE.Tr("MergePhysBone:error:makeParentWithChildren"));

                foreach (var physBone in sourceComponents)
                        physBone.GetTarget().parent = root;
            }
            else if (sourceComponents.Count == pb.GetTarget().parent.childCount)
            {
                root = pb.GetTarget().parent;
            } else
            {
                root = Utils.NewGameObject("PhysBoneRoot", pb.GetTarget().parent).transform;

                foreach (var physBone in sourceComponents)
                    physBone.GetTarget().parent = root;
            }

            // clear endpoint position
            foreach (var physBone in sourceComponents)
                ClearEndpointPositionProcessor.Process(physBone);

            // copy common properties
            {
                var mergedSerialized = new SerializedObject(merged);
                var pbSerialized = new SerializedObject(pb);

                ProcessProperties(merge, pb.limitType,
                    property =>
                    {
                        mergedSerialized.FindProperty(property).CopyDataFrom(pbSerialized.FindProperty(property));
                    }, (advancedBool, otherValue) =>
                    {
                        mergedSerialized.FindProperty(advancedBool).CopyDataFrom(pbSerialized.FindProperty(advancedBool));
                        if (mergedSerialized.FindProperty(advancedBool).enumValueIndex != 2) return;
                        mergedSerialized.FindProperty(otherValue).CopyDataFrom(pbSerialized.FindProperty(otherValue));
                    });
                mergedSerialized.ApplyModifiedProperties();
            }

            // copy other properties
            // === Transforms ===
            merged.rootTransform = root;
            merged.ignoreTransforms = sourceComponents.SelectMany(x => x.ignoreTransforms).Distinct().ToList();
            merged.endpointPosition = Vector3.zero;
            merged.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
            switch (merge.colliders)
            {
                case CollidersSettings.Copy:
                    merged.colliders = pb.colliders;
                    break;
                case CollidersSettings.Merge:
                    merged.colliders = sourceComponents.SelectMany(x => x.colliders).Distinct().ToList();
                    break;
                case CollidersSettings.Override:
                    // keep merged.colliders
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            // == Options ==
            merged.isAnimated = merge.isAnimated || sourceComponents.Any(x => x.isAnimated);

            foreach (var physBone in sourceComponents) session.Destroy(physBone);
            session.Destroy(merge);
        }

        internal delegate void PropertyCallback(string prop);
        internal delegate void AdvancedPropertyCallback(string advancedBoolCallback, string otherPropCallback);

        internal static void ProcessProperties(MergePhysBone merge, VRCPhysBoneBase.LimitType limitType, PropertyCallback callback, AdvancedPropertyCallback advancedCallback)
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
            void Advanced(string arg1, string arg2)
            {
                advancedCallback(arg1, arg2);
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
            if (!merge.allowCollision) Advanced("allowCollision", "collisionFilter");
            // colliders: There's no common part
            // == Grab & Pose ==
            if (!merge.allowGrabbing) Advanced("allowGrabbing", "grabFilter");
            if (!merge.allowPosing) Advanced("allowPosing", "poseFilter");
            if (!merge.snapToHand) Callback1("snapToHand");
            if (!merge.grabMovement) Callback1("grabMovement");
            if (!merge.maxStretch) Callback2("maxStretch", "maxStretchCurve");
            // == Options ==
            // Parameter: ignore: must be empty
            // Is Animated: ignore: we can merge them.
            if (!merge.resetWhenDisabled) Callback1("resetWhenDisabled");
            // Gizmos: ignore: it should not affect actual behaviour
        }
    }
}
