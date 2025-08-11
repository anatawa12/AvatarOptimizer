using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using Anatawa12.AvatarOptimizer.ndmf;
using Anatawa12.AvatarOptimizer.Processors;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static Anatawa12.AvatarOptimizer.Test.AnimatorControllerGeneratorStatics;

namespace Anatawa12.AvatarOptimizer.Test
{
    /// <summary>
    /// Currently this class only contains unit tests for <see cref="AutoMergeSkinnedMesh"/>.
    /// </summary>
    public class AutoMergeSkinnedMeshTest
    {
        [Test]
        public void CreateSubMeshesMergePreserveOrder()
        {
            var shader = Shader.Find("Standard");
            var material0 = new Material(shader);
            var material1 = new Material(shader);
            var material2 = new Material(shader);
            var material3 = new Material(shader);
            var material4 = new Material(shader);
            using var meshInfo2s = new[]
            {
                MakeMeshInfo2(material0, material1),
                MakeMeshInfo2(material0, material2),
                MakeMeshInfo2(material1, material2),
                MakeMeshInfo2(material0, material4),
                MakeMeshInfo2(material3, material4),
            }.ToDisposableList();
            var (indexMap, meshes) = AutoMergeSkinnedMesh.CreateSubMeshesMergePreserveOrder(meshInfo2s.ToArray());

            Assert.That(indexMap, Is.EquivalentTo(new[]
            {
                new[] { 0, 1 },
                new[] { 0, 2 },
                new[] { 1, 2 },
                new[] { 0, 4 },
                new[] { 3, 4 },
            }));

            Assert.That(meshes, Is.EquivalentTo(new[]
            {
                (MeshTopology.Triangles, material0),
                (MeshTopology.Triangles, material1),
                (MeshTopology.Triangles, material2),
                (MeshTopology.Triangles, material3),
                (MeshTopology.Triangles, material4),
            }));
        }

        MeshInfo2 MakeMeshInfo2(params Material[] material)
        {
            var gameObject = new GameObject();
            var newMesh = new Mesh();
            newMesh.subMeshCount = material.Length;
            var renderer = gameObject.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMaterials = material;
            renderer.sharedMesh = newMesh;
            return new MeshInfo2(renderer);
        }

        private BuildContext PreprocessAvatar(GameObject avatar)
        {
            var buildContext = new BuildContext(avatar, null);
            buildContext.GetState<AAOEnabled>().Enabled = true;
            buildContext.GetState<TraceAndOptimizeState>().MmdWorldCompatibility = false;
            buildContext.ActivateExtensionContext<ObjectMappingContext>();
            buildContext.ActivateExtensionContext<MeshInfo2Context>();
            ParseAnimator.RunPass(buildContext);

            return buildContext;
        }

        [Test]
        public void CategorizeMeshesForMerge_Split_DifferentActivenessAnimation()
        {
            // initialize test avatar
            var avatar = TestUtils.NewAvatar();
            avatar.AddComponent<TraceAndOptimize>();
            var renderer0GameObject = Utils.NewGameObject("Renderer0", avatar.transform);
            var renderer1GameObject = Utils.NewGameObject("Renderer1", avatar.transform);
            var renderer2GameObject = Utils.NewGameObject("Renderer2", avatar.transform);
            var renderer3GameObject = Utils.NewGameObject("Renderer3", avatar.transform);
            var renderer0 = renderer0GameObject.AddComponent<SkinnedMeshRenderer>();
            var renderer1 = renderer1GameObject.AddComponent<SkinnedMeshRenderer>();
            var renderer2 = renderer2GameObject.AddComponent<SkinnedMeshRenderer>();
            var renderer3 = renderer3GameObject.AddComponent<SkinnedMeshRenderer>();

            var rootBone = Utils.NewGameObject("RootBone", avatar.transform);
            renderer0.rootBone = rootBone.transform;
            renderer1.rootBone = rootBone.transform;
            renderer2.rootBone = rootBone.transform;
            renderer3.rootBone = rootBone.transform;

            TestUtils.SetFxLayer(avatar, BuildAnimatorController("")
                .AddLayer("Base", sm =>
                {
                    sm.NewClipState("State0", clip => clip
                        .AddPropertyBinding("Renderer0", typeof(GameObject), "m_IsActive",
                            new Keyframe(0, 0), new Keyframe(1, 1))
                        .AddPropertyBinding("Renderer1", typeof(GameObject), "m_IsActive",
                            new Keyframe(0, 0), new Keyframe(1, 1)));
                })
                .Build());

            // preprocess
            var buildContext = PreprocessAvatar(avatar);

            // do process
            AutoMergeSkinnedMesh.CategoryMeshesForMerge(buildContext, new List<MeshInfo2>()
            {
                buildContext.GetMeshInfoFor(renderer0), buildContext.GetMeshInfoFor(renderer1),
                buildContext.GetMeshInfoFor(renderer2), buildContext.GetMeshInfoFor(renderer3),
            }, out var categorization);

            Assert.That(categorization.Values, Is.EquivalentTo(new[]
            {
                new[] { buildContext.GetMeshInfoFor(renderer0), buildContext.GetMeshInfoFor(renderer1) },
                new[] { buildContext.GetMeshInfoFor(renderer2), buildContext.GetMeshInfoFor(renderer3) },
            }));
        }

