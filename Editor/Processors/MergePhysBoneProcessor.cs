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
                    throw new InvalidOperationException(CL4EE.Tr("MergePhysBone:error:makeParentWithChildren"));

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
                    throw new InvalidOperationException(CL4EE.Tr("MergePhysBone:error:parentDiffer"));

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

            protected override void NoSource() => throw new InvalidOperationException("No sources");

            protected override void UnsupportedPbVersion() =>
                throw new InvalidOperationException("Unsupported Pb Version");

            protected override void TransformSection()
            {
                var multiChildType = _sourcePhysBone.FindProperty(nameof(VRCPhysBoneBase.multiChildType));
                if (multiChildType.enumValueIndex != 0 || multiChildType.hasMultipleDifferentValues)
                    throw new InvalidOperationException("Some PysBone has multi child type != Ignore");
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
                PbPropImpl(label, overridePropName, overrides, () =>
                {
                    var sourceProp = _sourcePhysBone.FindProperty(pbPropName);
                    _mergedPhysBone.FindProperty(pbPropName).CopyDataFrom(sourceProp);
                    return sourceProp.hasMultipleDifferentValues;
                });
            }

            protected override void PbCurveProp(string label,
                string pbPropName,
                string pbCurvePropName,
                SerializedProperty overridePropName,
                params SerializedProperty[] overrides)
            {
                PbPropImpl(label, overridePropName, overrides, () =>
                {
                    var sourceValueProp = _sourcePhysBone.FindProperty(pbPropName);
                    _mergedPhysBone.FindProperty(pbPropName).CopyDataFrom(sourceValueProp);
                    var sourceCurveProp = _sourcePhysBone.FindProperty(pbCurvePropName);
                    _mergedPhysBone.FindProperty(pbCurvePropName).CopyDataFrom(sourceCurveProp);

                    return sourceValueProp.hasMultipleDifferentValues || sourceCurveProp.hasMultipleDifferentValues;
                });
            }

            protected override void PbPermissionProp(string label,
                string pbPropName,
                string pbFilterPropName,
                SerializedProperty overridePropName,
                params SerializedProperty[] overrides)
            {
                PbPropImpl(label, overridePropName, overrides, () =>
                {
                    var sourceValueProp = _sourcePhysBone.FindProperty(pbPropName);
                    _mergedPhysBone.FindProperty(pbPropName).CopyDataFrom(sourceValueProp);

                    if (sourceValueProp.enumValueIndex == 2)
                    {
                        var sourceFilterProp = _sourcePhysBone.FindProperty(pbFilterPropName);
                        var mergedFilterProp = _mergedPhysBone.FindProperty(pbFilterPropName);

                        var sourceAllowSelf = sourceFilterProp.FindPropertyRelative("allowSelf");
                        mergedFilterProp.FindPropertyRelative("allowSelf").CopyDataFrom(sourceAllowSelf);
                        var sourceAllowOthers = sourceFilterProp.FindPropertyRelative("allowOthers");
                        mergedFilterProp.FindPropertyRelative("allowOthers").CopyDataFrom(sourceAllowOthers);
                        
                        return sourceValueProp.hasMultipleDifferentValues || sourceFilterProp.hasMultipleDifferentValues;
                    }
                    else
                    {
                        return sourceValueProp.hasMultipleDifferentValues;
                    }
                });
            }

            protected override void Pb3DCurveProp(string label,
                string pbPropName,
                string pbXCurveLabel, string pbXCurvePropName,
                string pbYCurveLabel, string pbYCurvePropName,
                string pbZCurveLabel, string pbZCurvePropName,
                SerializedProperty overridePropName,
                params SerializedProperty[] overrides)
            {
                PbPropImpl(label, overridePropName, overrides, () =>
                {
                    var sourceValueProp = _sourcePhysBone.FindProperty(pbPropName);
                    var sourceXCurveProp = _sourcePhysBone.FindProperty(pbXCurvePropName);
                    var sourceYCurveProp = _sourcePhysBone.FindProperty(pbYCurvePropName);
                    var sourceZCurveProp = _sourcePhysBone.FindProperty(pbZCurvePropName);
                    _mergedPhysBone.FindProperty(pbPropName).CopyDataFrom(sourceValueProp);
                    _mergedPhysBone.FindProperty(pbXCurvePropName).CopyDataFrom(sourceXCurveProp);
                    _mergedPhysBone.FindProperty(pbYCurvePropName).CopyDataFrom(sourceYCurveProp);
                    _mergedPhysBone.FindProperty(pbZCurvePropName).CopyDataFrom(sourceZCurveProp);

                    return sourceValueProp.hasMultipleDifferentValues
                           || sourceXCurveProp.hasMultipleDifferentValues
                           || sourceYCurveProp.hasMultipleDifferentValues
                           || sourceZCurveProp.hasMultipleDifferentValues;
                });
            }

            private void PbPropImpl(string label,
                SerializedProperty overrideProp,
                SerializedProperty[] overrides,
                Func<bool> copy)
            {
                if (overrides.Any(x => x.boolValue) || overrideProp.boolValue) return;

                // Copy mode
                var differ = copy();

                if (differ)
                {
                    throw new InvalidOperationException(
                        $"The value of {label} is differ between two or more sources. " +
                        "You have to set same value OR override this property");
                }
            }

            protected override void ColliderProp(string label, string pbProp, SerializedProperty overrideProp)
            {
                // merged later
            }
        }
    }
}
