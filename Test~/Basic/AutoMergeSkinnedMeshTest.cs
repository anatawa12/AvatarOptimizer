using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.ndmf;
using Anatawa12.AvatarOptimizer.Processors;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using nadena.dev.ndmf;
using NUnit.Framework;
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
            var (indexMap, meshes) = AutoMergeSkinnedMesh.CreateSubMeshesMergePreserveOrder(new[]
            {
                MakeMeshInfo2(material0, material1),
                MakeMeshInfo2(material0, material2),
                MakeMeshInfo2(material1, material2),
                MakeMeshInfo2(material0, material4),
                MakeMeshInfo2(material3, material4),
            });

            Assert.That(indexMap, Is.EquivalentTo(new []
            {
                new []{0, 1},
                new []{0, 2},
                new []{1, 2},
                new []{0, 4},
                new []{3, 4},
            }));

            Assert.That(meshes, Is.EquivalentTo(new []
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
        
        [Test]
        public void CategorizeMeshesForMerge_SplitByActivenessAnimation()
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
            var buildContext = new BuildContext(avatar, null);
            buildContext.GetState<AAOEnabled>().Enabled = true;
            buildContext.ActivateExtensionContext<ObjectMappingContext>();
            buildContext.ActivateExtensionContext<MeshInfo2Context>();
            ParseAnimator.RunPass(buildContext);

            // do process
            var categorization = AutoMergeSkinnedMesh.CategoryMeshesForMerge(buildContext, new List<MeshInfo2>()
            {
                buildContext.GetMeshInfoFor(renderer0), buildContext.GetMeshInfoFor(renderer1),
                buildContext.GetMeshInfoFor(renderer2), buildContext.GetMeshInfoFor(renderer3),
            });

            Assert.That(categorization.Values, Is.EquivalentTo(new []
            {
                new []{buildContext.GetMeshInfoFor(renderer0), buildContext.GetMeshInfoFor(renderer1)},
                new []{buildContext.GetMeshInfoFor(renderer2), buildContext.GetMeshInfoFor(renderer3)},
            }));
        }
    }
}