using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.API;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIInternal.Externals
{
    #region Dynamic Bones

    [ComponentInformationWithGUID("f9ac8d30c6a0d9642a11e5be4c440740", 11500000)]
    internal class DynamicBoneInformation : ComponentInformation<Component>
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
            collector.MarkBehaviour();

            foreach (var transform in GetAffectedTransforms(component))
            {
                collector.AddDependency(transform, component)
                    .EvenIfDependantDisabled()
                    .OnlyIfTargetCanBeEnable();
                collector.AddDependency(transform);
            }

            foreach (var collider in (IReadOnlyList<MonoBehaviour>)((dynamic)component).m_Colliders)
            {
                // DynamicBone ignores enabled/disabled of Collider Component AFAIK
                collector.AddDependency(collider);
            }
        }

        protected override void CollectMutations(Component component, ComponentMutationsCollector collector)
        {
            foreach (var transform in GetAffectedTransforms(component))
                collector.TransformRotation(transform);
        }

        private static IEnumerable<Transform> GetAffectedTransforms(dynamic dynamicBone)
        {
            var ignores = new HashSet<Transform>(dynamicBone.m_Exclusions);
            var queue = new Queue<Transform>();
            Transform root = dynamicBone.m_Root;
            queue.Enqueue(root ? root : (Transform)dynamicBone.transform);

            while (queue.Count != 0)
            {
                var transform = queue.Dequeue();
                yield return transform;

                foreach (var child in transform.DirectChildrenEnumerable())
                    if (!ignores.Contains(child))
                        queue.Enqueue(child);
            }
        }
    }

    [ComponentInformationWithGUID("baedd976e12657241bf7ff2d1c685342", 11500000)]
    internal class DynamicBoneColliderInformation : ComponentInformation<Component>
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
        }
    }

    #endregion

    #region Satania's KiseteneEx Components

    [ComponentInformationWithGUID("e78466b6bcd24e5409dca557eb81d45b", 11500000)] // KiseteneComponent
    [ComponentInformationWithGUID("7f9c3fe1cfb9d1843a9dc7da26352ce2", 11500000)] // FlyAvatarSetupTool
    [ComponentInformationWithGUID("95f6e1368d803614f8a351322ab09bac", 11500000)] // BlendShapeOverrider
    internal class SataniaKiseteneExComponents : ComponentInformation<Component>
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
        }
    }

    #endregion

    
    #region VRCQuestTools

    [ComponentInformationWithGUID("f055e14e1beba894ea68aedffde8ada6", 11500000)] // VertexColorRemover
    internal class VRCQuestToolsComponents : ComponentInformation<Component>
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
        }
    }

    #endregion
}