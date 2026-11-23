using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    /// <summary>
    /// Checks for Unity's behavior about time of BlendTree and state.
    ///
    /// MergeBlendTreeLayer and EntryExitToBlendTree depend on (or have to avoid) those behaviors,
    /// so we check them to notice changes in future Unity versions.
    /// See https://github.com/anatawa12/AvatarOptimizer/issues/1829
    /// </summary>
    public class BlendTreeTimeBehaviorChecks
    {
        private const string TargetName = "Target";
        private const string OtherName = "Other";
        private const float Tolerance = 0.005f;

        /// <summary>
        /// Checks that children of Direct BlendTree shares normalized time.
        ///
        /// The BlendTree has two children: 1 second linear clip with weight 1, and 'other' clip.
        /// The duration of the Direct BlendTree is the sum of 'weight * length' of each child (not normalized by
        /// the sum of weight), and all children are played with the same normalized time.
        /// Therefore, the playback speed of the 1 second clip depends on the length and the weight of the 'other' clip,
        /// so merging layers with time dependent clip into single Direct BlendTree changes the behavior.
        /// </summary>
        [UnityTest]
        public IEnumerator DirectBlendTreeChildrenShareNormalizedTime()
        {
            yield return new EnterPlayMode();

            const float elapsed = 0.5f;
            var failures = new List<string>();

            foreach (var (otherLength, otherWeight, otherIsConstant) in new[]
                     {
                         (4f, 0f, false),
                         (4f, 1f, false),
                         (4f, 0.5f, false),
                         // typical toggle animation: constant clip with two keyframes
                         (1 / 60f, 0f, true),
                         (1 / 60f, 1f, true),
                     })
            {
                var blendTree = new BlendTree
                {
                    name = "Direct",
                    blendType = BlendTreeType.Direct,
                    children = new[]
                    {
                        new ChildMotion
                        {
                            motion = LinearClip(TargetName, 1f),
                            directBlendParameter = "One",
                            timeScale = 1,
                        },
                        new ChildMotion
                        {
                            motion = otherIsConstant
                                ? ConstantClip(OtherName, otherLength)
                                : LinearClip(OtherName, otherLength),
                            directBlendParameter = "OtherWeight",
                            timeScale = 1,
                        },
                    },
                };

                var controller = CreateController(blendTree, out _,
                    ("One", 1f), ("OtherWeight", otherWeight));
                var (animator, target) = CreateAnimator(controller);

                Step(animator, elapsed);

                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                var actual = target.localPosition.x;

                // duration of Direct BlendTree is sum of 'weight * length'
                var expectedDuration = 1f * 1f + otherWeight * otherLength;
                var expected = Mathf.Repeat(elapsed / expectedDuration, 1f);
                // the value if each child were played with their own length
                var ifIndependent = elapsed / 1f;

                var message = $"otherLength={otherLength}, otherWeight={otherWeight}, otherIsConstant={otherIsConstant}: " +
                              $"actual={actual}, expected={expected}, ifIndependent={ifIndependent}, " +
                              $"stateInfo.length={stateInfo.length}, expectedDuration={expectedDuration}, " +
                              $"stateInfo.normalizedTime={stateInfo.normalizedTime}";
                Debug.Log($"DirectBlendTreeChildrenShareNormalizedTime: {message}");

                if (Mathf.Abs(actual - expected) > Tolerance
                    || Mathf.Abs(stateInfo.length - expectedDuration) > Tolerance)
                    failures.Add(message);

                Object.Destroy(animator.gameObject);
            }

            Assert.That(failures, Is.Empty, "Direct BlendTree children do not share normalized time with duration of sum of weight * length");

            yield return new ExitPlayMode();
        }

        /// <summary>
        /// Checks how speed and cycle offset of the state work when motion time is active.
        ///
        /// When motion time is active, speed (and speed parameter) is ignored but
        /// cycle offset (and cycle offset parameter) is added to the motion time.
        /// EntryExitToBlendTree creates new state with motion time,
        /// so it can ignore speed but have to take care of cycle offset.
        /// </summary>
        [UnityTest]
        public IEnumerator SpeedAndCycleOffsetWithMotionTime()
        {
            yield return new EnterPlayMode();

            const float motionTime = 0.4f;
            const float cycleOffset = 0.25f;
            var failures = new List<string>();

            foreach (var (name, expected) in new[]
                     {
                         ("baseline", motionTime),
                         // speed is ignored when motion time is active
                         ("speed=3", motionTime),
                         ("speed=0", motionTime),
                         ("speedParameter=3", motionTime),
                         // cycle offset is applied even if motion time is active
                         ("cycleOffset", motionTime + cycleOffset),
                         ("cycleOffsetParameter", motionTime + cycleOffset),
                     })
            {
                var controller = CreateController(LinearClip(TargetName, 1f), out var state,
                    ("MotionTime", motionTime), ("Three", 3f), ("CycleOffset", cycleOffset));
                state.timeParameterActive = true;
                state.timeParameter = "MotionTime";

                switch (name)
                {
                    case "baseline":
                        break;
                    case "speed=3":
                        state.speed = 3;
                        break;
                    case "speed=0":
                        state.speed = 0;
                        break;
                    case "speedParameter=3":
                        state.speedParameterActive = true;
                        state.speedParameter = "Three";
                        break;
                    case "cycleOffset":
                        state.cycleOffset = cycleOffset;
                        break;
                    case "cycleOffsetParameter":
                        state.cycleOffsetParameterActive = true;
                        state.cycleOffsetParameter = "CycleOffset";
                        break;
                }

                var (animator, target) = CreateAnimator(controller);

                Step(animator, 0.5f);

                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                var actual = target.localPosition.x;

                var message = $"{name}: actual={actual}, expected={expected}, " +
                              $"stateInfo.normalizedTime={stateInfo.normalizedTime}, stateInfo.speed={stateInfo.speed}";
                Debug.Log($"SpeedAndCycleOffsetWithMotionTime: {message}");

                if (Mathf.Abs(actual - expected) > Tolerance)
                    failures.Add(message);

                Object.Destroy(animator.gameObject);
            }

            Assert.That(failures, Is.Empty, "speed / cycle offset with motion time behaves differently from expected");

            yield return new ExitPlayMode();
        }

        /// <summary>
        /// Checks cycle offset of the state works with BlendTree in the same way as with single clip
        /// when motion time is active.
        ///
        /// EntryExitToBlendTree replaces states with motion time with single state with 1D BlendTree,
        /// and copies cycle offset of the original states to the new state.
        /// Since only one child of the BlendTree has weight, the cycle offset of the new state must work
        /// as if the child with weight is the motion of the state, regardless of the length of other children.
        /// </summary>
        [UnityTest]
        public IEnumerator CycleOffsetWithMotionTimeAndBlendTree()
        {
            yield return new EnterPlayMode();

            const float motionTime = 0.4f;
            const float cycleOffset = 0.25f;
            var failures = new List<string>();

            foreach (var (name, blend, expected) in new[]
                     {
                         // blend = 0: 1 second clip for Target has weight, blend = 1: 4 seconds clip for Other has weight
                         ("baseline", 0f, motionTime),
                         ("baseline", 1f, motionTime),
                         ("cycleOffset", 0f, motionTime + cycleOffset),
                         ("cycleOffset", 1f, motionTime + cycleOffset),
                         ("cycleOffsetParameter", 0f, motionTime + cycleOffset),
                         ("cycleOffsetParameter", 1f, motionTime + cycleOffset),
                     })
            {
                var blendTree = new BlendTree
                {
                    name = "1D",
                    blendType = BlendTreeType.Simple1D,
                    blendParameter = "Blend",
                    useAutomaticThresholds = false,
                    children = new[]
                    {
                        new ChildMotion { motion = LinearClip(TargetName, 1f), threshold = 0, timeScale = 1 },
                        new ChildMotion { motion = LinearClip(OtherName, 4f), threshold = 1, timeScale = 1 },
                    },
                };

                var controller = CreateController(blendTree, out var state,
                    ("MotionTime", motionTime), ("Blend", blend), ("CycleOffset", cycleOffset));
                state.timeParameterActive = true;
                state.timeParameter = "MotionTime";

                switch (name)
                {
                    case "baseline":
                        break;
                    case "cycleOffset":
                        state.cycleOffset = cycleOffset;
                        break;
                    case "cycleOffsetParameter":
                        state.cycleOffsetParameterActive = true;
                        state.cycleOffsetParameter = "CycleOffset";
                        break;
                }

                var (animator, target) = CreateAnimator(controller);
                var other = animator.transform.Find(OtherName);

                Step(animator, 0.5f);

                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                // the transform animated by the child with weight
                var actual = blend == 0f ? target.localPosition.x : other.localPosition.x;

                var message = $"{name}, blend={blend}: actual={actual}, expected={expected}, " +
                              $"stateInfo.normalizedTime={stateInfo.normalizedTime}, stateInfo.length={stateInfo.length}";
                Debug.Log($"CycleOffsetWithMotionTimeAndBlendTree: {message}");

                if (Mathf.Abs(actual - expected) > Tolerance)
                    failures.Add(message);

                Object.Destroy(animator.gameObject);
            }

            Assert.That(failures, Is.Empty, "cycle offset with motion time and BlendTree behaves differently from expected");

            yield return new ExitPlayMode();
        }

        #region Utilities

        /// <summary>
        /// Advance the animator for <paramref name="elapsed"/> seconds in total.
        /// We update the animator manually without yielding so the result does not depend on frame time.
        /// </summary>
        private static void Step(Animator animator, float elapsed)
        {
            const int steps = 5;
            animator.Update(0f);
            for (var i = 0; i < steps; i++)
                animator.Update(elapsed / steps);
        }

        private static (Animator animator, Transform target) CreateAnimator(AnimatorController controller)
        {
            var root = new GameObject("BlendTreeTimeBehaviorTester");
            var target = new GameObject(TargetName).transform;
            target.SetParent(root.transform, false);
            var other = new GameObject(OtherName).transform;
            other.SetParent(root.transform, false);

            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            return (animator, target);
        }

        private static AnimatorController CreateController(Motion motion, out AnimatorState state,
            params (string name, float value)[] parameters)
        {
            var controller = new AnimatorController { name = "BlendTreeTimeBehavior" };
            foreach (var (name, value) in parameters)
            {
                controller.AddParameter(new AnimatorControllerParameter
                {
                    name = name,
                    type = AnimatorControllerParameterType.Float,
                    defaultFloat = value,
                });
            }

            var layer = new AnimatorControllerLayer
            {
                name = "Layer",
                defaultWeight = 1.0f,
                stateMachine = new AnimatorStateMachine
                {
                    name = "Layer",
                    hideFlags = HideFlags.HideInHierarchy,
                },
            };

            state = layer.stateMachine.AddState("State");
            state.motion = motion;
            state.writeDefaultValues = true;
            layer.stateMachine.defaultState = state;

            controller.AddLayer(layer);
            return controller;
        }

        /// <summary>Looping clip which moves localPosition.x of the object from 0 to 1 linearly</summary>
        private static AnimationClip LinearClip(string path, float length) =>
            CreateClip(path, AnimationCurve.Linear(0, 0, length, 1));

        /// <summary>Looping clip which keeps localPosition.x of the object to 1</summary>
        private static AnimationClip ConstantClip(string path, float length) =>
            CreateClip(path, AnimationCurve.Constant(0, length, 1));

        private static AnimationClip CreateClip(string path, AnimationCurve curve)
        {
            var clip = new AnimationClip { name = $"{path} clip" };
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"), curve);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        #endregion
    }
}
