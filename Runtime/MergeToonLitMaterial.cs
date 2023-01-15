using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Merge Toon Lit Material")]
    internal class MergeToonLitMaterial : EditSkinnedMeshComponent
    {
        public MergeInfo[] merges = Array.Empty<MergeInfo>();

        [Serializable]
        internal class MergeInfo
        {
            public MergeSource[] source = Array.Empty<MergeSource>();
            // 2^x. 13 (2^13 = 8192) is upper limit.
            public Vector2Int textureSize = new Vector2Int(2048, 2048);
        }
        [Serializable]
        internal class MergeSource
        {
            public int materialIndex;
            public Rect targetRect = new Rect(0, 0, 1, 1);
        }
    }
}
