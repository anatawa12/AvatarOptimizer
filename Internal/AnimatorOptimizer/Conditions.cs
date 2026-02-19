using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;

public static class Conditions
{
    public static bool IsContradiction(
        // AnimatorCondition[AND]
        IEnumerable<AnimatorCondition> conditions,
        Dictionary<string, AnimatorControllerParameterType> typeByName)
    {
        var conditionsByParameter = conditions.GroupBy(c => c.parameter);

        foreach (var group in conditionsByParameter)
        {
            var parameterName = group.Key;
            var parameterType = typeByName[parameterName];

            var parameterConds = group.ToArray();
            bool isContradiction = parameterType switch
            {
                AnimatorControllerParameterType.Float =>
                    RangesUtil.FloatRangeSetFromConditions(parameterConds).IsEmpty(),
                AnimatorControllerParameterType.Int =>
                    RangesUtil.IntRangeSetFromConditions(parameterConds).IsEmpty(),
                AnimatorControllerParameterType.Bool =>
                    RangesUtil.BoolSetFromConditions(parameterConds).IsEmpty(),
                AnimatorControllerParameterType.Trigger =>
                    RangesUtil.BoolSetFromConditions(parameterConds).IsEmpty(),
                _ => throw new InvalidOperationException($"Unknown parameter type: {parameterType}")
            };

            if (isContradiction) return true;
        }

        return false;
    }
}
