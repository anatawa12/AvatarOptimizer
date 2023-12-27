#if AAO_VRCSDK3_AVATARS

using System;
using System.Collections.Generic;
using System.Linq;
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
                    DoMerge(mergePhysBone);
                    Object.DestroyImmediate(mergePhysBone);
                }
            }
        }

        private static bool SetEq<T>(IEnumerable<T> a, IEnumerable<T> b) => 
            new HashSet<T>(a).SetEquals(b);

        internal static void DoMerge(MergePhysBone merge)
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
                    .Select(x => x.transform.parent)
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

            foreach (var physBone in sourceComponents) Object.DestroyImmediate(physBone);
        }

        sealed class MergePhysBoneMerger : MergePhysBoneEditorModificationUtils
        {
            private SerializedObject _mergedPhysBone;
            public MergePhysBoneMerger(SerializedObject serializedObject, SerializedObject mergedPhysBone) : base(serializedObject)
            {
                _mergedPhysBone = mergedPhysBone;
            }

            protected override void BeginPbConfig()
            {
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
                => PbPropImpl(label, prop, forceOverride);

            protected override void PbCurveProp(string label, CurveConfigProp prop, bool forceOverride = false)
                => PbPropImpl(label, prop, forceOverride);

            protected override void Pb3DCurveProp(string label, string pbXCurveLabel, string pbYCurveLabel, string pbZCurveLabel,
                CurveVector3ConfigProp prop, bool forceOverride = false)
                => PbPropImpl(label, prop, forceOverride);

            protected override void PbPermissionProp(string label, PermissionConfigProp prop, bool forceOverride = false)
                => PbPropImpl(label, prop, forceOverride);

            private void PbPropImpl(string label, OverridePropBase prop, bool forceOverride)
            {
                var @override = forceOverride || prop.IsOverride;
                foreach (var (pbName, property) in prop.GetActiveProps(@override))
                    _mergedPhysBone.FindProperty(pbName).CopyDataFrom(property);
            }

            protected override void CollidersProp(string label, CollidersConfigProp prop)
            {
                // merged later
            }
        }
    }
}

#endif