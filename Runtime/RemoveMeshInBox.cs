using System;
using Unity.Burst;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Remove Mesh in Box")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/remove-mesh-in-box/")]
    internal class RemoveMeshInBox : EditSkinnedMeshComponent
    {
        public BoundingBox[] boxes = Array.Empty<BoundingBox>();

        private void Reset()
        {
            boxes = new[] { BoundingBox.Default };
        }

        [Serializable]
        public struct BoundingBox
        {
            [AAOLocalized("RemoveMeshInBox:BoundingBox:prop:center")]
            public Vector3 center;
            [AAOLocalized("RemoveMeshInBox:BoundingBox:prop:size")]
            public Vector3 size;
            [AAOLocalized("RemoveMeshInBox:BoundingBox:prop:rotation")]
            public Quaternion rotation;

            public static BoundingBox Default = new BoundingBox
            {
                center = Vector3.zero,
                size = new Vector3(1, 1, 1),
                rotation = Quaternion.identity,
            };

            [BurstCompile]
            public bool ContainsVertex(Vector3 point)
            {
                var positionInBox = Quaternion.Inverse(rotation) * (point - center);
                var halfSize = size / 2;
                return (-halfSize.x <= positionInBox.x && positionInBox.x <= halfSize.x)
                       && (-halfSize.y <= positionInBox.y && positionInBox.y <= halfSize.y)
                       && (-halfSize.z <= positionInBox.z && positionInBox.z <= halfSize.z);
            }
        }

        [Serializable]
        public class BoundingBoxList
        {
            public Container[] firstLayer = Array.Empty<Container>();
            public Layer[] prefabLayers = Array.Empty<Layer>();

            [Serializable]
            public class Layer
            {
                public Container[] elements = Array.Empty<Container>();
            }

            [Serializable]
            public class Container
            {
                public BoundingBox value;
                public bool removed;
            }
        }
    }
}
