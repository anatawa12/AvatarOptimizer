using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorParserTest
{
    public class AnimationTest
    {
        #region source data verification

        [TestCaseSource(nameof(ConstSourceAnimationsData))]
        public void VerifyConstSourceAnimation(string name, string blendShapeProp, float constValue)
        {
            var clip = TestUtils.GetAssetAt<AnimationClip>($"AnimatorParser/{name}.anim");

            var binding =
                EditorCurveBinding.FloatCurve("", typeof(SkinnedMeshRenderer), blendShapeProp);

            Assert.That(AnimationUtility.GetCurveBindings(clip), Is.EqualTo(new[] { binding }));
            Assert.That(AnimationUtility.GetObjectReferenceCurveBindings(clip), Is.Empty);

            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            Assert.That(curve.Evaluate(0), Is.EqualTo(constValue));
            Assert.That(curve.Evaluate(0.5f), Is.EqualTo(constValue));
            Assert.That(curve.Evaluate(1), Is.EqualTo(constValue));
            Assert.That(curve.Evaluate(30), Is.EqualTo(constValue));
            Assert.That(curve.Evaluate(60), Is.EqualTo(constValue));
        }

        [TestCaseSource(nameof(VariableSourceAnimationsData))]
        public void VerifyVariableSourceAnimation(string name, string blendShapeProp)
        {
            var clip = TestUtils.GetAssetAt<AnimationClip>($"AnimatorParser/{name}.anim");

            var binding =
                EditorCurveBinding.FloatCurve("", typeof(SkinnedMeshRenderer), blendShapeProp);

            Assert.That(AnimationUtility.GetCurveBindings(clip), Is.EqualTo(new[] { binding }));
            Assert.That(AnimationUtility.GetObjectReferenceCurveBindings(clip), Is.Empty);

            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            var values = new HashSet<float>
            {
                curve.Evaluate(0),
                curve.Evaluate(0.5f),
                curve.Evaluate(1),
                curve.Evaluate(30),
                curve.Evaluate(60),
            };
            Assert.That(values.Count, Is.GreaterThan(1));
        }
        
        #endregion

        #region parser test

        [TestCaseSource(nameof(ConstSourceAnimationsData))]
        public void TestParsingConstSourceAnimation(string name, string blendShapeProp, float constValue)
        {
            var clip = TestUtils.GetAssetAt<AnimationClip>($"AnimatorParser/{name}.anim");
            var prefab = TestUtils.GetAssetAt<GameObject>($"AnimatorParser/AnimatorParserAnimated.prefab");
            var skinnedRenderer = prefab.GetComponent<SkinnedMeshRenderer>() ?? throw new InvalidOperationException();
            var parser = new AnimationParser();

            var parsed = parser.ParseMotion(prefab, clip, Utils.EmptyDictionary<AnimationClip, AnimationClip>())
                .ToImmutable();

            Assert.That(parsed.ModifiedProperties.Count, Is.EqualTo(1));
            Assert.That(parsed.ModifiedProperties.Keys, Has.Member((ComponentOrGameObject)skinnedRenderer));

            var props = parsed.ModifiedProperties[skinnedRenderer];

            Assert.That(props.Count, Is.EqualTo(1));
            Assert.That(props.Keys, Has.Member(blendShapeProp));

            var source = new AnimationSource(clip,
                EditorCurveBinding.FloatCurve("", typeof(SkinnedMeshRenderer), blendShapeProp));
            Assert.That(props[blendShapeProp], Is.EqualTo(AnimationFloatProperty.ConstAlways(constValue, source)));
        }

        [TestCaseSource(nameof(VariableSourceAnimationsData))]
        public void TestParsingVariableSourceAnimation(string name, string blendShapeProp)
        {
            var clip = TestUtils.GetAssetAt<AnimationClip>($"AnimatorParser/{name}.anim");
            var prefab = TestUtils.GetAssetAt<GameObject>($"AnimatorParser/AnimatorParserAnimated.prefab");
            var skinnedRenderer = prefab.GetComponent<SkinnedMeshRenderer>() ?? throw new InvalidOperationException();
            var parser = new AnimationParser();

            var parsed = parser.ParseMotion(prefab, clip, Utils.EmptyDictionary<AnimationClip, AnimationClip>())
                .ToImmutable();

            Assert.That(parsed.ModifiedProperties.Count, Is.EqualTo(1));
            Assert.That(parsed.ModifiedProperties.Keys, Has.Member((ComponentOrGameObject)skinnedRenderer));

            var props = parsed.ModifiedProperties[skinnedRenderer];

            Assert.That(props.Count, Is.EqualTo(1));
            Assert.That(props.Keys, Has.Member(blendShapeProp));
            
            Assert.That(props[blendShapeProp], Is.EqualTo(AnimationFloatProperty.Variable(TestSourceImpl.Instance)));
        }

        #endregion

        public static IEnumerable<TestCaseData> ConstSourceAnimationsData() =>
            ConstSourceAnimations().Select(t => new TestCaseData(t.Item1, t.Item2, t.Item3));

        public static IEnumerable<(string, string, float)> ConstSourceAnimations()
        {
            yield return ("Animate0To100", "blendShape.shape0", 100);
            yield return ("Animate1To100", "blendShape.shape1", 100);
            yield return ("Animate3To100", "blendShape.shape3", 100);
            yield return ("Animate4To100", "blendShape.shape4", 100);
            yield return ("Animate5To0", "blendShape.shape5", 0);
            yield return ("Animate5To100", "blendShape.shape5", 100);
            yield return ("Animate6To0", "blendShape.shape6", 0);
            yield return ("Animate6To100", "blendShape.shape6", 100);
            yield return ("Animate7To100", "blendShape.shape7", 100);
            yield return ("Animate8To100", "blendShape.shape8", 100);
            yield return ("Animate9To100", "blendShape.shape9", 100);
            yield return ("Animate10To100", "blendShape.shape10", 100);
            yield return ("Animate11To100", "blendShape.shape11", 100);
            yield return ("Animate12To10", "blendShape.shape12", 10);
            yield return ("Animate13To10", "blendShape.shape13", 10);
            yield return ("Animate14To10", "blendShape.shape14", 10);
            yield return ("Animate15To10", "blendShape.shape15", 10);
            yield return ("Animate16To100", "blendShape.shape16", 100);
            yield return ("Animate17To100", "blendShape.shape17", 100);
        }

        public static IEnumerable<TestCaseData> VariableSourceAnimationsData() =>
            VariableSourceAnimations().Select(t => new TestCaseData(t.Item1, t.Item2));

        public static IEnumerable<(string, string)> VariableSourceAnimations()
        {
            yield return ("Animate1ToVariable", "blendShape.shape1");
            yield return ("Animate2ToVariable", "blendShape.shape2");
            yield return ("Animate15ToVariable", "blendShape.shape15");
        }
    }
}