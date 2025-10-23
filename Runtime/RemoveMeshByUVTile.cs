using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Remove Mesh By UV Tile")]
    // [RequireComponent(typeof(SkinnedMeshRenderer) or typeof(MeshRenderer))] // handled in editor
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/remove-mesh-by-uv-tile/")]
    internal sealed class RemoveMeshByUVTile : EditSkinnedMeshComponent, INoSourceEditSkinnedMeshComponent
    {
        [SerializeField]
        internal MaterialSlot[] materials = Array.Empty<MaterialSlot>();

        private void Reset()
        {
            if (!TryGetComponent<SkinnedMeshRenderer>(out var renderer)) return;
            var mesh = renderer.sharedMesh;
            if (mesh == null) return;
            materials = new MaterialSlot[mesh.subMeshCount];
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct MaterialSlot : IEquatable<MaterialSlot>
        {
            [SerializeField] public bool removeTile0;
            [SerializeField] public bool removeTile1;
            [SerializeField] public bool removeTile2;
            [SerializeField] public bool removeTile3;
            [SerializeField] public bool removeTile4;
            [SerializeField] public bool removeTile5;
            [SerializeField] public bool removeTile6;
            [SerializeField] public bool removeTile7;
            [SerializeField] public bool removeTile8;
            [SerializeField] public bool removeTile9;
            [SerializeField] public bool removeTile10;
            [SerializeField] public bool removeTile11;
            [SerializeField] public bool removeTile12;
            [SerializeField] public bool removeTile13;
            [SerializeField] public bool removeTile14;
            [SerializeField] public bool removeTile15;

            [AAOLocalized("RemoveMeshByUVTile:prop:UVChannel")]
            [SerializeField] public UVChannel uvChannel;

            [BurstCompile]
            private Span<bool> AsSpan() => MemoryMarshal.CreateSpan(ref removeTile0, 16);

            public bool RemoveAnyTile =>
                removeTile0 || removeTile1 || removeTile2 || removeTile3 ||
                removeTile4 || removeTile5 || removeTile6 || removeTile7 ||
                removeTile8 || removeTile9 || removeTile10 || removeTile11 ||
                removeTile12 || removeTile13 || removeTile14 || removeTile15;

            [BurstCompile]
            public bool GetTile(int tile) => AsSpan()[tile];
            

            public bool Equals(MaterialSlot other) =>
                removeTile0 == other.removeTile0 &&
                removeTile1 == other.removeTile1 &&
                removeTile2 == other.removeTile2 &&
                removeTile3 == other.removeTile3 &&
                removeTile4 == other.removeTile4 &&
                removeTile5 == other.removeTile5 &&
                removeTile6 == other.removeTile6 &&
                removeTile7 == other.removeTile7 &&
                removeTile8 == other.removeTile8 &&
                removeTile9 == other.removeTile9 &&
                removeTile10 == other.removeTile10 &&
                removeTile11 == other.removeTile11 &&
                removeTile12 == other.removeTile12 &&
                removeTile13 == other.removeTile13 &&
                removeTile14 == other.removeTile14 &&
                removeTile15 == other.removeTile15;

            public override bool Equals(object? obj) => obj is MaterialSlot other && Equals(other);

            public override int GetHashCode()
            {
                var hashCode = new HashCode();
                hashCode.Add(removeTile0);
                hashCode.Add(removeTile1);
                hashCode.Add(removeTile2);
                hashCode.Add(removeTile3);
                hashCode.Add(removeTile4);
                hashCode.Add(removeTile5);
                hashCode.Add(removeTile6);
                hashCode.Add(removeTile7);
                hashCode.Add(removeTile8);
                hashCode.Add(removeTile9);
                hashCode.Add(removeTile10);
                hashCode.Add(removeTile11);
                hashCode.Add(removeTile12);
                hashCode.Add(removeTile13);
                hashCode.Add(removeTile14);
                hashCode.Add(removeTile15);
                return hashCode.ToHashCode();
            }

            public static bool operator ==(MaterialSlot left, MaterialSlot right) => left.Equals(right);
            public static bool operator !=(MaterialSlot left, MaterialSlot right) => !left.Equals(right);
        }

        internal enum UVChannel
        {
            TexCoord0,
            TexCoord1,
            TexCoord2,
            TexCoord3,
            TexCoord4,
            TexCoord5,
            TexCoord6,
            TexCoord7,
        }
    }
}
