using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.API;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIInternal.Externals
{
    [ComponentInformationWithGUID("f9ac8d30c6a0d9642a11e5be4c440740", 11500000)]
    internal class DynamicBoneInformation : ComponentInformation<Component>, IExternalMarker
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
            collector.MarkHeavyBehaviour();

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
    internal class DynamicBoneColliderInformation : ComponentInformation<Component>, IExternalMarker
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
        }
    }
}
