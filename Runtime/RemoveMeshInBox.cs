using System;
using System.Linq;
using JetBrains.Annotations;
using Unity.Burst;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Remove Mesh in Box")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/remove-mesh-in-box/")]
    [PublicAPI]
    public sealed class RemoveMeshInBox : EditSkinnedMeshComponent
    {
        [SerializeField]
        internal BoundingBox[] boxes = Array.Empty<BoundingBox>();

        internal RemoveMeshInBox()
        {
        }

        private void Reset()
        {
            boxes = new[] { BoundingBox.Default };
        }

        [Serializable]
        [PublicAPI]
        public struct BoundingBox
        {
            [AAOLocalized("RemoveMeshInBox:BoundingBox:prop:center")]
            [SerializeField]
            internal Vector3 center;
            [AAOLocalized("RemoveMeshInBox:BoundingBox:prop:size")]
            [SerializeField]
            internal Vector3 size;
            [AAOLocalized("RemoveMeshInBox:BoundingBox:prop:rotation")]
            [SerializeField]
            internal Quaternion rotation;

            /// <summary>
            /// Gets or sets the center of the box in local space.
            /// </summary>
            [PublicAPI]
            public Vector3 Center
            {
                get => center;
                set => center = value;
            }

            /// <summary>
            /// Gets or sets the size of the box in local space.
            /// </summary>
            [PublicAPI]
            public Vector3 Size
            {
                get => size;
                set => size = value;
            }

            /// <summary>
            /// Gets or sets the rotation of the box in local space.
            /// </summary>
            [PublicAPI]
            public Quaternion Rotation
            {
                get => rotation;
                set => rotation = value;
            }

            internal static BoundingBox Default = new BoundingBox
            {
                center = Vector3.zero,
                size = new Vector3(1, 1, 1),
                rotation = Quaternion.identity,
            };

            [BurstCompile]
            internal bool ContainsVertex(Vector3 point)
            {
                var positionInBox = Quaternion.Inverse(rotation) * (point - center);
                var halfSize = size / 2;
                return (-halfSize.x <= positionInBox.x && positionInBox.x <= halfSize.x)
                       && (-halfSize.y <= positionInBox.y && positionInBox.y <= halfSize.y)
                       && (-halfSize.z <= positionInBox.z && positionInBox.z <= halfSize.z);
            }
        }

        /// <summary>
        /// Initializes the RemoveMeshInBox with the specified default behavior version.
        ///
        /// As Described in the documentation, you have to call this method after `AddComponent` to make sure
        /// the default configuration is what you want.
        /// Without calling this method, the default configuration might be changed in the future.
        /// </summary>
        /// <param name="version">
        /// The default configuration version.
        /// Since 1.7.0, version 1 is supported.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Unsupported configuration version</exception>
        [PublicAPI]
        public void Initialize(int version)
        {
            switch (version)
            {
                case 1:
                    // nothing to do
                    break; 
                default:
                    throw new ArgumentOutOfRangeException(nameof(version), $"unsupported version: {version}");
            }
        }

        /// <summary>
        /// Gets or sets the boxes to remove meshes.
        ///
        /// This component will remove the polygons entirely in the box.
        /// </summary>
        [PublicAPI]
        public BoundingBox[] Boxes
        {
            // clone them for future API changes
            get => boxes.ToArray();
            set => boxes = value.ToArray();
        }
    }
}
