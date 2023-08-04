using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal partial class AutomaticConfigurationProcessor
    {
        private AutomaticConfiguration _config;
        private OptimizerSession _session;

        private Dictionary<Object, Dictionary<string, AnimationProperty>> _modifiedProperties =
            new Dictionary<Object, Dictionary<string, AnimationProperty>>();

        public void Process(OptimizerSession session)
        {
            _session = session;
            _config = session.GetRootComponent<AutomaticConfiguration>();
            if (!_config) return;

            // TODO: implement
            GatherAnimationModifications();
            if (_config.freezeBlendShape)
                AutoFreezeBlendShape();
            if (_config.removeUnusedObjects)
                FindUnusedObjects();
        }

        private IReadOnlyDictionary<string, AnimationProperty> GetModifiedProperties(Component component)
        {
            return _modifiedProperties.TryGetValue(component, out var value) ? value : EmptyProperties;
        }

        private IReadOnlyDictionary<string, AnimationProperty> GetModifiedProperties(GameObject component)
        {
            return _modifiedProperties.TryGetValue(component, out var value) ? value : EmptyProperties;
        }

        private static readonly IReadOnlyDictionary<string, AnimationProperty> EmptyProperties =
            new ReadOnlyDictionary<string, AnimationProperty>(new Dictionary<string, AnimationProperty>());

        readonly struct AnimationProperty
        {
            public readonly AnimationPropertyFlags Flags;
            public bool IsConst => (Flags & AnimationPropertyFlags.Constant) != 0;
            public bool IsAlwaysApplied => (Flags & AnimationPropertyFlags.AlwaysApplied) != 0;
            public readonly float ConstValue;

            private AnimationProperty(AnimationPropertyFlags flags, float constValue) =>
                (Flags, ConstValue) = (flags, constValue);

            public static AnimationProperty Const(float value) =>
                new AnimationProperty(AnimationPropertyFlags.Constant, value);

            public static AnimationProperty Variable() =>
                new AnimationProperty(AnimationPropertyFlags.Variable, float.NaN);

            public AnimationProperty Merge(AnimationProperty b)
            {
                var isConstant = IsConst && b.IsConst && ConstValue.CompareTo(b.ConstValue) == 0;
                var isAlwaysApplied = IsAlwaysApplied && b.IsAlwaysApplied;

                return new AnimationProperty(
                    (isConstant ? AnimationPropertyFlags.Constant : AnimationPropertyFlags.Variable)
                    | (isAlwaysApplied ? AnimationPropertyFlags.AlwaysApplied : AnimationPropertyFlags.Variable),
                    ConstValue);
            }

            public static AnimationProperty? ParseProperty(AnimationCurve curve)
            {
                if (curve.keys.Length == 0) return null;
                if (curve.keys.Length == 1)
                    return Const(curve.keys[0].value);

                float constValue = 0;
                foreach (var (preKey, postKey) in curve.keys.ZipWithNext())
                {
                    var preWeighted = preKey.weightedMode == WeightedMode.Out || preKey.weightedMode == WeightedMode.Both;
                    var postWeighted = postKey.weightedMode == WeightedMode.In || postKey.weightedMode == WeightedMode.Both;

                    if (preKey.value.CompareTo(postKey.value) != 0) return Variable();
                    constValue = preKey.value;
                    // it's constant
                    if (float.IsInfinity(preKey.outWeight) || float.IsInfinity(postKey.inTangent))
                        continue;
                    if (preKey.outTangent == 0 && postKey.inTangent == 0)
                        continue;
                    if (preWeighted && postWeighted && preKey.outWeight == 0 && postKey.inWeight == 0)
                        continue;
                    return Variable();
                }

                return Const(constValue);
            }

            public AnimationProperty AlwaysApplied() =>
                new AnimationProperty(Flags | AnimationPropertyFlags.AlwaysApplied, ConstValue);
            public AnimationProperty PartiallyApplied() =>
                new AnimationProperty(Flags & ~AnimationPropertyFlags.AlwaysApplied, ConstValue);
        }

        [Flags]
        enum AnimationPropertyFlags
        {
            Variable = 0,
            Constant = 1,
            AlwaysApplied = 2,
        }
    }
}
