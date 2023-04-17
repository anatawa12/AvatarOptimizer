using System;
using CustomLocalization4EditorExtension;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Remove Mesh in Box")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    internal class RemoveMeshInBox : EditSkinnedMeshComponent
    {
        public BoundingBox[] boxes = Array.Empty<BoundingBox>();

        [Obsolete("legacy v2", true)]
        public BoundingBoxList boxList = new BoundingBoxList();

        [Serializable]
        public class BoundingBox
        {
            [CL4EELocalized("RemoveMeshInBox:BoundingBox:prop:center")]
            public Vector3 center;
            [CL4EELocalized("RemoveMeshInBox:BoundingBox:prop:size")]
            public Vector3 size = new Vector3(1, 1, 1);
            [CL4EELocalized("RemoveMeshInBox:BoundingBox:prop:rotation")]
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

        [Serializable]
        public class BoundingBoxList
        {
            public Container[] firstLayer = Array.Empty<Container>();
            public Layer[] prefabLayers = Array.Empty<Layer>();

            [Serializable]
            public class Layer : PrefabLayer<BoundingBox, Container>
            {
                public Container[] elements = Array.Empty<Container>();
            }

            [Serializable]
            public class Container : ValueContainer<BoundingBox>
            {
                public BoundingBox value;
                public bool removed;
            }
        }
    }
}
