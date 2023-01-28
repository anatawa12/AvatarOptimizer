using System.Collections;
using System.Linq;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class MergeToonLitMaterialProcessor : EditSkinnedMeshProcessor<MergeToonLitMaterial>
    {
        private static readonly int MainTexProp = Shader.PropertyToID("_MainTex");
        private static readonly int MainTexStProp = Shader.PropertyToID("_MainTex_ST");
        private static readonly int RectProp = Shader.PropertyToID("_Rect");

        private static Material _helperMaterial;

        private static Material HelperMaterial =>
            _helperMaterial ? _helperMaterial : _helperMaterial = new Material(Utils.MergeTextureHelper);

        public MergeToonLitMaterialProcessor(MergeToonLitMaterial component) : base(component)
        {
        }

        public override int ProcessOrder => -10000;

        public override void Process(OptimizerSession session, MeshInfo2 target)
        {
            // compute usages. AdditionalTemporal is usage count for now.
            // if #usages is not zero for merging triangles
            foreach (var v in target.Vertices) v.AdditionalTemporal = 0;

            foreach (var targetSubMesh in target.SubMeshes)
            foreach (var v in targetSubMesh.Triangles)
                v.AdditionalTemporal++;

            // compute per-material data
            var mergingIndices = ComputeMergingIndices();
            var targetRectForMaterial = new Rect[target.SubMeshes.Count];
            foreach (var componentMerge in Component.merges)
            foreach (var mergeSource in componentMerge.source)
                targetRectForMaterial[mergeSource.materialIndex] = mergeSource.targetRect;

            // map UVs
            for (var subMeshI = 0; subMeshI < target.SubMeshes.Count; subMeshI++)
            {
                if (mergingIndices[subMeshI])
                {
                    // the material is for merge.
                    var subMesh = target.SubMeshes[subMeshI];
                    var targetRect = targetRectForMaterial[subMeshI];
                    for (var i = 0; i < subMesh.Triangles.Count; i++)
                    {
                        if (subMesh.Triangles[i].AdditionalTemporal != 1)
                        {
                            // if there are multiple users for the vertex: duplicate it
                            var cloned = subMesh.Triangles[i].Clone();
                            target.Vertices.Add(cloned);
                            subMesh.Triangles[i] = cloned;

                            subMesh.Triangles[i].AdditionalTemporal--;
                            cloned.AdditionalTemporal = 1;
                        }

                        subMesh.Triangles[i].TexCoord0 = MapUV(subMesh.Triangles[i].TexCoord0, targetRect);
                    }
                }
            }

            // merge submeshes
            var copied = target.SubMeshes.Where((_, i) => !mergingIndices[i]);
            var materials = target.SubMeshes.Select(x => x.SharedMaterial).ToArray();
            var merged = Component.merges.Select(x => new SubMesh(
                x.source.SelectMany(src => target.SubMeshes[src.materialIndex].Triangles).ToList(),
                CreateMaterial(GenerateTexture(x, materials))));
            var subMeshes = copied.Concat(merged).ToList();
            target.SubMeshes.Clear();
            target.SubMeshes.AddRange(subMeshes);
            
            foreach (var subMesh in target.SubMeshes)
            {
                session.AddToAsset(subMesh.SharedMaterial);
                session.AddToAsset(subMesh.SharedMaterial.GetTexture(MainTexProp));
            }
        }

        private Vector2 MapUV(Vector2 vector2, Rect destSourceRect) =>
            vector2 * new Vector2(destSourceRect.width, destSourceRect.height) 
            + new Vector2(destSourceRect.x, destSourceRect.y);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// </summary>
        /// <returns>bitarray[i] is true if the materials[i] ill be merged to other material</returns>
        public BitArray ComputeMergingIndices()
        {
            var mergingIndices = new BitArray(Target.sharedMesh.subMeshCount);
            foreach (var mergeInfo in Component.merges)
            foreach (var source in mergeInfo.source)
                mergingIndices[source.materialIndex] = true;
            return mergingIndices;
        }

        private Material[] CreateMaterials(BitArray mergingIndices, Material[] upstream, bool fast)
        {
            var copied = upstream.Where((_, i) => !mergingIndices[i]);
            if (fast)
            {
                return copied.Concat(Component.merges.Select(x => new Material(Utils.ToonLitShader))).ToArray();
            }
            else
            {
                // slow mode: generate texture actually
                return copied.Concat(GenerateTextures(Component, upstream).Select(CreateMaterial)).ToArray();
            }
        }

        private static Material CreateMaterial(Texture texture)
        {
            var mat = new Material(Utils.ToonLitShader);
            mat.SetTexture(MainTexProp, texture);
            return mat;
        }

        public static Texture[] GenerateTextures(MergeToonLitMaterial config, Material[] materials)
        {
            return config.merges.Select(x => GenerateTexture(x, materials)).ToArray();
        }

        private static Texture GenerateTexture(MergeToonLitMaterial.MergeInfo mergeInfo, Material[] materials)
        {
            var texWidth = mergeInfo.textureSize.x;
            var texHeight = mergeInfo.textureSize.y;
            var texture = new Texture2D(texWidth, texHeight);
            var target = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGB32);

            foreach (var source in mergeInfo.source)
            {
                var sourceMat = materials[source.materialIndex];
                var sourceTex = sourceMat.GetTexture(MainTexProp);
                var sourceTexSt = sourceMat.GetVector(MainTexStProp);
                HelperMaterial.SetTexture(MainTexProp, sourceTex);
                HelperMaterial.SetVector(MainTexStProp, sourceTexSt);
                HelperMaterial.SetVector(RectProp,
                    new Vector4(source.targetRect.x, source.targetRect.y, source.targetRect.width,
                        source.targetRect.height));
                Graphics.Blit(sourceTex, target, HelperMaterial);
            }

            var prev = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                texture.Apply();
            }
            finally
            {
                RenderTexture.active = prev;
            }

            return texture;
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this, upstream);

        class MeshInfoComputer : AbstractMeshInfoComputer
        {
            private readonly MergeToonLitMaterialProcessor _processor;

            public MeshInfoComputer(MergeToonLitMaterialProcessor processor, IMeshInfoComputer upstream) : base(upstream)
                => _processor = processor;

            public override Material[] Materials(bool fast = true) => 
                _processor.CreateMaterials(_processor.ComputeMergingIndices(), base.Materials(fast), fast);
        }
    }
}