        readonly struct TwoCubeAvatar
        {
            public readonly GameObject avatar;
            public readonly SkinnedMeshRenderer renderer0;
            public readonly SkinnedMeshRenderer renderer1;
            public readonly Material material0;
            public readonly Material material1;

            public TwoCubeAvatar(int dummy)
            {
                avatar = TestUtils.NewAvatar();
                avatar.AddComponent<TraceAndOptimize>();
                var renderer0GameObject = Utils.NewGameObject("Renderer0", avatar.transform);
                var renderer1GameObject = Utils.NewGameObject("Renderer1", avatar.transform);
                renderer0 = renderer0GameObject.AddComponent<SkinnedMeshRenderer>();
                renderer1 = renderer1GameObject.AddComponent<SkinnedMeshRenderer>();

                var rootBone = Utils.NewGameObject("RootBone", avatar.transform);
                renderer0.rootBone = rootBone.transform;
                renderer1.rootBone = rootBone.transform;

                renderer0.sharedMesh = GetCubeMesh();
                material0 = renderer0.material = new Material(Shader.Find("Standard"));
                renderer1.sharedMesh = GetCubeMesh();
                material1 = renderer1.material = new Material(Shader.Find("Standard"));
            }
        }

        [Test]
        public void CategorizeMeshesForMerge_Merge_AlwaysAppliedAnimationWithSameInitialValue()
        {
            // initialize test avatar
            var avatar = new TwoCubeAvatar(0);

            avatar.material0.SetFloat("_Metallic", 0);
            avatar.material1.SetFloat("_Metallic", 0);

            TestUtils.SetFxLayer(avatar.avatar, BuildAnimatorController("")
                .AddLayer("Base", sm =>
                {
                    sm.NewClipState("State0", clip => clip
                        .AddPropertyBinding("Renderer0", typeof(SkinnedMeshRenderer), "material._Metallic",
                            AnimationCurve.Linear(0, 0, 1, 1))
                        .AddPropertyBinding("Renderer1", typeof(SkinnedMeshRenderer), "material._Metallic",
                            AnimationCurve.Linear(0, 0, 1, 1)));
                })
                .Build());

            // preprocess
            var buildContext = PreprocessAvatar(avatar.avatar);
            Assert.That(
                buildContext.GetAnimationComponent(avatar.renderer0).GetFloatNode("material._Metallic").ApplyState,
                Is.EqualTo(ApplyState.Always));
            Assert.That(
                buildContext.GetAnimationComponent(avatar.renderer1).GetFloatNode("material._Metallic").ApplyState,
                Is.EqualTo(ApplyState.Always));

            // do process
            AutoMergeSkinnedMesh.CategoryMeshesForMerge(buildContext, new List<MeshInfo2>()
            {
                buildContext.GetMeshInfoFor(avatar.renderer0), buildContext.GetMeshInfoFor(avatar.renderer1),
            }, out var categorization);

            Assert.That(categorization.Values, Is.EquivalentTo(new[]
            {
                new[] { buildContext.GetMeshInfoFor(avatar.renderer0), buildContext.GetMeshInfoFor(avatar.renderer1) },
            }));
        }

        [Test]
        public void CategorizeMeshesForMerge_Merge_AlwaysAppliedAnimationWithDifferentInitialValue()
        {
            // initialize test avatar
            var avatar = new TwoCubeAvatar(0);

            avatar.material0.SetFloat("_Metallic", 0);
            avatar.material1.SetFloat("_Metallic", 1);

            TestUtils.SetFxLayer(avatar.avatar, BuildAnimatorController("")
                .AddLayer("Base", sm =>
                {
                    sm.NewClipState("State0", clip => clip
                        .AddPropertyBinding("Renderer0", typeof(SkinnedMeshRenderer), "material._Metallic",
                            AnimationCurve.Linear(0, 0, 1, 1))
                        .AddPropertyBinding("Renderer1", typeof(SkinnedMeshRenderer), "material._Metallic",
                            AnimationCurve.Linear(0, 0, 1, 1)));
                })
                .Build());

            // preprocess
            var buildContext = PreprocessAvatar(avatar.avatar);
            Assert.That(
                buildContext.GetAnimationComponent(avatar.renderer0).GetFloatNode("material._Metallic").ApplyState,
                Is.EqualTo(ApplyState.Always));
            Assert.That(
                buildContext.GetAnimationComponent(avatar.renderer1).GetFloatNode("material._Metallic").ApplyState,
                Is.EqualTo(ApplyState.Always));

            // do process
            AutoMergeSkinnedMesh.CategoryMeshesForMerge(buildContext, new List<MeshInfo2>()
            {
                buildContext.GetMeshInfoFor(avatar.renderer0), buildContext.GetMeshInfoFor(avatar.renderer1),
            }, out var categorization);

            Assert.That(categorization.Values, Is.EquivalentTo(new[]
            {
                new[] { buildContext.GetMeshInfoFor(avatar.renderer0), buildContext.GetMeshInfoFor(avatar.renderer1) },
            }));
        }

