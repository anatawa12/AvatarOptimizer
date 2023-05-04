using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
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
            BuildReport.ReportingObjects(session.GetComponents<MergePhysBone>(),
                mergePhysBone => DoMerge(mergePhysBone, session));
        }

        private static bool SetEq<T>(IEnumerable<T> a, IEnumerable<T> b) => 
            new HashSet<T>(a).SetEquals(b);

        internal static void DoMerge(MergePhysBone merge, OptimizerSession session)
        {
            var sourceComponents = merge.componentsSet.GetAsList();
            if (sourceComponents.Count == 0) return;

            var pb = sourceComponents[0];
            var merged = merge.Merged;

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
                    root = Utils.NewGameObject("PhysBoneRoot", pb.GetTarget().parent).transform;

                    foreach (var physBone in sourceComponents)
                        physBone.GetTarget().parent = root;
                }
            }

            // clear endpoint position
            foreach (var physBone in sourceComponents)
                ClearEndpointPositionProcessor.Process(physBone);

            // copy common properties
            new MergePhysBoneMerger(new SerializedObject(merge)).DoProcess();

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

            // show the new PhysBone
            merged.hideFlags &= ~(HideFlags.HideInHierarchy | HideFlags.HideInInspector);

            foreach (var physBone in sourceComponents) session.Destroy(physBone);
            session.Destroy(merge);
        }

        sealed class MergePhysBoneMerger : MergePhysBoneEditorModificationUtils
        {
            public MergePhysBoneMerger(SerializedObject serializedObject) : base(serializedObject)
            {
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
            }

            protected override void OptionParameter()
            {
                // nothing to do
            }

            protected override void OptionIsAnimated()
            {
                // nothing to do: _isAnimatedProp is merged later
            }

            protected override void PbVersionProp(string label, string pbPropName, SerializedProperty overridePropName,
                params SerializedProperty[] overrides) =>
                PbProp(label, pbPropName, overridePropName, overrides);

            protected override void PbProp(string label,
                string pbPropName,
                SerializedProperty overridePropName,
                params SerializedProperty[] overrides)
            {
                PbPropImpl(label, overridePropName, overrides, pbPropName);
            }

            protected override void PbCurveProp(string label,
                string pbPropName,
                string pbCurvePropName,
                SerializedProperty overridePropName,
                params SerializedProperty[] overrides)
            {
                PbPropImpl(label, overridePropName, overrides, pbPropName, pbCurvePropName);
            }

            protected override void PbPermissionProp(string label,
                string pbPropName,
                string pbFilterPropName,
                SerializedProperty overridePropName,
                params SerializedProperty[] overrides)
            {
                PbPropImpl(label, overridePropName, overrides, pbPropName, pbFilterPropName);
            }

            protected override void Pb3DCurveProp(string label,
                string pbPropName,
                string pbXCurveLabel, string pbXCurvePropName,
                string pbYCurveLabel, string pbYCurvePropName,
                string pbZCurveLabel, string pbZCurvePropName,
                SerializedProperty overridePropName,
                params SerializedProperty[] overrides)
            {
                PbPropImpl(label, overridePropName, overrides, pbPropName,
                    pbXCurvePropName, pbYCurvePropName, pbZCurvePropName);
            }

            private void PbPropImpl(string label,
                SerializedProperty overrideProp,
                SerializedProperty[] overrides,
                params string[] props)
            {
                if (overrides.Any(x => x.boolValue) || overrideProp.boolValue) return;

                // Copy mode
                // differ error reported by validator
                foreach (var prop in props)
                    _mergedPhysBone.FindProperty(prop).CopyDataFrom(_sourcePhysBone.FindProperty(prop));
            }

            protected override void ColliderProp(string label, string pbProp, SerializedProperty overrideProp)
            {
                // merged later
            }
        }
    }
}
