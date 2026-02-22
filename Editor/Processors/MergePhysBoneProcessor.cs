#if AAO_VRCSDK3_AVATARS

using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using nadena.dev.ndmf;
using Unity.Mathematics;
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
                {
                    var oldParent = physBone.GetTarget().parent;
                    physBone.GetTarget().parent = root;
                    context?.Extension<GCComponentInfoContext>()?.ReplaceParent(physBone.GetTarget(), root, oldParent);
                }
            }
            else
            {
                var parentDiffer = sourceComponents
                    .Select(x => x.GetTarget().parent)
                    .ZipWithNext()
                    .Any(x => x.Item1 != x.Item2);
                
                if (parentDiffer)
                    return; // differ error reported by validator

                if (context != null && !pb.GetTarget().IsChildOf(context.AvatarRootTransform))
                {
                    BuildLog.LogError("MergePhysBone:error:physbone-outside-of-avatar-root", pb, merge, pb.GetTarget());
                    return;
                }

                if (sourceComponents.Count == pb.GetTarget().parent.childCount)
                {
                    root = pb.GetTarget().parent;
                }
                else
                {
                    root = Utils.NewGameObject($"PhysBoneRoot-{Guid.NewGuid()}", pb.GetTarget().parent).transform;
                    context?.Extension<GCComponentInfoContext>()?.NewComponent(root.transform);

                    foreach (var physBone in sourceComponents)
                    {
                        var oldParent = physBone.GetTarget().parent;
                        physBone.GetTarget().parent = root;
                        context?.Extension<GCComponentInfoContext>()?.ReplaceParent(physBone.GetTarget(), root, oldParent);
                    }
                }
            }

            // clear endpoint position
            if (merge.endpointPositionConfig.@override == MergePhysBone.EndPointPositionConfigStruct.Override.Clear)
                foreach (var physBone in sourceComponents)
                    ClearEndpointPositionProcessor.Process(physBone, context);

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
                case MergePhysBone.EndPointPositionConfigStruct.Override.Clear:
                    merged.endpointPosition = Vector3.zero;
                    break;
                case MergePhysBone.EndPointPositionConfigStruct.Override.Copy:
                    merged.endpointPosition = pb.endpointPosition;
                    break;
                case MergePhysBone.EndPointPositionConfigStruct.Override.Override:
                    merged.endpointPosition = merge.endpointPositionConfig.value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(merge), merge.endpointPositionConfig.@override, $"Invalid override mode: {merge.collidersConfig.@override}");
            }

            merged.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
            switch (merge.collidersConfig.@override)
            {
                case MergePhysBone.CollidersConfigStruct.Override.Copy:
                    merged.colliders = pb.colliders;
                    break;
                case MergePhysBone.CollidersConfigStruct.Override.Merge:
                    merged.colliders = sourceComponents.SelectMany(x => x.colliders).Distinct().ToList();
                    break;
                case MergePhysBone.CollidersConfigStruct.Override.Override:
                    merged.colliders = merge.collidersConfig.value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(merge), merge.collidersConfig.@override, $"Invalid override mode: {merge.collidersConfig.@override}");
            }
            // == Limits ==
            
            // yaw / pitch fix
            if (merge.limitRotationConfig.@override == MergePhysBone.LimitRotationConfigStruct.Override.Fix)
            {
                if (context != null)
                {
                    var animations = new HashSet<ObjectReference>();
                    foreach (var physBone in sourceComponents)
                    foreach (var affectedTransform in physBone.GetAffectedTransforms())
                    {
                        var component = context.GetAnimationComponent(affectedTransform);
                        foreach (var property in TransformRotationAndPositionAnimationKeys)
                        {
                            var node = component.GetFloatNode(property);
                            animations.UnionWith(node.ComponentNodes.OfType<AnimatorPropModNode<FloatValueInfo>>()
                                .SelectMany(x => x.ContextReferences));
                        }
                    }

                    if (animations.Count != 0)
                    {
                        BuildLog.LogWarning("MergePhysBone:warning:limit-rotation-fix-animation", animations);
                    }
                }

                var originalBones = new List<Transform>();
                // fix rotations
                foreach (var physBone in sourceComponents)
                    FixYawPitch(physBone, root, context, originalBones);

                if (originalBones.Any(x => x.TryGetComponent<MergeBone>(out _)))
                {
                    BuildLog.LogError("MergePhysBone:error:limit-rotation-fix-merge-bone");
                }

                // fix configurations
                merged.ignoreTransforms = merged.ignoreTransforms.Concat(originalBones).ToList();

                var sourceComponent = sourceComponents[0];
                var chainLength = sourceComponent.BoneChainLength();
                var yaws = new float[chainLength];
                float fixedRollOfLastBone = 0;
                var pitches = new float[chainLength];

                for (var i = 0; i < chainLength; i++)
                {
                    var rotationSpecified = sourceComponent.CalcLimitRotation((float)i / (chainLength - 1));
                    var rotation = ConvertRotation(rotationSpecified);
                    pitches[i] = rotation.x;
                    fixedRollOfLastBone = rotation.y;
                    yaws[i] = rotation.z;
                }

                var maxPitch = pitches.Select(Mathf.Abs).Max();
                var maxYaw = yaws.Select(Mathf.Abs).Max();

                merged.limitRotation = new Vector3(maxPitch, 0, maxYaw);

                if (maxPitch != 0 || maxYaw != 0)
                {
                    // avoid NaN
                    if (maxPitch == 0) maxPitch = 1;
                    if (maxYaw == 0) maxYaw = 1;

                    var pitchCurve = new AnimationCurve();
                    var yawCurve = new AnimationCurve();

                    pitchCurve.AddKey(0, pitches[0] / maxPitch);
                    yawCurve.AddKey(0, yaws[0] / maxYaw);

                    for (var i = 0; i < chainLength; i++)
                    {
                        var time = (float)(i + 1) / chainLength;
                        pitchCurve.AddKey(time, pitches[i] / maxPitch);
                        yawCurve.AddKey(time, yaws[i] / maxYaw);
                    }

                    merged.limitRotationXCurve = pitchCurve;
                    merged.limitRotationZCurve = yawCurve;
                }

                if (merged.endpointPosition != Vector3.zero)
                {
                    // TODO: this Endpoint Fix might not enough
                    // Rotation fix will conflict with this fix
                    merged.endpointPosition = Quaternion.Euler(0, -fixedRollOfLastBone, 0) * merged.endpointPosition;
                }
            }

            // == Options ==
            merged.isAnimated = merge.isAnimatedConfig.value || sourceComponents.Any(x => x.isAnimated);

            // show the new PhysBone
            merged.hideFlags &= ~(HideFlags.HideInHierarchy | HideFlags.HideInInspector);

            foreach (var physBone in sourceComponents) DestroyTracker.DestroyImmediate(physBone);

            if (context != null)
            {
                var gcContext = context.Extension<GCComponentInfoContext>();
                APIInternal.VRCSDK.VRCPhysBoneInformation.AddDependencyInformation(gcContext.NewComponent(merged), merged, gcContext);

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
        public static void FixYawPitch(
            VRCPhysBoneBase physBone,
            Transform root, 
            BuildContext? context,
            List<Transform> originalBones)
        {
            // Already fixed; nothing to do!
            if (physBone.limitRotation.Equals(Vector3.zero)) return;

            physBone.InitTransforms(true);
            var maxChainLength = physBone.BoneChainLength();

            var ignoreTransforms = new HashSet<Transform>(physBone.ignoreTransforms);

            RotateRecursive(physBone, physBone.GetTarget(), root, maxChainLength, 0, context, ignoreTransforms, originalBones);
        }

        /*
         RotateRecursive will transform
        Parent <= Parent
         `- Root <= Transform
             +- Bone1
             |   +- Bone2
             |       +- Bone3
             `- Bone4
                 +- Bone5
         into
        Parent
         `- Root (AAO Merge Proxy)
             +- Root
             +- Bone1 (AAO Merge Proxy)
             |   +- Bone1
             |   `- Bone2 (AAO Merge Proxy)
             |       +- Bone2
             |       `- Bone3 (AAO Merge Proxy)
             |           +- Bone3
             `- Bone4 (AAO Merge Proxy)
                 +- Bone4
                 `- Bone5 (AAO Merge Proxy)
                     +- Bone5

          One pass of this method will transform into

         Parent
           `- Root (AAO Merge Proxy) <= New Parent
               `- Root
                   +- Bone1 <= New Transform
                   |   +- Bone2
                   |       +- Bone3
                   `- Bone4 <= New Transform
                       +- Bone5
         and calls RotateRecursive with new set of bones to complete

         */

        private static void RotateRecursive(VRCPhysBoneBase physBone,
            Transform transform,
            Transform parent,
            int totalDepth,
            int depth,
            BuildContext? context,
            HashSet<Transform> ignoreTransforms,
            List<Transform> originalBones)
        {
            Vector3 targetLocation;

            var activeChildren = Enumerable.Range(0, transform.childCount)
                .Select(transform.GetChild)
                .Where(child => !ignoreTransforms.Contains(child))
                .ToArray();

            switch (activeChildren.Length)
            {
                case 0:
                    // end bone
                    if (physBone.endpointPosition != Vector3.zero)
                        targetLocation = physBone.endpointPosition;
                    else
                        targetLocation = Vector3.up;
                    break;
                case 1:
                    targetLocation = activeChildren[0].localPosition;
                    break;
                default:
                    switch (physBone.multiChildType)
                    {
                        case VRCPhysBoneBase.MultiChildType.Ignore:
                            targetLocation = Vector3.up;
                            break;
                        case VRCPhysBoneBase.MultiChildType.First:
                            targetLocation = activeChildren[0].localPosition;
                            break;
                        case VRCPhysBoneBase.MultiChildType.Average:
                            targetLocation =
                                activeChildren.Aggregate(Vector3.zero, (current, child) => current + child.localPosition) /
                                activeChildren.Length;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(physBone), physBone.multiChildType, $"Invalid multi child type: {physBone.multiChildType}");
                    }

                    break;
            }

            var specifiedRotation = physBone.CalcLimitRotation((float)depth / totalDepth);
            var rotation = ConvertRotation(specifiedRotation).y;

            // if the bone is at (0, -x, 0), we have infinite rotation for `FromToRotation` and
            // `Quaternion.FromToRotation`'s choice is not happy for logic below.
            // We need special handling for this case.
            var dot = Vector3.Dot(Vector3.up, math.normalizesafe(targetLocation));
            var critical = dot <= -1;

            //Debug.Log($"is critical: {critical}, dot: {dot}, transform: {transform.name}");
            var thisRotation = !critical ? rotation : -rotation;

            // create new (actual) bone
            var newBone = new GameObject($"{transform.name} (AAO Merge Proxy)");

            // new bone should be at exactly same transform as the original bone
            newBone.transform.parent = transform;
            newBone.transform.localPosition = Vector3.zero;
            newBone.transform.localRotation = Quaternion.identity;
            newBone.transform.localScale = Vector3.one;

            // move to parent
            newBone.transform.SetParent(parent, true);

            // rotate newBone to fix roll
            newBone.transform.Rotate(Vector3.up, thisRotation, Space.Self);

            // move old bone to child of newBone
            transform.SetParent(newBone.transform, true);

            context?.Extension<GCComponentInfoContext>()?.NewComponent(newBone.transform);
            originalBones.Add(transform);

            //var rotationQuaternion = Quaternion.Euler(0, -thisRotation, 0);

            foreach (var child in activeChildren)
            {
                //child.localPosition = rotationQuaternion * child.localPosition;
                //child.localRotation = rotationQuaternion * child.localRotation;
                
                if (ignoreTransforms.Contains(child)) continue;
                RotateRecursive(physBone, child, newBone.transform, totalDepth, depth + 1, context, ignoreTransforms, originalBones);
            }
        }

        public static Vector3 ConvertRotation(Vector3 limitRotation)
        {
            // XYZ is the order used in VRCPhysBone
            var quat = quaternion.EulerXYZ(limitRotation * Mathf.Deg2Rad);
            return QuaternionToEulerXZY(quat) * Mathf.Rad2Deg;
        }

        private static Vector3 QuaternionToEulerXZY(Quaternion q)
        {
            // Quaternion to Euler
            // https://qiita.com/aa_debdeb/items/abe90a9bd0b4809813da
            // YZX Order in the article. (XZY in Unity)
            // We use different perspective to represent same order of Euler order between Unity and the article.
            var sz = 2 * q.x * q.y + 2 * q.z * q.w;
            var unlocked = Mathf.Abs(sz) < 0.99999f;
            Debug.Log("unlocked: " + unlocked);
            return new Vector3(
                unlocked ? Mathf.Atan2(-(2 * q.y * q.z - 2 * q.x * q.w), 2 * q.w * q.w + 2 * q.y * q.y - 1) : 0,
                unlocked
                    ? Mathf.Atan2(-(2 * q.x * q.z - 2 * q.y * q.w), 2 * q.w * q.w + 2 * q.x * q.x - 1)
                    : Mathf.Atan2(2 * q.x * q.z + 2 * q.y * q.w, 2 * q.w * q.w + 2 * q.z * q.z - 1),
                Mathf.Asin(sz)
            );
        }

        private static readonly string[] TransformRotationAndPositionAnimationKeys =
        {
            "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w", 
            "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z" ,
            // Animator Window won't create the following properties, but generated by some scripts and works in runtime
            "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w",
            "localPosition.x", "localPosition.y", "localPosition.z", 
        };

        sealed class MergePhysBoneMerger : MergePhysBoneEditorModificationUtils
        {
            private SerializedObject _mergedPhysBone;
            private int _maxChainLength; // = maxBoneChainIndex + (endpointPosition != Vector3.zero ? 1 : 0))

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

#if AAO_VRCSDK3_AVATARS_IGNORE_OTHER_PHYSBONE
                if (IgnoreOtherPhysBones.IsOverride)
                    _mergedPhysBone.FindProperty(IgnoreOtherPhysBones.PhysBoneValueName).boolValue
                        = IgnoreOtherPhysBones.OverrideValue.boolValue;
                else
                    _mergedPhysBone.FindProperty(IgnoreOtherPhysBones.PhysBoneValueName).boolValue
                        = !IgnoreOtherPhysBones.SourceValue!.hasMultipleDifferentValues &&
                          IgnoreOtherPhysBones.SourceValue!.boolValue;
#endif
            }

            protected override void OptionParameter()
            {
                _mergedPhysBone.FindProperty(Parameter.PhysBoneValueName).stringValue = Parameter.OverrideValue.stringValue;
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
                        prop == Radius 
                            ? FixTransformCurve(prop.GetCurveProperty(@override).animationCurveValue)
                            : FixBoneCurve(prop.GetCurveProperty(@override).animationCurveValue);
                }
            }

            protected override void Pb3DCurveProp(string label, string pbXCurveLabel, string pbYCurveLabel, string pbZCurveLabel,
                CurveVector3ConfigProp prop, bool forceOverride = false)
            {
                switch (prop.GetOverride(forceOverride))
                {
                    case MergePhysBone.LimitRotationConfigStruct.Override.Copy:
                        _mergedPhysBone.FindProperty(prop.PhysBoneValueName).vector3Value =
                            prop.SourceValue!.vector3Value;
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveXName).animationCurveValue =
                            FixBoneCurve(prop.SourceCurveX!.animationCurveValue);
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveYName).animationCurveValue =
                            FixBoneCurve(prop.SourceCurveY!.animationCurveValue);
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveZName).animationCurveValue =
                            FixBoneCurve(prop.SourceCurveZ!.animationCurveValue);
                        break;
                    case MergePhysBone.LimitRotationConfigStruct.Override.Override:
                        _mergedPhysBone.FindProperty(prop.PhysBoneValueName).vector3Value =
                            prop.OverrideValue.vector3Value;
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveXName).animationCurveValue =
                            prop.OverrideCurveX.animationCurveValue;
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveYName).animationCurveValue =
                            prop.OverrideCurveY.animationCurveValue;
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveZName).animationCurveValue =
                            prop.OverrideCurveZ.animationCurveValue;
                        break;
                    case MergePhysBone.LimitRotationConfigStruct.Override.Fix:
                        // Fixing rotation is proceeded before.
                        // We just reset the value and curve.
                        _mergedPhysBone.FindProperty(prop.PhysBoneValueName).vector3Value = Vector3.zero;
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveXName).animationCurveValue = new AnimationCurve();
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveYName).animationCurveValue = new AnimationCurve();
                        _mergedPhysBone.FindProperty(prop.PhysBoneCurveZName).animationCurveValue = new AnimationCurve();
                        break;
                    case var overrideMode:
                        throw new ArgumentOutOfRangeException(nameof(prop), overrideMode, $"Invalid override mode: {overrideMode}");
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
            
            // Fixes a transform curve (curves ratio calculated with CalcTransformRatio) that is used for calculating radius
            private AnimationCurve FixTransformCurve(AnimationCurve curve) => FixCurve(curve, _maxChainLength);

            // Fixes a bone curve (curves ratio calculated with CalcBoneRatio) that is used for calculating limit and force
            private AnimationCurve FixBoneCurve(AnimationCurve curve) => FixCurve(curve, _maxChainLength - 1);

            private AnimationCurve FixCurve(AnimationCurve curve, int chainLength)
            {
                if (curve == null || curve.length == 0)
                    return new AnimationCurve();
                if (chainLength <= 0)
                {
                    // If original chain length is less than or equals to 0, CalcBoneRatio always returns 1,
                    // and the first frame of curve is used for whole chain.
                    // We just calculate ratio for weight 0 and set it to whole curve.
                    // This case is incorrect for transform chain, but transform chain with length 0
                    // means no affected transform, so it won't cause any problem.
                    var value = curve.Evaluate(0);
                    return AnimationCurve.Constant(0, 1, value);
                }
                var offset = 1f / (chainLength + 1f);
                var tangentRatio = (chainLength + 1f) / chainLength;
                var keys = curve.keys;
                foreach (ref var curveKey in keys.AsSpan())
                {
                    curveKey.time = Mathf.LerpUnclamped(offset, 1, curveKey.time);
                    curveKey.inTangent *= tangentRatio;
                    curveKey.outTangent *= tangentRatio;
                }
                return new AnimationCurve(keys);
            }
        }
    }
}

#endif