        [Test]
        public void CategorizeMeshesForMerge_Merge_PartiallyAppliedAnimationWithSameInitialValue()
        {
            // initialize test avatar
            var avatar = new TwoCubeAvatar(0);

            avatar.material0.SetFloat("_Metallic", 0);
            avatar.material1.SetFloat("_Metallic", 0);

            TestUtils.SetFxLayer(avatar.avatar, BuildAnimatorController("")
                .AddLayer("Base", sm => sm
                    .NewClipState("State0", clip => clip
                        .AddPropertyBinding("Renderer0", typeof(SkinnedMeshRenderer), "material._Metallic",
                            AnimationCurve.Linear(0, 0, 1, 1))
                        .AddPropertyBinding("Renderer1", typeof(SkinnedMeshRenderer), "material._Metallic",
                            AnimationCurve.Linear(0, 0, 1, 1)))
                    .NewClipState("State1", _ => { })) // empty clip: partially applied
                .Build());

            // preprocess
            var buildContext = PreprocessAvatar(avatar.avatar);
            Assert.That(
                buildContext.GetAnimationComponent(avatar.renderer0).GetFloatNode("material._Metallic").ApplyState,
                Is.EqualTo(ApplyState.Partially));
            Assert.That(
                buildContext.GetAnimationComponent(avatar.renderer1).GetFloatNode("material._Metallic").ApplyState,
                Is.EqualTo(ApplyState.Partially));

            // do process
            AutoMergeSkinnedMesh.CategoryMeshesForMerge(buildContext, new List<MeshInfo2>()
            {
                buildContext.GetMeshInfoFor(avatar.renderer0), buildContext.GetMeshInfoFor(avatar.renderer1),
            }, out var categorization);

            Assert.That(categorization.Values, Is.EquivalentTo(new[]
            {
                new[] { buildContext.GetMeshInfoFor(avatar.renderer0), buildContext.GetMeshInfoFor(avatar.renderer1) },
            }));
        }

        [Test]
        public void CategorizeMeshesForMerge_Split_PartiallyAppliedAnimationWithDifferentInitialValue()
        {
            // initialize test avatar
            var avatar = new TwoCubeAvatar(0);

            avatar.material0.SetFloat("_Metallic", 0);
            avatar.material1.SetFloat("_Metallic", 1);

            TestUtils.SetFxLayer(avatar.avatar, BuildAnimatorController("")
                .AddLayer("Base", sm => sm
                    .NewClipState("State0", clip => clip
                        .AddPropertyBinding("Renderer0", typeof(SkinnedMeshRenderer), "material._Metallic",
                            AnimationCurve.Linear(0, 0, 1, 1))
                        .AddPropertyBinding("Renderer1", typeof(SkinnedMeshRenderer), "material._Metallic",
                            AnimationCurve.Linear(0, 0, 1, 1)))
                    .NewClipState("State1", _ => { })) // empty clip: partially applied
                .Build());

            // preprocess
            var buildContext = PreprocessAvatar(avatar.avatar);
            Assert.That(
                buildContext.GetAnimationComponent(avatar.renderer0).GetFloatNode("material._Metallic").ApplyState,
                Is.EqualTo(ApplyState.Partially));
            Assert.That(
                buildContext.GetAnimationComponent(avatar.renderer1).GetFloatNode("material._Metallic").ApplyState,
                Is.EqualTo(ApplyState.Partially));

            // do process
            AutoMergeSkinnedMesh.CategoryMeshesForMerge(buildContext, new List<MeshInfo2>()
            {
                buildContext.GetMeshInfoFor(avatar.renderer0), buildContext.GetMeshInfoFor(avatar.renderer1),
            }, out var categorization);

            Assert.That(categorization.Values, Is.EquivalentTo(Enumerable.Empty<List<MeshInfo2>>()));
        }

