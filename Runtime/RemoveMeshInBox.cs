using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Remove Mesh in Box")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    internal class RemoveMeshInBox : EditSkinnedMeshComponent
    {
        public BoundingBox[] boxes = Array.Empty<BoundingBox>();

        [Serializable]
        public class BoundingBox
        {
            public Vector3 center;
            public Vector3 size = new Vector3(1, 1, 1);
            public Quaternion rotation = Quaternion.identity;

            public bool ContainsVertex(Vector3 point)
            {
                var positionInBox = Quaternion.Inverse(rotation) * (point - center);
                var halfSize = size / 2;
                return (-halfSize.x <= positionInBox.x && positionInBox.x <= halfSize.x)
                       && (-halfSize.y <= positionInBox.y && positionInBox.y <= halfSize.y)
                       && (-halfSize.z <= positionInBox.z && positionInBox.z <= halfSize.z);
            }
        }
    }
}
