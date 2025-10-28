using System.Collections;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.Test.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    public class UnityBehaviorChecks : AnimatorOptimizerTestBase
    {
        /// <summary>This test tests the diamond-style entry-exit animator controller's first frame behavior.</summary>
        ///
        /// <remarks>
        /// Even when the default state is set to "On State", before applying the animation (enabling the component),
        /// transition is computed and the "Off State" is entered first (therefore the component kept disabled).<br/>
        /// EntryExit to BlendTree optimization depends on this behavior, so it is important to ensure that.
        ///
        /// <code>
        ///                    +--------------------+
        ///                    | On State (default) |
        ///                    +--------------------+
        ///                 /                          \      
        ///   +----------+                                +----------+
        ///   |  Entry   |                                |   Exit   |
        ///   +----------+                                +----------+
        ///                 \                          /        
        ///                    +--------------------+ 
        ///                    |      Off State     |
        ///                    +--------------------+ 
        /// </code>
        /// </remarks>
        [UnityTest]
        public IEnumerator DiamondStyleEntryExitFirstFrameCheck()
        {
            yield return new EnterPlayMode(expectDomainReload: false);

            var tester = LoadTester();
            var controller = LoadCloneAnimatorController("DiamondStyleEntryExit");

            tester.Animator.runtimeAnimatorController = controller;

            // Pre-check the initial state of the components
            Assert.That(tester.EnabledByDefault.onEnableCalled, Is.EqualTo(1));
            Assert.That(tester.EnabledByDefault.onUpdateCalled, Is.Zero);
            Assert.That(tester.EnabledByDefault.onDisableCalled, Is.Zero);
            Assert.That(tester.EnabledByDefault.gameObject.activeInHierarchy, Is.True);
            Assert.That(tester.DisabledByDefault.onEnableCalled, Is.Zero);
            Assert.That(tester.DisabledByDefault.onUpdateCalled, Is.Zero);
            Assert.That(tester.DisabledByDefault.onDisableCalled, Is.Zero);
            Assert.That(tester.DisabledByDefault.gameObject.activeInHierarchy, Is.False);

            // the animator is on the default state, which is "On State"
            Assert.That(TestUtils.GetStateName(tester.Animator.GetCurrentAnimatorStateInfo(0), controller), Is.EqualTo("On State"));

            // Run one frame
            yield return null;

            // After one frame, the animator should be on the "Off State"
            Assert.That(TestUtils.GetStateName(tester.Animator.GetCurrentAnimatorStateInfo(0), controller), Is.EqualTo("Off State"));

            // The EnabledByDefault component should have been disabled, and the DisabledByDefault component should have never been enabled.
            Assert.That(tester.EnabledByDefault.onEnableCalled, Is.EqualTo(1));
            Assert.That(tester.EnabledByDefault.onUpdateCalled, Is.Zero.Or.EqualTo(1));
            Assert.That(tester.EnabledByDefault.onDisableCalled, Is.EqualTo(1));
            Assert.That(tester.EnabledByDefault.gameObject.activeInHierarchy, Is.False);
            Assert.That(tester.DisabledByDefault.onEnableCalled, Is.Zero);
            Assert.That(tester.DisabledByDefault.onUpdateCalled, Is.Zero);
            Assert.That(tester.DisabledByDefault.onDisableCalled, Is.Zero);
            Assert.That(tester.DisabledByDefault.gameObject.activeInHierarchy, Is.False);

            yield return new ExitPlayMode();
        }
        
        /// <summary>This test tests the linear entry-exit animator controller's first frame behavior.</summary>
        ///
        /// <remarks>
        /// The state machine is looks to enter "On State" first, but the transition is computed before applying the animation (enabling the component),
        /// therefore the "Off State" is animated first (and the component kept disabled).<br/>
        /// AAO's EntryExit to BlendTree optimization depends on this behavior, so it is important to ensure that.
        ///
        /// <code>
        ///   +----------+       +--------------------+        +--------------------+        +----------+
        ///   |  Entry   | ===>  | On State (default) |  ===>  |      Off State     |  ===>  |   Exit   |
        ///   +----------+       +--------------------+        +--------------------+        +----------+
        /// </code>
        /// </remarks>
        [UnityTest]
        public IEnumerator LinearStyleEntryExitFirstFrameCheck()
        {
            yield return new EnterPlayMode(expectDomainReload: false);

            var tester = LoadTester();
            var controller = LoadCloneAnimatorController("LinearStyleEntryExit");

            tester.Animator.runtimeAnimatorController = controller;

            // Pre-check the initial state of the components
            Assert.That(tester.EnabledByDefault.onEnableCalled, Is.EqualTo(1));
            Assert.That(tester.EnabledByDefault.onUpdateCalled, Is.Zero);
            Assert.That(tester.EnabledByDefault.onDisableCalled, Is.Zero);
            Assert.That(tester.EnabledByDefault.gameObject.activeInHierarchy, Is.True);
            Assert.That(tester.DisabledByDefault.onEnableCalled, Is.Zero);
            Assert.That(tester.DisabledByDefault.onUpdateCalled, Is.Zero);
            Assert.That(tester.DisabledByDefault.onDisableCalled, Is.Zero);
            Assert.That(tester.DisabledByDefault.gameObject.activeInHierarchy, Is.False);

            // the animator is on the default state, which is "On State"
            Assert.That(TestUtils.GetStateName(tester.Animator.GetCurrentAnimatorStateInfo(0), controller), Is.EqualTo("On State"));

            // Run one frame
            yield return null;

            // After one frame, the animator should be on the "Off State"
            Assert.That(TestUtils.GetStateName(tester.Animator.GetCurrentAnimatorStateInfo(0), controller), Is.EqualTo("Off State"));

            // The EnabledByDefault component should have been disabled, and the DisabledByDefault component should have never been enabled.
            Assert.That(tester.EnabledByDefault.onEnableCalled, Is.EqualTo(1));
            Assert.That(tester.EnabledByDefault.onUpdateCalled, Is.Zero.Or.EqualTo(1));
            Assert.That(tester.EnabledByDefault.onDisableCalled, Is.EqualTo(1));
            Assert.That(tester.EnabledByDefault.gameObject.activeInHierarchy, Is.False);
            Assert.That(tester.DisabledByDefault.onEnableCalled, Is.Zero);
            Assert.That(tester.DisabledByDefault.onUpdateCalled, Is.Zero);
            Assert.That(tester.DisabledByDefault.onDisableCalled, Is.Zero);
            Assert.That(tester.DisabledByDefault.gameObject.activeInHierarchy, Is.False);

            yield return new ExitPlayMode();
        }

        #region Utilities

        struct TesterObject
        {
            public GameObject RootGameObject;
            public Animator Animator;
            public CheckEnabled EnabledByDefault;
            public CheckEnabled DisabledByDefault;
        }

        private TesterObject LoadTester()
        {
            var rootGameObject = LoadPrefab("UnityBehaviorTester");
            var animator = rootGameObject.GetComponent<Animator>();
            var enabledByDefault = rootGameObject.transform.Find("EnabledByDefault").GetComponent<CheckEnabled>();
            var disabledByDefault = rootGameObject.transform.Find("DisabledByDefault").GetComponent<CheckEnabled>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(enabledByDefault, Is.Not.Null);
            Assert.That(disabledByDefault, Is.Not.Null);
            return new TesterObject
            {
                Animator = animator,
                RootGameObject = rootGameObject,
                EnabledByDefault = enabledByDefault,
                DisabledByDefault = disabledByDefault
            };
        }

        #endregion
    }
}