#if AAO_VRCSDK3_AVATARS_PARAMETER_BIT_WIDTH
using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    // This pass reduces the bit width of synced Int parameters in VRCExpressionParameters.
    // The bit width is inferred from the values written to the parameter by Expressions Menu and Parameter Drivers,
    // and the values read by transition conditions of Animators.
    // Parameters whose values cannot be determined are kept as is.
    internal class ReduceParameterBitWidth : TraceAndOptimizePass<ReduceParameterBitWidth>
    {
        public override string DisplayName => "T&O: Reduce Parameter Bit Width";
        protected override bool Enabled(TraceAndOptimizeState state) => state.ReduceParameterBitWidth;

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            var descriptor = context.AvatarRootObject.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null || !descriptor.customExpressions) return;
            var expressionParameters = descriptor.expressionParameters;
            if (expressionParameters == null || expressionParameters.parameters == null) return;

            var usages = CreateUsages(expressionParameters.parameters);
            if (usages.Count == 0) return;

            var collector = new UsageCollector(usages);
            collector.CollectMenu(descriptor.expressionsMenu);
            collector.CollectAnimators(context.AvatarRootObject, descriptor);
            collector.CollectComponents(context);

            VRCExpressionParameters? cloned = null;
            foreach (var (name, usage) in usages)
            {
                var parameter = expressionParameters.parameters[usage.Index];
                var bitWidth = ComputeBitWidth(usage);
                Tracing.Trace(TracingArea.TraceAndOptimizeDecision,
                    $"ReduceParameterBitWidth: {name}: writer={usage.HasWriter}, unbounded={usage.Unbounded}, range=[{usage.Min}, {usage.Max}], bitWidth={bitWidth?.ToString() ?? "unchanged"}");
                if (bitWidth is not { } width || width >= parameter.GetBitWidthByType()) continue;

                cloned ??= CloneExpressionParameters(descriptor, expressionParameters);
                cloned.parameters[usage.Index].bitWidth = (byte)width;
            }
        }

        private static Dictionary<string, ParameterUsage> CreateUsages(VRCExpressionParameters.Parameter[] parameters)
        {
            // We cannot know which entry VRChat uses for duplicated names so we keep all of them as is.
            var duplicatedNames = parameters
                .Where(parameter => parameter != null && !string.IsNullOrEmpty(parameter.name))
                .GroupBy(parameter => parameter.name)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet();

            var external = AssetDescription.GetParametersChangedByExternalTools();
            var usages = new Dictionary<string, ParameterUsage>();
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter == null || string.IsNullOrEmpty(parameter.name)) continue;
                if (duplicatedNames.Contains(parameter.name)) continue;
                if (!IsCandidate(parameter, external)) continue;
                usages.Add(parameter.name, new ParameterUsage(i, IsSigned(parameter), parameter.defaultValue));
            }

            return usages;
        }

        private static bool IsCandidate(VRCExpressionParameters.Parameter parameter, AssetDescription.OscParameterInfo external)
        {
            if (!parameter.networkSynced) return false;
            if (parameter.valueType != VRCExpressionParameters.ValueType.Int) return false;
            if (!HasDefaultBitWidth(parameter)) return false;
            if (VRChatBuiltinParameters.Contains(parameter.name)) return false;
            if (external.ExactMatch.Contains(parameter.name)) return false;
            if (external.RegexMatch.Any(regex => regex.IsMatch(parameter.name))) return false;
            return true;
        }

        // Since the default bit width is 8, we cannot distinguish explicitly configured 8 from the default.
        // We treat any other width as explicitly configured and keep it as is.
        private static bool HasDefaultBitWidth(VRCExpressionParameters.Parameter parameter) =>
            parameter.GetBitWidthByType() == VRCExpressionParameters.Parameter.MaxBitWidth;

        private static bool IsSigned(VRCExpressionParameters.Parameter parameter) =>
            parameter.GetRepresentationByType() == VRCExpressionParameters.Parameter.ValueRepresentation.Signed;

        private static (int min, int max) RepresentableRange(bool signed, int width) =>
            signed ? (-(1 << (width - 1)), (1 << (width - 1)) - 1) : (0, (1 << width) - 1);

        private static int? ComputeBitWidth(ParameterUsage usage)
        {
            if (!usage.HasWriter || usage.Unbounded) return null;

            for (var width = VRCExpressionParameters.Parameter.MinBitWidth; width <= VRCExpressionParameters.Parameter.MaxBitWidth; width++)
            {
                var (min, max) = RepresentableRange(usage.Signed, width);
                if (min <= usage.Min && usage.Max <= max) return width;
            }

            // The values do not fit in the current representation so we do not touch the parameter.
            return null;
        }

        private static VRCExpressionParameters CloneExpressionParameters(VRCAvatarDescriptor descriptor,
            VRCExpressionParameters original)
        {
            var cloned = Object.Instantiate(original);
            cloned.name = original.name;
            ObjectRegistry.RegisterReplacedObject(original, cloned);
            descriptor.expressionParameters = cloned;
            return cloned;
        }

        private class ParameterUsage
        {
            public readonly int Index;
            public readonly bool Signed;
            public int Min;
            public int Max;
            public bool HasWriter;
            public bool Unbounded;

            public ParameterUsage(int index, bool signed, float defaultValue)
            {
                Index = index;
                Signed = signed;
                Min = FloorToInt(defaultValue);
                Max = CeilToInt(defaultValue);
            }

            // The rounding of non-integer values differs between Copy (floor) and other conversions (nearest),
            // so we include both of the neighboring integers.
            public void Write(float value)
            {
                HasWriter = true;
                Include(FloorToInt(value));
                Include(CeilToInt(value));
            }

            public void ReadByCondition(int value)
            {
                var (min, max) = RepresentableRange(Signed, VRCExpressionParameters.Parameter.MaxBitWidth);
                Include(Math.Clamp(value, min, max));
            }

            public void Unknown()
            {
                HasWriter = true;
                Unbounded = true;
            }

            private void Include(int value)
            {
                Min = Math.Min(Min, value);
                Max = Math.Max(Max, value);
            }
        }

        private class UsageCollector
        {
            private readonly Dictionary<string, ParameterUsage> _usages;

            public UsageCollector(Dictionary<string, ParameterUsage> usages) => _usages = usages;

            public void CollectMenu(VRCExpressionsMenu? root)
            {
                if (root == null) return;

                var visited = new HashSet<VRCExpressionsMenu> { root };
                var queue = new Queue<VRCExpressionsMenu>();
                queue.Enqueue(root);

                while (queue.Count != 0)
                {
                    var menu = queue.Dequeue();
                    if (menu.controls == null) continue;

                    foreach (var control in menu.controls)
                    {
                        if (control == null) continue;

                        if (control.parameter?.name is { } name && _usages.TryGetValue(name, out var usage))
                        {
                            // The parameter is set to the value when the control is active,
                            // and reset to the default value (or zero) when the control is inactive.
                            usage.Write(control.value);
                            usage.Write(0);
                        }

                        if (control.subParameters != null)
                        {
                            // Sub parameters of puppets receive float values.
                            foreach (var subParameter in control.subParameters)
                                if (subParameter?.name is { } subName && _usages.TryGetValue(subName, out var subUsage))
                                    subUsage.Unknown();
                        }

                        if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu
                            && control.subMenu != null && visited.Add(control.subMenu))
                            queue.Enqueue(control.subMenu);
                    }
                }
            }

            public void CollectAnimators(GameObject root, VRCAvatarDescriptor descriptor)
            {
                var controllers = new List<RuntimeAnimatorController>();

                if (root.GetComponent<Animator>() is { } animator && animator.runtimeAnimatorController != null)
                    controllers.Add(animator.runtimeAnimatorController);
                controllers.AddRange(descriptor.GetCustomizedPlayableLayerControllers());

                foreach (var runtimeController in controllers.Distinct())
                {
                    var (controller, _) = ACUtils.GetControllerAndOverrides(runtimeController);
                    var parameterTypes = new Dictionary<string, AnimatorControllerParameterType>();
                    foreach (var parameter in controller.parameters)
                        parameterTypes.TryAdd(parameter.name, parameter.type);

                    foreach (var layer in controller.layers)
                    {
                        if (layer.syncedLayerIndex >= 0) continue;
                        foreach (var transition in ACUtils.AllTransitions(layer.stateMachine))
                        foreach (var condition in transition.conditions)
                            CollectCondition(condition);
                    }

                    foreach (var behaviour in ACUtils.StateMachineBehaviours(runtimeController))
                        if (behaviour is VRC_AvatarParameterDriver driver)
                            CollectDriver(driver, parameterTypes);
                }
            }

            private void CollectCondition(AnimatorCondition condition)
            {
                if (!_usages.TryGetValue(condition.parameter, out var usage)) return;

                // Those values are only read by the conditions but the parameter should be able to represent them
                // to keep the conditions meaningful.
                // Values out of the current representation can never match so they are clamped.
                switch (condition.mode)
                {
                    case AnimatorConditionMode.Equals:
                    case AnimatorConditionMode.NotEqual:
                        usage.ReadByCondition(FloorToInt(condition.threshold));
                        usage.ReadByCondition(CeilToInt(condition.threshold));
                        break;
                    case AnimatorConditionMode.Greater:
                        usage.ReadByCondition(FloorToInt(condition.threshold) + 1);
                        break;
                    case AnimatorConditionMode.Less:
                        usage.ReadByCondition(CeilToInt(condition.threshold) - 1);
                        break;
                }
            }

            private void CollectDriver(VRC_AvatarParameterDriver driver,
                Dictionary<string, AnimatorControllerParameterType> parameterTypes)
            {
                if (driver.parameters == null) return;

                foreach (var parameter in driver.parameters)
                {
                    if (parameter == null || string.IsNullOrEmpty(parameter.name)) continue;
                    if (!_usages.TryGetValue(parameter.name, out var usage)) continue;

                    switch (parameter.type)
                    {
                        case VRC_AvatarParameterDriver.ChangeType.Set:
                            usage.Write(parameter.value);
                            break;
                        case VRC_AvatarParameterDriver.ChangeType.Random:
                            usage.Write(parameter.valueMin);
                            usage.Write(parameter.valueMax);
                            break;
                        case VRC_AvatarParameterDriver.ChangeType.Copy:
                            if (parameter.convertRange)
                            {
                                usage.Write(Math.Min(parameter.destMin, parameter.destMax));
                                usage.Write(Math.Max(parameter.destMin, parameter.destMax));
                            }
                            else if (!string.IsNullOrEmpty(parameter.source)
                                     && parameterTypes.TryGetValue(parameter.source, out var sourceType)
                                     && sourceType is AnimatorControllerParameterType.Bool
                                         or AnimatorControllerParameterType.Trigger)
                            {
                                usage.Write(0);
                                usage.Write(1);
                            }
                            else
                            {
                                usage.Unknown();
                            }
                            break;
                        default:
                            usage.Unknown();
                            break;
                    }
                }
            }

            public void CollectComponents(BuildContext context)
            {
                foreach (var component in context.GetComponents<Component>())
                {
                    switch (component)
                    {
                        case VRCPhysBone physBone:
                            MarkPrefixUnknown(physBone.parameter);
                            break;
                        case VRCContactReceiver receiver:
                            MarkPrefixUnknown(receiver.parameter);
                            break;
                        case VRCRaycast raycast:
                            MarkPrefixUnknown(raycast.Parameter);
                            break;
                        case VRCAvatarDescriptor descriptor:
                            if (descriptor.faceTrackingParameterBehaviors == null) break;
                            foreach (var behavior in descriptor.faceTrackingParameterBehaviors)
                            {
                                if (behavior == null) continue;
                                if (behavior.targetMode != VRCAvatarDescriptor.ParameterBehaviorTargetMode.ForwardToAnimator) continue;
                                if (string.IsNullOrEmpty(behavior.outputParameterName)) continue;
                                if (_usages.TryGetValue(behavior.outputParameterName, out var usage))
                                    usage.Unknown();
                            }
                            break;
                    }
                }
            }

            private void MarkPrefixUnknown(string? prefix)
            {
                if (string.IsNullOrEmpty(prefix)) return;
                foreach (var (name, usage) in _usages)
                    if (name == prefix || name.StartsWith(prefix + "_", StringComparison.Ordinal))
                        usage.Unknown();
            }
        }

        // Values far outside of the 8-bit range never fit so clamping them is safe.
        private const int ClampRange = 1 << 20;
        private static int FloorToInt(float value) => (int)Math.Floor(Math.Clamp(value, -ClampRange, ClampRange));
        private static int CeilToInt(float value) => (int)Math.Ceiling(Math.Clamp(value, -ClampRange, ClampRange));

        // https://creators.vrchat.com/avatars/animator-parameters/
        // Those parameters are written by VRChat itself so we cannot know the range of the values.
        private static readonly HashSet<string> VRChatBuiltinParameters = new()
        {
            "IsLocal",
            "PreviewMode",
            "Viseme",
            "Voice",
            "GestureLeft",
            "GestureRight",
            "GestureLeftWeight",
            "GestureRightWeight",
            "AngularY",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "VelocityMagnitude",
            "Upright",
            "Grounded",
            "Seated",
            "AFK",
            "TrackingType",
            "VRMode",
            "MuteSelf",
            "InStation",
            "Earmuffs",
            "IsOnFriendsList",
            "AvatarVersion",
            "ScaleModified",
            "ScaleFactor",
            "ScaleFactorInverse",
            "EyeHeightAsMeters",
            "EyeHeightAsPercent",
            "IsAnimatorEnabled",
            "UserEyeTracking",
            "UserFaceTracking",
        };
    }
}
#endif
