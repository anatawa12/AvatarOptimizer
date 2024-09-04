#if AAO_VRCSDK3_AVATARS

using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class MergePhysBoneProcessor : Pass<MergePhysBoneProcessor>
    {
        public override string DisplayName => "MergePhysBone";

        protected override void Execute(BuildContext context)
        {
            foreach (var mergePhysBone in context.GetComponents<MergePhysBone>())
            {
                using (ErrorReport.WithContextObject(mergePhysBone))
                {
                    DoMerge(mergePhysBone, context);
                    DestroyTracker.DestroyImmediate(mergePhysBone);
                }
            }
        }

        private static bool SetEq<T>(IEnumerable<T> a, IEnumerable<T> b) => 
            new HashSet<T>(a).SetEquals(b);

        internal static void DoMerge(MergePhysBone merge, BuildContext? context)
        {
            var sourceComponents = merge.componentsSet.GetAsList();
            if (sourceComponents.Count == 0) return;

            var pb = sourceComponents[0];

            // optimization: if All children of the parent is to be merged,
            //    reuse that parent GameObject instead of creating new one.
            Transform root;
            if (merge.makeParent)
            {
                root = merge.transform;

                if (root.childCount != 0)
                    return; // error reported by validator

                foreach (var physBone in sourceComponents)
                    physBone.GetTarget().parent = root;
            }
            else
            {
                var parentDiffer = sourceComponents
                    .Select(x => x.GetTarget().parent)
                    .ZipWithNext()
                    .Any(x => x.Item1 != x.Item2);
                
                if (parentDiffer)
                    return; // differ error reported by validator

                if (sourceComponents.Count == pb.GetTarget().parent.childCount)
                {
                    root = pb.GetTarget().parent;
                }
                else
                {
                    root = Utils.NewGameObject($"PhysBoneRoot-{Guid.NewGuid()}", pb.GetTarget().parent).transform;

                    foreach (var physBone in sourceComponents)
                        physBone.GetTarget().parent = root;
                }
            }

            // yaw / pitch fix
            if (merge.limitRotationConfig.@override == MergePhysBone.CurveVector3Config.CurveOverride.Fix)
                foreach (var physBone in sourceComponents)
                    FixYawPitch(physBone, context);

            // clear endpoint position
            if (merge.endpointPositionConfig.@override == MergePhysBone.EndPointPositionConfig.Override.Clear)
                foreach (var physBone in sourceComponents)
                    ClearEndpointPositionProcessor.Process(physBone);

            var merged = merge.gameObject.AddComponent<VRCPhysBone>();

            // copy common properties
            var mergedSerialized = new SerializedObject(merged);
            new MergePhysBoneMerger(new SerializedObject(merge), mergedSerialized).DoProcess();
            mergedSerialized.ApplyModifiedPropertiesWithoutUndo();

            // copy other properties
            // === Transforms ===
            merged.rootTransform = root;
            merged.ignoreTransforms = sourceComponents.SelectMany(x => x.ignoreTransforms).Distinct().ToList();

            switch (merge.endpointPositionConfig.@override)
            {
                case MergePhysBone.EndPointPositionConfig.Override.Clear:
                    merged.endpointPosition = Vector3.zero;
                    break;
                case MergePhysBone.EndPointPositionConfig.Override.Copy:
                    merged.endpointPosition = pb.endpointPosition;
                    break;
                case MergePhysBone.EndPointPositionConfig.Override.Override:
                    merged.endpointPosition = merge.endpointPositionConfig.value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            merged.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
            switch (merge.collidersConfig.@override)
            {
                case MergePhysBone.CollidersConfig.CollidersOverride.Copy:
                    merged.colliders = pb.colliders;
                    break;
                case MergePhysBone.CollidersConfig.CollidersOverride.Merge:
                    merged.colliders = sourceComponents.SelectMany(x => x.colliders).Distinct().ToList();
                    break;
                case MergePhysBone.CollidersConfig.CollidersOverride.Override:
                    merged.colliders = merge.collidersConfig.value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            // == Options ==
            merged.isAnimated = merge.isAnimatedConfig.value || sourceComponents.Any(x => x.isAnimated);

            // show the new PhysBone
            merged.hideFlags &= ~(HideFlags.HideInHierarchy | HideFlags.HideInInspector);

            foreach (var physBone in sourceComponents) DestroyTracker.DestroyImmediate(physBone);

            if (context != null)
            {
                // register modifications by merged component
                foreach (var transform in merged.GetAffectedTransforms())
                {
                    var component = context.GetAnimationComponent(transform);
                    foreach (var property in TransformRotationAndPositionAnimationKeys)
                    {
                        component.AddModification(property, new VariableComponentPropModNode(merged), ApplyState.Always);
                    }
                }
            }
        }

        // To preserve bone reference, we keep original bone and create new GameObject for it.
        // and later Trace and Object remove unused objects will merge original bones
        public static void FixYawPitch(VRCPhysBoneBase physBone, BuildContext? context)
        {
            // Already fixed; nothing to do!
            if (physBone.limitRotation.Equals(Vector3.zero)) return;

            
            physBone.InitTransforms(true);
            var maxChainLength = physBone.BoneChainLength();

            throw new NotImplementedException();
        }

        private static readonly string[] TransformRotationAndPositionAnimationKeys =
        {
            "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w", 
            "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z" ,
        };

        sealed class MergePhysBoneMerger : MergePhysBoneEditorModificationUtils
        {
            private SerializedObject _mergedPhysBone;
            private int _maxChainLength;

            public MergePhysBoneMerger(SerializedObject serializedObject, SerializedObject mergedPhysBone) : base(serializedObject)
            {
                _mergedPhysBone = mergedPhysBone;
            }

            protected override void BeginPbConfig()
            {
                foreach (var vrcPhysBoneBase in SourcePhysBones)
                    vrcPhysBoneBase.InitTransforms(true);

                _maxChainLength = SourcePhysBones.Max(x => x.BoneChainLength());
            }

            protected override bool BeginSection(string name, string docTag)
            {
                return true;
            }

            protected override void EndSection()
            {
            }

            protected override void EndPbConfig()
            {
            }

            protected override void NoSource()
            {
                // differ error reported by validator
            }

            protected override void UnsupportedPbVersion()
            {
                // differ error reported by validator
            }

            protected override void TransformSection()
            {
                // differ error reported by validator
                // merge of endpointPosition proceed later
            }

            protected override void OptionParameter()
            {
                // nothing to do
            }

            protected override void OptionIsAnimated()
            {
                // nothing to do: _isAnimatedProp is merged later
            }

            protected override void PbVersionProp(string label, ValueConfigProp prop, bool forceOverride = false)
                => PbProp(label, prop, forceOverride);

            protected override void PbProp(string label, ValueConfigProp prop, bool forceOverride = false)
            {
                var @override = forceOverride || prop.IsOverride;
                _mergedPhysBone.FindProperty(prop.PhysBoneValueName).CopyDataFrom(prop.GetValueProperty(@override));
            }

            protected override void PbCurveProp(string label, CurveConfigProp prop, bool forceOverride = false)
            {
                var @override = forceOverride || prop.IsOverride;
                _mergedPhysBone.FindProperty(prop.PhysBoneValueName).floatValue =
                    prop.GetValueProperty(@override).floatValue;
                if (@override)
                {
                    _mergedPhysBone.FindProperty(prop.PhysBoneCurveName).animationCurveValue =
                        prop.GetCurveProperty(@override).animationCurveValue;
                }
                else
                {
                    _mergedPhysBone.FindProperty(prop.PhysBoneCurveName).animationCurveValue =
                        FixCurve(prop.GetCurveProperty(@override).animationCurveValue);
                }
            }

            protected override void Pb3DCurveProp(string label, string pbXCurveLabel, string pbYCurveLabel, string pbZCurveLabel,
                CurveVector3ConfigProp prop, bool forceOverride = false)
            {
                switch (prop.GetOverride(forceOverride))
                {
                    case MergePhysBone.CurveVector3Config.CurveOverride.Copy:
                        _mergedPhysBone.FindProperty(prop.PhysBoneValueName).vector3Value =
                            prop.SourceValue!.vector3Value;
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveXName).animationCurveValue =
                            FixCurve(prop.SourceCurveX!.animationCurveValue);
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveYName).animationCurveValue =
                            FixCurve(prop.SourceCurveY!.animationCurveValue);
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveZName).animationCurveValue =
                            FixCurve(prop.SourceCurveZ!.animationCurveValue);
                        break;
                    case MergePhysBone.CurveVector3Config.CurveOverride.Override:
                        _mergedPhysBone.FindProperty(prop.PhysBoneValueName).vector3Value =
                            prop.OverrideValue.vector3Value;
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveXName).animationCurveValue =
                            prop.OverrideCurveX.animationCurveValue;
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveYName).animationCurveValue =
                            prop.OverrideCurveY.animationCurveValue;
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveZName).animationCurveValue =
                            prop.OverrideCurveZ.animationCurveValue;
                        break;
                    case MergePhysBone.CurveVector3Config.CurveOverride.Fix:
                        // Fixing rotation is proceeded before.
                        // We just reset the value and curve.
                        _mergedPhysBone.FindProperty(prop.PhysBoneValueName).vector3Value = Vector3.zero;
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveXName).animationCurveValue = new AnimationCurve();
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveYName).animationCurveValue = new AnimationCurve();
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveZName).animationCurveValue = new AnimationCurve();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            protected override void PbPermissionProp(string label, PermissionConfigProp prop, bool forceOverride = false)
            {
                var @override = forceOverride || prop.IsOverride;
                _mergedPhysBone.FindProperty(prop.PhysBoneValueName).intValue =
                    prop.GetValueProperty(@override).intValue;
                _mergedPhysBone.FindProperty(prop.PhysBoneFilterName)
                    .CopyDataFrom(prop.GetFilterProperty(@override));
            }

            protected override void CollidersProp(string label, CollidersConfigProp prop)
            {
                // merged later
            }
            
            private AnimationCurve FixCurve(AnimationCurve curve)
            {
                //return curve;
                var offset = 1f / (_maxChainLength + 1);
                var tangentRatio = (_maxChainLength + 1f) / _maxChainLength;
                var keys = curve.keys;
                foreach (ref var curveKey in keys.AsSpan())
                {
                    curveKey.time = Mathf.LerpUnclamped(offset, 1, curveKey.time);
                    curveKey.inTangent *= tangentRatio;
                    curveKey.outTangent *= tangentRatio;
                }
                curve.keys = keys;
                return curve;
            }
        }
    }
}

#endif
