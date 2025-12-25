using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Animations;
using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    public class ConditionsTest
    {
        // Parameterized tests for Conditions.IsContradiction
        private static IEnumerable<TestCaseData> IsContradictionCases()
        {
            // Float: non-contradictory (0 < x < 1)
            yield return new TestCaseData(
                new AnimatorCondition[]
                {
                    new() { parameter = "f", mode = AnimatorConditionMode.Greater, threshold = 0f },
                    new() { parameter = "f", mode = AnimatorConditionMode.Less, threshold = 1f },
                },
                new Dictionary<string, AnimatorControllerParameterType> { ["f"] = AnimatorControllerParameterType.Float },
                false
            ).SetName("Float_NonContradiction_GreaterAndLess");

            // Float: contradiction (x > 1 and x < 1)
            yield return new TestCaseData(
                new AnimatorCondition[]
                {
                    new() { parameter = "f", mode = AnimatorConditionMode.Greater, threshold = 1f },
                    new() { parameter = "f", mode = AnimatorConditionMode.Less, threshold = 1f },
                },
                new Dictionary<string, AnimatorControllerParameterType> { ["f"] = AnimatorControllerParameterType.Float },
                true
            ).SetName("Float_Contradiction_GreaterAndLess_SameBound");

            // Int: contradiction (equals 1 and equals 2)
            yield return new TestCaseData(
                new AnimatorCondition[]
                {
                    new() { parameter = "i", mode = AnimatorConditionMode.Equals, threshold = 1f },
                    new() { parameter = "i", mode = AnimatorConditionMode.Equals, threshold = 2f },
                },
                new Dictionary<string, AnimatorControllerParameterType> { ["i"] = AnimatorControllerParameterType.Int },
                true
            ).SetName("Int_Contradiction_EqualsDifferent");

            // Int: non-contradictory range (i > 0 && i < 5)
            yield return new TestCaseData(
                new AnimatorCondition[]
                {
                    new() { parameter = "i", mode = AnimatorConditionMode.Greater, threshold = 0f },
                    new() { parameter = "i", mode = AnimatorConditionMode.Less, threshold = 5f },
                },
                new Dictionary<string, AnimatorControllerParameterType> { ["i"] = AnimatorControllerParameterType.Int },
                false
            ).SetName("Int_NonContradiction_GreaterAndLess");

            // Bool: contradiction (If and IfNot)
            yield return new TestCaseData(
                new AnimatorCondition[]
                {
                    new() { parameter = "b", mode = AnimatorConditionMode.If, threshold = 0f },
                    new() { parameter = "b", mode = AnimatorConditionMode.IfNot, threshold = 0f },
                },
                new Dictionary<string, AnimatorControllerParameterType> { ["b"] = AnimatorControllerParameterType.Bool },
                true
            ).SetName("Bool_Contradiction_IfAndIfNot");

            // Trigger: treated as Bool, contradiction (If and IfNot)
            yield return new TestCaseData(
                new AnimatorCondition[]
                {
                    new() { parameter = "t", mode = AnimatorConditionMode.If, threshold = 0f },
                    new() { parameter = "t", mode = AnimatorConditionMode.IfNot, threshold = 0f },
                },
                new Dictionary<string, AnimatorControllerParameterType> { ["t"] = AnimatorControllerParameterType.Trigger },
                true
            ).SetName("Trigger_Contradiction_IfAndIfNot");

            // Multiple parameters: one parameter contradictory makes overall result true
            yield return new TestCaseData(
                new AnimatorCondition[]
                {
                    new AnimatorCondition { parameter = "f", mode = AnimatorConditionMode.Greater, threshold = 0f },
                    new AnimatorCondition { parameter = "f", mode = AnimatorConditionMode.Less, threshold = 1f },
                    new AnimatorCondition { parameter = "i", mode = AnimatorConditionMode.Equals, threshold = 1f },
                    new AnimatorCondition { parameter = "i", mode = AnimatorConditionMode.Equals, threshold = 2f },
                },
                new Dictionary<string, AnimatorControllerParameterType>
                {
                    ["f"] = AnimatorControllerParameterType.Float,
                    ["i"] = AnimatorControllerParameterType.Int,
                },
                true
            ).SetName("MultipleParameters_OneContradiction_OverallTrue");
        }

        [Test, TestCaseSource(nameof(IsContradictionCases))]
        public void IsContradiction_Param(AnimatorCondition[] conditions, Dictionary<string, AnimatorControllerParameterType> types, bool expected)
        {
            Assert.That(Conditions.IsContradiction(conditions, types), Is.EqualTo(expected));
        }

    }
}