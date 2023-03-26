using System;
using Anatawa12.AvatarOptimizer.PrefabSafeList;
using CustomLocalization4EditorExtension;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Remove Mesh in Box")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    internal class RemoveMeshInBox : EditSkinnedMeshComponent
    {
        [Obsolete("legacy v1", true)]
        public BoundingBox[] boxes = Array.Empty<BoundingBox>();

        public BoundingBoxList boxList;

        public RemoveMeshInBox()
        {
            boxList = new BoundingBoxList(this);
        }

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
        public class BoundingBoxList : PrefabSafeList<BoundingBox, BoundingBoxList.Layer, BoundingBoxList.Container>
        {
            [Serializable]
            public class Layer : PrefabLayer<BoundingBox, Container> {}
            [Serializable]
            public class Container : ValueContainer<BoundingBox> {}

            public BoundingBoxList(Object outerObject) : base(outerObject)
            {
            }
        }
    }
}