        [Test]
        public void CategorizeMeshesForMerge_Merge_NeverAppliedAnimationWithSameInitialValue()
        {
            // initialize test avatar
            var avatar = new TwoCubeAvatar(0);

            avatar.material0.SetFloat("_Metallic", 0);
            avatar.material1.SetFloat("_Metallic", 0);

            TestUtils.SetFxLayer(avatar.avatar, BuildAnimatorController("")
                .AddLayer("Base", _ => { })
                .AddLayer("ZeroWeight", 0, sm => sm
                    .NewClipState("State0", clip => clip
                        .AddPropertyBinding("Renderer0", typeof(SkinnedMeshRenderer), "material._Metallic",
                            AnimationCurve.Linear(0, 0, 1, 1))
                        .AddPropertyBinding("Renderer1", typeof(SkinnedMeshRenderer), "material._Metallic",
                            AnimationCurve.Linear(0, 0, 1, 1))))
                .Build());

            // preprocess
            var buildContext = PreprocessAvatar(avatar.avatar);
            Assert.That(
                buildContext.GetAnimationComponent(avatar.renderer0).GetFloatNode("material._Metallic").ApplyState,
                Is.EqualTo(ApplyState.Never));
            Assert.That(
                buildContext.GetAnimationComponent(avatar.renderer1).GetFloatNode("material._Metallic").ApplyState,
                Is.EqualTo(ApplyState.Never));

            // do process
            AutoMergeSkinnedMesh.CategoryMeshesForMerge(buildContext, new List<MeshInfo2>()
            {
                buildContext.GetMeshInfoFor(avatar.renderer0), buildContext.GetMeshInfoFor(avatar.renderer1),
            }, out var categorization);

            Assert.That(categorization.Values, Is.EquivalentTo(new[]
            {
                new[] { buildContext.GetMeshInfoFor(avatar.renderer0), buildContext.GetMeshInfoFor(avatar.renderer1) },
            }));
        }

        [Test]
        public void CategorizeMeshesForMerge_Split_NeverAppliedAnimationWithDifferentInitialValue()
        {
            // initialize test avatar
            var avatar = new TwoCubeAvatar(0);

            avatar.material0.SetFloat("_Metallic", 0);
            avatar.material1.SetFloat("_Metallic", 1);

            TestUtils.SetFxLayer(avatar.avatar, BuildAnimatorController("")
                .AddLayer("Base", _ => { })
                .AddLayer("ZeroWeight", 0, sm => sm
                    .NewClipState("State0", clip => clip
                        .AddPropertyBinding("Renderer0", typeof(SkinnedMeshRenderer), "material._Metallic",
                            AnimationCurve.Linear(0, 0, 1, 1))
                        .AddPropertyBinding("Renderer1", typeof(SkinnedMeshRenderer), "material._Metallic",
                            AnimationCurve.Linear(0, 0, 1, 1))))
                .Build());

            // preprocess
            var buildContext = PreprocessAvatar(avatar.avatar);

            // check precondition
            Assert.That(
                buildContext.GetAnimationComponent(avatar.renderer0).GetFloatNode("material._Metallic").ApplyState,
                Is.EqualTo(ApplyState.Never));
            Assert.That(
                buildContext.GetAnimationComponent(avatar.renderer1).GetFloatNode("material._Metallic").ApplyState,
                Is.EqualTo(ApplyState.Never));

            // do process
            AutoMergeSkinnedMesh.CategoryMeshesForMerge(buildContext, new List<MeshInfo2>()
            {
                buildContext.GetMeshInfoFor(avatar.renderer0), buildContext.GetMeshInfoFor(avatar.renderer1),
            }, out var categorization);

            Assert.That(categorization.Values, Is.EquivalentTo(Enumerable.Empty<List<MeshInfo2>>()));
        }

        [Test]
        public void Issue1252NoCrashWithObjectReferenceCurve()
        {
            // initialize test avatar
            var avatar = new TwoCubeAvatar(0);

            TestUtils.SetFxLayer(avatar.avatar, BuildAnimatorController("")
                .AddLayer("Base", _ => { })
                .AddLayer("ZeroWeight", 0, sm => sm
                    .NewClipState("State0", clip => clip
                        .AddObjectReferenceBinding("Renderer0", typeof(SkinnedMeshRenderer),
                            "m_Materials.Array.data[0]",
                            (0, new Material(Shader.Find("Standard"))))))
                .Build());

            // preprocess
            var buildContext = PreprocessAvatar(avatar.avatar);

            // do process
            AutoMergeSkinnedMesh.CategoryMeshesForMerge(buildContext, new List<MeshInfo2>()
            {
                buildContext.GetMeshInfoFor(avatar.renderer0), buildContext.GetMeshInfoFor(avatar.renderer1),
            }, out _);

            // No crash
        }

        private static Mesh GetCubeMesh()
        {
            return AssetDatabase.LoadAllAssetsAtPath("Library/unity default resources")
                .OfType<Mesh>()
                .First(x => x.name == "Cube");
        }
    }
}
