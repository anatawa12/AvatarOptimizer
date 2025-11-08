using System;
using System.Linq;
using JetBrains.Annotations;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Animations;

namespace Anatawa12.AvatarOptimizer
{
    // Since AAO 1.8.0, this component can be added multiple times.
    // In AAO 1.7.0 or earlier, this component was marked as [DisallowMultipleComponent].
    [AddComponentMenu("Avatar Optimizer/AAO Remove Mesh By Box")]
    // [RequireComponent(typeof(SkinnedMeshRenderer) or typeof(MeshRenderer))] // handled in editor
    [AllowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/remove-mesh-in-box/")]
    [PublicAPI]
    public sealed class RemoveMeshInBox : EditSkinnedMeshComponent
    {
        [SerializeField]
        internal BoundingBox[] boxes = Array.Empty<BoundingBox>();

        [SerializeField]
        [AAOLocalized("RemoveMeshInBox:prop:removePolygonsToggle")]
        [NotKeyable]
        internal bool removeInBox = true;

        APIChecker _checker;

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
            // In Play Mode, the Reset() is not called so we have to call it manually here.
            Reset();
            switch (version)
            {
                case 1:
                    // nothing to do
                    break; 
                default:
                    throw new ArgumentOutOfRangeException(nameof(version), $"unsupported version: {version}");
            }
            _checker.OnInitialize(version, this);
        }

        [PublicAPI]
        public bool RemoveInBox
        {
            get => _checker.OnAPIUsage(this, removeInBox);
            set => _checker.OnAPIUsage(this, removeInBox = value);
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
            get => _checker.OnAPIUsage(this, boxes.ToArray());
            set => _checker.OnAPIUsage(this, boxes = value.ToArray());
        }
    }
}
