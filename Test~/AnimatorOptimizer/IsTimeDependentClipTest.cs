using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    public class IsTimeDependentClipTest
    {
        private static readonly EditorCurveBinding FloatBinding =
            EditorCurveBinding.FloatCurve("Body", typeof(GameObject), "m_IsActive");

        private static readonly EditorCurveBinding ObjectBinding =
            EditorCurveBinding.PPtrCurve("Body", typeof(MeshFilter), "m_Mesh");

        private static bool IsTimeDependent(AnimationClip clip) =>
            new AnimatorOptimizerState().IsTimeDependentClip(clip);

        private static AnimationClip FloatClip(AnimationCurve curve)
        {
            var clip = new AnimationClip();
            AnimationUtility.SetEditorCurve(clip, FloatBinding, curve);
            return clip;
        }

        private static AnimationClip ObjectClip(params Object[] values)
        {
            var keys = new ObjectReferenceKeyframe[values.Length];
            for (var i = 0; i < values.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / 60f, value = values[i] };

            var clip = new AnimationClip();
            AnimationUtility.SetObjectReferenceCurve(clip, ObjectBinding, keys);
            return clip;
        }

        [Test]
        public void EmptyClip() =>
            Assert.That(IsTimeDependent(new AnimationClip()), Is.False);

        [Test]
        public void FloatSingleKeyframe() =>
            Assert.That(IsTimeDependent(FloatClip(new AnimationCurve(new Keyframe(0, 1)))), Is.False);

        [Test]
        public void FloatConstant() =>
            Assert.That(IsTimeDependent(FloatClip(AnimationCurve.Constant(0, 1, 1))), Is.False);

        [Test]
        public void FloatChanging() =>
            Assert.That(IsTimeDependent(FloatClip(AnimationCurve.Linear(0, 0, 1, 1))), Is.True);

        [Test]
        public void ObjectSingleKeyframe() =>
            Assert.That(IsTimeDependent(ObjectClip(new Mesh())), Is.False);

        [Test]
        public void ObjectConstant()
        {
            var mesh = new Mesh();
            Assert.That(IsTimeDependent(ObjectClip(mesh, mesh)), Is.False);
        }

        // material / sprite swap animation is time dependent
        [Test]
        public void ObjectChanging() =>
            Assert.That(IsTimeDependent(ObjectClip(new Mesh(), new Mesh())), Is.True);

        [Test]
        public void ObjectChangingAndBack()
        {
            var mesh = new Mesh();
            Assert.That(IsTimeDependent(ObjectClip(mesh, new Mesh(), mesh)), Is.True);
        }

        [Test]
        public void FloatConstantWithObjectChanging()
        {
            var clip = ObjectClip(new Mesh(), new Mesh());
            AnimationUtility.SetEditorCurve(clip, FloatBinding, AnimationCurve.Constant(0, 1, 1));
            Assert.That(IsTimeDependent(clip), Is.True);
        }
    }
}
