using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer
{
    partial class Utils
    {
        public static Transform GetTarget(this VRCPhysBoneBase physBoneBase) =>
            physBoneBase.rootTransform ? physBoneBase.rootTransform : physBoneBase.transform;

        public static IEnumerable<Transform> GetAffectedTransforms(this VRCPhysBoneBase physBoneBase)
        {
            var ignores = new HashSet<Transform>(physBoneBase.ignoreTransforms);
            var queue = new Queue<Transform>();
            queue.Enqueue(physBoneBase.GetTarget());

            while (queue.Count != 0)
            {
                var transform = queue.Dequeue();
                yield return transform;

                foreach (var child in transform.DirectChildrenEnumerable())
                    if (!ignores.Contains(child))
                        queue.Enqueue(child);
            }
        }

        // https://creators.vrchat.com/avatars/#proxy-animations
        public static bool IsProxy(this AnimationClip clip) => clip.name.StartsWith("proxy_", StringComparison.Ordinal);
    }
}