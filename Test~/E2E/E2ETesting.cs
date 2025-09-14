using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.E2E
{
    // We should order test cases by issue / pr number.
    // If the test has issue, use issue number and add comment with link to the issue.
    // If the test does not have issue, use PR number that fixes the error and add comment with link to the PR.
    public class E2ETesting
    {
        #region Issue or Regression Testing

        [Test]
        public void PR1522_GCComponentDoesNotRemoveDisabledComponents()
        {
            // Create an avatar
            var avatar = TestUtils.NewAvatar();
            TestUtils.SetFxLayer(avatar, new AnimatorController());
            var rendererGO = new GameObject("Renderer");
            rendererGO.transform.SetParent(avatar.transform, false);
            var smr = rendererGO.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = TestUtils.NewCubeMesh();
            smr.enabled = false;
            avatar.AddComponent<TraceAndOptimize>();

            // Run NDMF
            AvatarProcessor.ProcessAvatar(avatar);

            // Run Checks
            Assert.That(smr == null, "SkinnedMeshRenderer should be removed by GCComponent");
            Assert.That(rendererGO == null, "SkinnedMeshRenderer should be removed by GCComponent");
        }

        #endregion
    }
}