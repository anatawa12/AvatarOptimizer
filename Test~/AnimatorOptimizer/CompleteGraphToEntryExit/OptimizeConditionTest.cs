using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using UnityEditor.Animations;
using NUnit.Framework;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer.CompleteGraphToEntryExits
{
    using IntRangeSet = RangeSet<int, RangeIntTrait>;
    using FloatRangeSet = RangeSet<float, RangeFloatTrait>;

    public class OptimizeConditionTest
    {
        // utilities
        static AnimatorCondition AnimatorCondition(string parameter, AnimatorConditionMode mode, float threshold = 0) =>
            new()
            {
                parameter = parameter,
                mode = mode,
                threshold = threshold,
            };
        static AnimatorCondition GreaterCondition(string parameter, float threshold) => AnimatorCondition(parameter, AnimatorConditionMode.Greater, threshold);
        static AnimatorCondition LessCondition(string parameter, float threshold) => AnimatorCondition(parameter, AnimatorConditionMode.Less, threshold);
        static AnimatorCondition EqualsCondition(string parameter, float threshold) => AnimatorCondition(parameter, AnimatorConditionMode.Equals, threshold);
        static AnimatorCondition NotEqualsCondition(string parameter, float threshold) => AnimatorCondition(parameter, AnimatorConditionMode.NotEqual, threshold);
        static AnimatorCondition IfCondition(string parameter) => AnimatorCondition(parameter, AnimatorConditionMode.If);
        static AnimatorCondition IfNotCondition(string parameter) => AnimatorCondition(parameter, AnimatorConditionMode.IfNot);

        // Removed Canonicalize; introduce comparer to be used with Is.EquivalentTo(...).Using(...)
        private class AnimatorConditionArrayComparer : IEqualityComparer<AnimatorCondition[]>
        {
            public static AnimatorConditionArrayComparer Instance { get; } = new();
            private readonly StringComparison sc = StringComparison.Ordinal;
            public bool Equals(AnimatorCondition[] x, AnimatorCondition[] y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null || y is null) return false;
                if (x.Length != y.Length) return false;

                return x.ToHashSet(CompleteGraphToEntryExit.AnimatorConditionEqualityComparer.Instance).SetEquals(y);
            }

            public int GetHashCode(AnimatorCondition[] obj)
            {
                var hash = obj.Length;
                foreach (var condition in obj)
                    hash ^= CompleteGraphToEntryExit.AnimatorConditionEqualityComparer.Instance.GetHashCode(condition);
                return hash;
            }
        }

        // -------------------------
        // OptimizeFloatConditions (single parameterized test)
        // -------------------------
        static IEnumerable<TestCaseData> FloatCases()
        {
            // Merge greater: Greater(2) OR Greater(1) => single Greater(1)
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("f", 2f) },
                    new[] { GreaterCondition("f", 1f) }
                },
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("f", 1f) }
                }
            ).SetName("Float_MergeGreater");

            // Empty conjunction: one branch is empty -> entire (empty conditions)
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    Array.Empty<AnimatorCondition>()
                },
                new List<AnimatorCondition[]>
                {
                    Array.Empty<AnimatorCondition>()
                }
            ).SetName("Float_EmptyConjunction");

            // Disjoint ranges: (1,3) and (3,5) -> cannot merge (touch at 3 exclusive)
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("f", 1f), LessCondition("f", 3f) },
                    new[] { GreaterCondition("f", 3f), LessCondition("f", 5f) }
                },
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("f", 1f), LessCondition("f", 3f) },
                    new[] { GreaterCondition("f", 3f), LessCondition("f", 5f) }
                }
            ).SetName("Float_DisjointRanges");

            // Connecting/overlapping float ranges should merge into single bigger range.
            // e.g. (1,3) and (2.9,5) overlap -> (1,5)
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("f", 1f), LessCondition("f", 3f) },
                    new[] { GreaterCondition("f", 2.9f), LessCondition("f", 5f) }
                },
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("f", 1f), LessCondition("f", 5f) }
                }
            ).SetName("Float_ConnectingRanges_MergeToSingle");

            // Multiple overlapping/connecting float ranges chain -> merge to one (1,7)
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("f", 1f), LessCondition("f", 3f) },    // (1,3)
                    new[] { GreaterCondition("f", 2.5f), LessCondition("f", 5f) },  // (2.5,5) overlaps previous
                    new[] { GreaterCondition("f", 4.9f), LessCondition("f", 7f) }   // (4.9,7) overlaps second
                },
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("f", 1f), LessCondition("f", 7f) }
                }
            ).SetName("Float_MultipleConnectingRanges_MergeToSingle");
        }

        [TestCaseSource(nameof(FloatCases))]
        public void OptimizeFloatConditions_Various(List<AnimatorCondition[]> input, List<AnimatorCondition[]> expected)
        {
            var res = CompleteGraphToEntryExit.OptimizeConditions<FloatRangeSet, FloatSetTrait>(input);
            Assert.That(res, Is.EquivalentTo(expected).Using(AnimatorConditionArrayComparer.Instance));
        }

        // -------------------------
        // OptimizeIntConditions (single parameterized test)
        // -------------------------
        static IEnumerable<TestCaseData> IntCases()
        {
            // Equals combine adjacent points: ==1 OR ==2 => >0 and <3
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { EqualsCondition("i", 1f) },
                    new[] { EqualsCondition("i", 2f) }
                },
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("i", 0f), LessCondition("i", 3f) }
                }
            ).SetName("Int_EqualsCombineAdjacentPoints");

            // NotEqual: !=2 => preserved as a NotEqual condition (converted later by OptimizeCondition)
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { NotEqualsCondition("i", 2f) }
                },
                new List<AnimatorCondition[]>
                {
                    new[] { NotEqualsCondition("i", 2f) },
                }
            ).SetName("Int_NotEqualProducesTwoRanges");

            // Unsatisfiable: ==1 AND ==2 -> none
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { EqualsCondition("i", 1f), EqualsCondition("i", 2f) }
                },
                new List<AnimatorCondition[]>()
            ).SetName("Int_UnsatisfiableConjunction");

            // Connecting int ranges: (1,3) and (3,5) -> single range [1,5]
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    // to represent inclusive integer ranges [1,3] use >0 and <4
                    new[] { GreaterCondition("i", 0f), LessCondition("i", 4f) },
                    new[] { GreaterCondition("i", 2f), LessCondition("i", 6f) } // [3,5]
                },
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("i", 0f), LessCondition("i", 6f) } // [1,5]
                }
            ).SetName("Int_ConnectingRanges_MergeToSingle");

            // Multiple isolated points collapse into one range with holes.
            // Points: 1, 4, 6 -> merged range [1,6] with holes 2,3,5 (represented as NotEquals)
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { EqualsCondition("i", 1f) },
                    new[] { EqualsCondition("i", 4f) },
                    new[] { EqualsCondition("i", 6f) },
                },
                new List<AnimatorCondition[]>
                {
                    new[]
                    {
                        GreaterCondition("i", 0f), // >=1
                        LessCondition("i", 7f),    // <=6
                        NotEqualsCondition("i", 2f),
                        NotEqualsCondition("i", 3f),
                        NotEqualsCondition("i", 5f)
                    }
                }
            ).SetName("Int_MultiplePoints_MergeToOneWithHoles");

            // Multiple connecting integer ranges that form a single contiguous range.
            // [1,3], [3,5], [5,7] -> merged to [1,7]
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("i", 0f), LessCondition("i", 4f) }, // [1,3]
                    new[] { GreaterCondition("i", 2f), LessCondition("i", 6f) }, // [3,5]
                    new[] { GreaterCondition("i", 4f), LessCondition("i", 8f) }  // [5,7]
                },
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("i", 0f), LessCondition("i", 8f) } // [1,7]
                }
            ).SetName("Int_MultipleConnectingRanges_MergeToSingle");

            // NEW: Multiple ranges that merge into one range with a single hole.
            // Ranges: [1..3] and [5..7] => merged [1..7] with hole 4
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("i", 0f), LessCondition("i", 4f) }, // [1,3]
                    new[] { GreaterCondition("i", 4f), LessCondition("i", 8f) }  // [5,7]
                },
                new List<AnimatorCondition[]>
                {
                    new[]
                    {
                        GreaterCondition("i", 0f), // >=1
                        LessCondition("i", 8f),    // <=7
                        NotEqualsCondition("i", 4f) // hole at 4
                    }
                }
            ).SetName("Int_MultipleRanges_MergeToOne_WithSingleHole");

            // NEW: Multiple ranges that merge into one range with two holes.
            // Ranges: [1..2] and [5..6] => merged [1..6] with holes 3 and 4
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("i", 0f), LessCondition("i", 3f) }, // [1,2]
                    new[] { GreaterCondition("i", 4f), LessCondition("i", 7f) }  // [5,6]
                },
                new List<AnimatorCondition[]>
                {
                    new[]
                    {
                        GreaterCondition("i", 0f), // >=1
                        LessCondition("i", 7f),    // <=6
                        NotEqualsCondition("i", 3f),
                        NotEqualsCondition("i", 4f)
                    }
                }
            ).SetName("Int_MultipleRanges_MergeToOne_WithTwoHoles");
        }

        [TestCaseSource(nameof(IntCases))]
        public void OptimizeIntConditions_Various(List<AnimatorCondition[]> input, List<AnimatorCondition[]> expected)
        {
            var res = CompleteGraphToEntryExit.OptimizeConditions<IntRangeSet, IntSetTrait>(input);
            Assert.That(res, Is.EquivalentTo(expected).Using(AnimatorConditionArrayComparer.Instance));
        }

        // -------------------------
        // OptimizeBoolConditions (single parameterized test)
        // -------------------------
        static IEnumerable<TestCaseData> BoolCases()
        {
            // both true and false across branches -> always true (empty condition)
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { IfCondition("b") },
                    new[] { IfNotCondition("b") }
                },
                new List<AnimatorCondition[]>
                {
                    Array.Empty<AnimatorCondition>()
                }
            ).SetName("Bool_BothTrueAndFalse_CollapsesToAlwaysTrue");

            // all never true -> none
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { IfCondition("b"), IfNotCondition("b") },
                    new[] { IfCondition("b"), IfNotCondition("b") }
                },
                new List<AnimatorCondition[]>()
            ).SetName("Bool_AllNever_YieldsNoConditions");

            // only true
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { IfCondition("b") }
                },
                new List<AnimatorCondition[]>
                {
                    new[] { IfCondition("b") }
                }
            ).SetName("Bool_OnlyTrue");

            // only false
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { IfNotCondition("b") }
                },
                new List<AnimatorCondition[]>
                {
                    new[] { IfNotCondition("b") }
                }
            ).SetName("Bool_OnlyFalse");
        }

        [TestCaseSource(nameof(BoolCases))]
        public void OptimizeBoolConditions_Various(List<AnimatorCondition[]> input, List<AnimatorCondition[]> expected)
        {
            var res = CompleteGraphToEntryExit.OptimizeConditions<BoolSet, BoolSetTrait>(input);
            Assert.That(res, Is.EquivalentTo(expected).Using(AnimatorConditionArrayComparer.Instance));
        }

        // -------------------------
        // OptimizeTriggerConditions (single parameterized test)
        // -------------------------
        static IEnumerable<TestCaseData> TriggerCases()
        {
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { IfCondition("t") },
                    new[] { IfCondition("t") }
                },
                new List<AnimatorCondition[]>
                {
                    new[] { IfCondition("t") }
                }
            ).SetName("Trigger_NonEmptyYieldsIf");

            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    Array.Empty<AnimatorCondition>(),
                    new[] { IfCondition("t") }
                },
                new List<AnimatorCondition[]>
                {
                    Array.Empty<AnimatorCondition>(),
                }
            ).SetName("Trigger_MixedEmptyAndIf");
        }

        [TestCaseSource(nameof(TriggerCases))]
        public void OptimizeTriggerConditions_Various(List<AnimatorCondition[]> input, List<AnimatorCondition[]> expected)
        {
            var res = CompleteGraphToEntryExit.OptimizeConditions<BoolSet, BoolSetTrait>(input);
            Assert.That(res, Is.EquivalentTo(expected).Using(AnimatorConditionArrayComparer.Instance));
        }

        // -------------------------
        // OptimizeCondition (integration single parameterized test)
        // -------------------------
        static IEnumerable<TestCaseData> OptimizeConditionCases()
        {
            // (i == 1 OR i == 2) AND b == true  => i merged, b preserved
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { EqualsCondition("i", 1f), IfCondition("b") },
                    new[] { EqualsCondition("i", 2f), IfCondition("b") },
                },
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("i", 0f), LessCondition("i", 3f), IfCondition("b") }
                },
                new Dictionary<string, AnimatorControllerParameterType>
                {
                    ["i"] = AnimatorControllerParameterType.Int,
                    ["b"] = AnimatorControllerParameterType.Bool
                }
            ).SetName("OptimizeCondition_MultiParameterTypes");

            // Float: merge greater thresholds
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("f", 2f) },
                    new[] { GreaterCondition("f", 1f) }
                },
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("f", 1f) }
                },
                new Dictionary<string, AnimatorControllerParameterType>
                {
                    ["f"] = AnimatorControllerParameterType.Float
                }
            ).SetName("OptimizeCondition_FloatMerge");

            // Float: disjoint ranges remain separate
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("f", 1f), LessCondition("f", 3f) },
                    new[] { GreaterCondition("f", 3f), LessCondition("f", 5f) }
                },
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("f", 1f), LessCondition("f", 3f) },
                    new[] { GreaterCondition("f", 3f), LessCondition("f", 5f) }
                },
                new Dictionary<string, AnimatorControllerParameterType>
                {
                    ["f"] = AnimatorControllerParameterType.Float
                }
            ).SetName("OptimizeCondition_FloatDisjointRanges");

            // Unknown parameter type: should be preserved (no optimization)
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("x", 1f) }
                },
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("x", 1f) }
                },
                new Dictionary<string, AnimatorControllerParameterType>() // x missing
            ).SetName("OptimizeCondition_UnknownType_Preserved");

            // If one branch lacks the float condition while other has it but both share same other-conditions,
            // the result should collapse to the other-condition only (empty thisConditions -> otherConditions preserved)
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { IfCondition("b") }, // branch without 'f' but has same other-condition If b
                    new[] { GreaterCondition("f", 1f), IfCondition("b") }
                },
                new List<AnimatorCondition[]>
                {
                    new[] { IfCondition("b") } // 'f' is removed because one branch had no 'f' (always-true for that group)
                },
                new Dictionary<string, AnimatorControllerParameterType>
                {
                    ["f"] = AnimatorControllerParameterType.Float,
                    ["b"] = AnimatorControllerParameterType.Bool
                }
            ).SetName("OptimizeCondition_EmptyThisCondition_PreservesOther");

            // Int: merge distant points with holes (==1 OR ==4 => range [1..4] with holes 2,3)
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { EqualsCondition("i", 1f) },
                    new[] { EqualsCondition("i", 4f) }
                },
                new List<AnimatorCondition[]>
                {
                    new[]
                    {
                        GreaterCondition("i", 0f),
                        LessCondition("i", 5f),
                        NotEqualsCondition("i", 2f),
                        NotEqualsCondition("i", 3f)
                    }
                },
                new Dictionary<string, AnimatorControllerParameterType>
                {
                    ["i"] = AnimatorControllerParameterType.Int
                }
            ).SetName("OptimizeCondition_IntMergeWithHoles");

            // Int: unsatisfiable conjunction inside single branch should be removed (no outputs)
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { EqualsCondition("i", 1f), EqualsCondition("i", 2f) } // impossible conjunction
                },
                new List<AnimatorCondition[]>(),
                new Dictionary<string, AnimatorControllerParameterType>
                {
                    ["i"] = AnimatorControllerParameterType.Int
                }
            ).SetName("OptimizeCondition_IntUnsatisfiableConjunction_Removed");

            // Grouping by other conditions: different other-conditions keep separate optimized results
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { EqualsCondition("i", 1f), IfCondition("b") },
                    new[] { EqualsCondition("i", 2f), IfNotCondition("b") }
                },
                new List<AnimatorCondition[]>
                {
                    new[] { EqualsCondition("i", 1f), IfCondition("b") },
                    new[] { EqualsCondition("i", 2f), IfNotCondition("b") }
                },
                new Dictionary<string, AnimatorControllerParameterType>
                {
                    ["i"] = AnimatorControllerParameterType.Int,
                    ["b"] = AnimatorControllerParameterType.Bool
                }
            ).SetName("OptimizeCondition_GroupedByOtherConditions_PreservedSeparately");

            // NEW: Int multiple ranges -> single with single hole (integration)
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("i", 0f), LessCondition("i", 4f) }, // [1,3]
                    new[] { GreaterCondition("i", 4f), LessCondition("i", 8f) }  // [5,7]
                },
                new List<AnimatorCondition[]>
                {
                    new[]
                    {
                        GreaterCondition("i", 0f),
                        LessCondition("i", 8f),
                        NotEqualsCondition("i", 4f)
                    }
                },
                new Dictionary<string, AnimatorControllerParameterType>
                {
                    ["i"] = AnimatorControllerParameterType.Int
                }
            ).SetName("OptimizeCondition_IntMultipleRanges_ToOneWithSingleHole");

            // NEW: Int multiple ranges -> single with two holes (integration)
            yield return new TestCaseData(
                new List<AnimatorCondition[]>
                {
                    new[] { GreaterCondition("i", 0f), LessCondition("i", 3f) }, // [1,2]
                    new[] { GreaterCondition("i", 4f), LessCondition("i", 7f) }  // [5,6]
                },
                new List<AnimatorCondition[]>
                {
                    new[]
                    {
                        GreaterCondition("i", 0f),
                        LessCondition("i", 7f),
                        NotEqualsCondition("i", 3f),
                        NotEqualsCondition("i", 4f)
                    }
                },
                new Dictionary<string, AnimatorControllerParameterType>
                {
                    ["i"] = AnimatorControllerParameterType.Int
                }
            ).SetName("OptimizeCondition_IntMultipleRanges_ToOneWithTwoHoles");
        }

        [TestCaseSource(nameof(OptimizeConditionCases))]
        public void OptimizeCondition_Various(List<AnimatorCondition[]> input, List<AnimatorCondition[]> expected, Dictionary<string, AnimatorControllerParameterType> typeByName)
        {
            var res = CompleteGraphToEntryExit.OptimizeCondition(input, typeByName);
            Assert.That(res, Is.EquivalentTo(expected).Using(AnimatorConditionArrayComparer.Instance));
        }
    }
}
