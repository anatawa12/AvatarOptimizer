using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;

internal class OptimizeTexture : TraceAndOptimizePass<OptimizeTexture>
{
    public override string DisplayName => "T&O: OptimizeTexture";

    readonly struct UVID: IEquatable<UVID>
    {
        public readonly MeshInfo2? MeshInfo2;
        public readonly int SubMeshIndex;
        public readonly ShaderKnowledge.UVChannel UVChannel;

        public SubMeshId SubMeshId => MeshInfo2 != null ? new SubMeshId(MeshInfo2!, SubMeshIndex) : throw new InvalidOperationException();

        public UVID(SubMeshId subMeshId, ShaderKnowledge.UVChannel uvChannel)
            : this(subMeshId.MeshInfo2, subMeshId.SubMeshIndex, uvChannel)
        {
        }

        public UVID(MeshInfo2 meshInfo2, int subMeshIndex, ShaderKnowledge.UVChannel uvChannel)
        {
            MeshInfo2 = meshInfo2;
            SubMeshIndex = subMeshIndex;
            UVChannel = uvChannel;
            switch (uvChannel)
            {
                case ShaderKnowledge.UVChannel.UV0:
                case ShaderKnowledge.UVChannel.UV1:
                case ShaderKnowledge.UVChannel.UV2:
                case ShaderKnowledge.UVChannel.UV3:
                case ShaderKnowledge.UVChannel.UV4:
                case ShaderKnowledge.UVChannel.UV5:
                case ShaderKnowledge.UVChannel.UV6:
                case ShaderKnowledge.UVChannel.UV7:
                    break;
                case ShaderKnowledge.UVChannel.NonMeshRelated:
                    MeshInfo2 = null;
                    SubMeshIndex = -1;
                    break;
                case ShaderKnowledge.UVChannel.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(uvChannel), uvChannel, null);
            }
        }

        public bool Equals(UVID other) => MeshInfo2 == other.MeshInfo2 && SubMeshIndex == other.SubMeshIndex && UVChannel == other.UVChannel;
        public override bool Equals(object? obj) => obj is UVID other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(MeshInfo2, SubMeshIndex, UVChannel);

        public override string ToString()
        {
            if (MeshInfo2 == null) return UVChannel.ToString();
            return $"{MeshInfo2.SourceRenderer.name} {SubMeshIndex} {UVChannel}";
        }
    }

    readonly struct SubMeshId : IEquatable<SubMeshId>
    {
        public readonly MeshInfo2 MeshInfo2;
        public readonly int SubMeshIndex;

        public SubMeshId(MeshInfo2 meshInfo2, int subMeshIndex)
        {
            MeshInfo2 = meshInfo2;
            SubMeshIndex = subMeshIndex;
        }

        public bool Equals(SubMeshId other) => MeshInfo2 == other.MeshInfo2 && SubMeshIndex == other.SubMeshIndex;
        public override bool Equals(object? obj) => obj is SubMeshId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(MeshInfo2, SubMeshIndex);

        public override string ToString() => $"{MeshInfo2.SourceRenderer.name} {SubMeshIndex}";
    }

    protected override void Execute(BuildContext context, TraceAndOptimizeState state)
    {
        if (!state.OptimizeTexture) return;

        // those two maps should only hold mergeable materials and submeshes
        var materialUsers = new Dictionary<Material, HashSet<SubMeshId>>();
        var materialsBySubMesh = new Dictionary<SubMeshId, HashSet<Material>>();

        var unmergeableMaterials = new HashSet<Material>();

        // first, collect all submeshes information
        foreach (var renderer in context.GetComponents<SkinnedMeshRenderer>())
        {
            var meshInfo = context.GetMeshInfoFor(renderer);

            if (meshInfo.SubMeshes.All(x => x.SharedMaterials.Length == 1 && x.SharedMaterial != null))
            {
                // Good! It's mergeable
                for (var submeshIndex = 0; submeshIndex < meshInfo.SubMeshes.Count; submeshIndex++)
                {
                    var subMesh = meshInfo.SubMeshes[submeshIndex];

                    var possibleMaterials = new HashSet<Material>(new[] { subMesh.SharedMaterial! });
                    var (safeToMerge, animatedMaterials) = GetAnimatedMaterialsForSubMesh(context,
                        meshInfo.SourceRenderer, submeshIndex);
                    possibleMaterials.UnionWith(animatedMaterials);

                    if (safeToMerge)
                    {
                        materialsBySubMesh.Add(new SubMeshId(meshInfo, submeshIndex), possibleMaterials);
                        foreach (var possibleMaterial in possibleMaterials)
                        {
                            if (!materialUsers.TryGetValue(possibleMaterial, out var users))
                                materialUsers.Add(possibleMaterial, users = new HashSet<SubMeshId>());

                            users.Add(new SubMeshId(meshInfo, submeshIndex));
                        }
                    }
                    else
                    {
                        unmergeableMaterials.UnionWith(possibleMaterials);
                    }
                }
            }
            else
            {
                // Sorry, I don't support this (for now)
                var materialSlotIndex = 0;

                foreach (var subMesh in meshInfo.SubMeshes)
                {
                    foreach (var material in subMesh.SharedMaterials)
                    {
                        if (material != null) unmergeableMaterials.Add(material);

                        var (_, materials) = GetAnimatedMaterialsForSubMesh(context, renderer, materialSlotIndex);
                        unmergeableMaterials.UnionWith(materials);
                        materialSlotIndex++;
                    }
                }
            }
        }

        // collect usageInformation for each material, and add to unmergeableMaterials if it's impossible
        var usageInformations = new Dictionary<Material, ShaderKnowledge.TextureUsageInformation[]>();
        {

            foreach (var (material, _) in materialUsers)
            {
                var provider = new MaterialPropertyAnimationProvider(
                    materialUsers[material].Select(x => context.GetAnimationComponent(x.MeshInfo2.SourceRenderer))
                        .ToList());
                if (GetTextureUsageInformations(material, provider) is not { } textureUsageInformations)
                    unmergeableMaterials.Add(material);
                else
                    usageInformations.Add(material, textureUsageInformations);
            }
        }

        // for implementation simplicity, we don't support texture(s) that are not used by multiple set of UV
        {
            var materialsByUSerSubmeshId = new Dictionary<EqualsHashSet<SubMeshId>, HashSet<Material>>();
            foreach (var (material, users) in materialUsers)
            {
                var set = new EqualsHashSet<SubMeshId>(users);
                if (!materialsByUSerSubmeshId.TryGetValue(set, out var materials))
                    materialsByUSerSubmeshId.Add(set, materials = new HashSet<Material>());
                materials.Add(material);
            }

            var textureUserSets = new Dictionary<Texture2D, HashSet<EqualsHashSet<UVID>>>();
            var textureUserMaterials = new Dictionary<Texture2D, HashSet<Material>>();
            foreach (var (key, materials) in materialsByUSerSubmeshId)
            {
                foreach (var material in materials)
                {
                    foreach (var information in usageInformations[material])
                    {
                        var texture = (Texture2D)material.GetTexture(information.MaterialPropertyName);
                        if (texture == null) continue;
                        if (!textureUserSets.TryGetValue(texture, out var users))
                            textureUserSets.Add(texture, users = new HashSet<EqualsHashSet<UVID>>());
                        users.Add(key.backedSet.Select(x => new UVID(x, information.UVChannel)).ToEqualsHashSet());
                        if (!textureUserMaterials.TryGetValue(texture, out var materialsSet))
                            textureUserMaterials.Add(texture, materialsSet = new HashSet<Material>());
                        materialsSet.Add(material);
                    }
                }
            }

            foreach (var (texture, users) in textureUserSets.Where(x => x.Value.Count >= 2))
                unmergeableMaterials.UnionWith(textureUserMaterials[texture]);
        }

        // remove unmergeable materials and submeshes that have unmergeable materials
        {
            var processMaterials = new List<Material>(unmergeableMaterials);
            while (processMaterials.Count != 0)
            {
                var processSubmeshes = new List<SubMeshId>();

                foreach (var processMaterial in processMaterials)
                {
                    if (!materialUsers.Remove(processMaterial, out var users)) continue;

                    foreach (var user in users)
                        processSubmeshes.Add(user);
                }

                processMaterials.Clear();

                foreach (var processSubmesh in processSubmeshes)
                {
                    if (!materialsBySubMesh.Remove(processSubmesh, out var materials)) continue;

                    var newUnmergeableMaterials = materials.Where(m => !unmergeableMaterials.Contains(m)).ToList();
                    unmergeableMaterials.UnionWith(newUnmergeableMaterials);
                    processMaterials.AddRange(newUnmergeableMaterials);
                }
            }
        }

        // TODO: implement merging
        {
            var textureUserMaterials = new Dictionary<Texture2D, HashSet<(Material, string)>>();
            var textureByUVs = new Dictionary<EqualsHashSet<UVID>, HashSet<Texture2D>>();
            foreach (var (material, value) in materialUsers)
            {
                foreach (var information in usageInformations[material])
                {
                    var texture = (Texture2D)material.GetTexture(information.MaterialPropertyName);
                    if (texture == null) continue;

                    var uvSet = new EqualsHashSet<UVID>(value.Select(x => new UVID(x, information.UVChannel)));
                    if (!textureByUVs.TryGetValue(uvSet, out var textures))
                        textureByUVs.Add(uvSet, textures = new HashSet<Texture2D>());
                    textures.Add(texture);

                    if (!textureUserMaterials.TryGetValue(texture, out var materials))
                        textureUserMaterials.Add(texture, materials = new HashSet<(Material, string)>());
                    materials.Add((material, information.MaterialPropertyName));
                }
            }

            foreach (var (uvSet, textures) in textureByUVs)
            {
                MayAtlasTexture(textures, uvSet.backedSet);
            }
        }
    }

    (bool safeToMerge, IEnumerable<Material> materials) GetAnimatedMaterialsForSubMesh(
        BuildContext context, Renderer renderer, int materialSlotIndex)
    {
        var component = context.GetAnimationComponent(renderer);

        if (!component.TryGetObject($"m_Materials.Array.data[{materialSlotIndex}]", out var animation))
            return (safeToMerge: true, Array.Empty<Material>());

        if (animation.ComponentNodes.SingleOrDefault() is AnimatorPropModNode<Object> componentNode)
        {
            if (componentNode.Value.PossibleValues is { } possibleValues)
            {
                if (possibleValues.All(x => x is Material))
                    return (safeToMerge: true, materials: possibleValues.Cast<Material>());

                return (safeToMerge: false, materials: possibleValues.OfType<Material>());
            }
            else
            {
                return (safeToMerge: false, materials: Array.Empty<Material>());
            }
        }
        else if (animation.Value.PossibleValues is { } possibleValues)
        {
            return (safeToMerge: false, materials: possibleValues.OfType<Material>());
        }
        else if (animation.ComponentNodes.OfType<AnimatorPropModNode<Object>>().FirstOrDefault() is
                 { } fallbackAnimatorNode)
        {
            var materials = fallbackAnimatorNode.Value.PossibleValues?.OfType<Material>() ?? Array.Empty<Material>();
            return (safeToMerge: false, materials);
        }

        return (safeToMerge: true, Array.Empty<Material>());
    }

    static ShaderKnowledge.TextureUsageInformation[]?
        GetTextureUsageInformations(Material material,
            ShaderKnowledge.IMaterialPropertyAnimationProvider animationProvider)
    {
        if (ShaderKnowledge.GetTextureUsageInformationForMaterial(material, animationProvider)
            is not { } textureInformations)
            return null;

        foreach (var textureInformation in textureInformations)
        {
            switch (textureInformation.UVChannel)
            {
                case ShaderKnowledge.UVChannel.UV0:
                case ShaderKnowledge.UVChannel.UV1:
                case ShaderKnowledge.UVChannel.UV2:
                case ShaderKnowledge.UVChannel.UV3:
                case ShaderKnowledge.UVChannel.UV4:
                case ShaderKnowledge.UVChannel.UV5:
                case ShaderKnowledge.UVChannel.UV6:
                case ShaderKnowledge.UVChannel.UV7:
                case ShaderKnowledge.UVChannel.NonMeshRelated:
                    break;
                case ShaderKnowledge.UVChannel.Unknown:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return textureInformations;
    }

    class MaterialPropertyAnimationProvider : ShaderKnowledge.IMaterialPropertyAnimationProvider
    {
        private readonly List<AnimationComponentInfo<PropertyInfo>> _infos;

        public MaterialPropertyAnimationProvider(List<AnimationComponentInfo<PropertyInfo>> infos)
        {
            _infos = infos;
        }

        public bool IsAnimated(string propertyName) => 
            _infos.Any(x => x.TryGetFloat($"material.{propertyName}", out _));
    }

    // TODO: uncomment before release
    // [Conditional("NEVER_TRUE_VALUE_IS_EXPECTED")]
    private static void TraceLog(string message)
    {
        Debug.Log(message);
    }

    struct AtlasResult
    {
        public Dictionary<Texture2D, Texture2D> TextureMapping;
        public Dictionary<(Vertex, int uvChannel), Vector2> NewUVs;

        public AtlasResult(Dictionary<Texture2D, Texture2D> textureMapping, Dictionary<(Vertex, int uvChannel), Vector2> newUVs)
        {
            TextureMapping = textureMapping;
            NewUVs = newUVs;
        }

        public static AtlasResult Empty = new(new Dictionary<Texture2D, Texture2D>(),
            new Dictionary<(Vertex, int uvChannel), Vector2>());
    }

    static AtlasResult MayAtlasTexture(ICollection<Texture2D> textures, ICollection<UVID> users)
    {
        if (users.Any(uvid => uvid.UVChannel == ShaderKnowledge.UVChannel.NonMeshRelated))
            return AtlasResult.Empty;

        foreach (var user in users)
        {
            var submesh = user.MeshInfo2!.SubMeshes[user.SubMeshIndex];
            // currently non triangle topology is not supported
            if (submesh.Topology != MeshTopology.Triangles)
                return AtlasResult.Empty;
            foreach (var vertex in submesh.Vertices)
            {
                var coord = vertex.GetTexCoord((int)user.UVChannel);

                // UV Tiling is currently not supported
                // TODO: if entire island is in n.0<=x<n+1, y.0<=y<n+1, then it might be safe to atlas (if TextureWrapMode is repeat)
                if (coord.x is not (>= 0 and < 1) || coord.y is not (>= 0 and < 1))
                    return AtlasResult.Empty;
            }
        }

        static IEnumerable<IslandUtility.Triangle> TrianglesByUVID(UVID uvid)
        {
            var submesh = uvid.MeshInfo2!.SubMeshes[uvid.SubMeshIndex];
            for (var index = 0; index < submesh.Vertices.Count; index += 3)
            {
                var vertex0 = submesh.Vertices[index + 0];
                var vertex1 = submesh.Vertices[index + 1];
                var vertex2 = submesh.Vertices[index + 2];
                yield return new IslandUtility.Triangle((int)uvid.UVChannel, vertex0, vertex1, vertex2);
            }
        }

        var triangles = users.SelectMany(TrianglesByUVID).ToList();
        var islands = IslandUtility.UVtoIsland(triangles);

        // TODO: merge too over wrapped islands
        // We should: merge islands completely inside other island
        // We may: merge islands >N% wrapped (heuristic)

        // fit Island bounds to pixel bounds
        var maxResolution = -1;
        var minResolution = int.MaxValue;
        {
            foreach (var texture2D in textures)
            {
                var width = texture2D.width;
                var height = texture2D.height;

                if (!width.IsPowerOfTwo() || !height.IsPowerOfTwo())
                {
                    TraceLog($"{string.Join(", ", textures)} will not merged because {texture2D} is not power of two");
                    return AtlasResult.Empty;
                }

                maxResolution = Mathf.Max(maxResolution, width, height);
                minResolution = Mathf.Min(minResolution, width, height);
            }

            // padding is at least 4px with max resolution, 1px in min resolution
            const int paddingSize = 4;

            if (minResolution <= paddingSize || maxResolution <= paddingSize)
            {
                TraceLog(
                    $"{string.Join(", ", textures)} will not merged because min resolution is less than 4 ({minResolution})");
                return AtlasResult.Empty;
            }

            if (maxResolution / paddingSize < minResolution)
                minResolution = maxResolution / paddingSize;

            foreach (var island in islands)
            {
                ref var min = ref island.MinPos;
                ref var max = ref island.MaxPos;

                // floor/ceil to pixel bounds and add padding
                
                min.x = Mathf.Max(Mathf.Floor(min.x * minResolution - 1) / minResolution, 0);
                min.y = Mathf.Max(Mathf.Floor(min.y * minResolution - 1) / minResolution, 0);
                max.x = Mathf.Min(Mathf.Ceil(max.x * minResolution + 1) / minResolution, 1);
                max.y = Mathf.Min(Mathf.Ceil(max.y * minResolution + 1) / minResolution, 1);
            }
        }

        // Check for island size before trying to atlas
        var totalIslandSize = islands.Sum(x => x.Size.x * x.Size.y);
        if (totalIslandSize >= 0.5)
        {
            TraceLog($"{string.Join(", ", textures)} will not merged because more than half ({totalIslandSize}) are used");

            return AtlasResult.Empty;
        }

        var maxIslandLength = islands.Max(x => Mathf.Max(x.Size.x, x.Size.y));
        if (maxIslandLength >= 0.5)
        {
            TraceLog($"{string.Join(", ", textures)} will not merged because max island length is more than 0.5 ({maxIslandLength})");

            return AtlasResult.Empty;
        }

        TraceLog($"{string.Join(", ", textures)} will go to atlas texture (using {totalIslandSize} of texture)");
        return AtlasResult.Empty;
    }

    // Copied from TexTransTool
    // https://github.com/ReinaS-64892/TexTransTool/blob/48c608c816c718acc5be607b5c1232870bafc674/TexTransCore/Island/IslandUtility.cs
    // Licensed under MIT
    // Copyright (c) 2023 Reina_Sakiria
    internal static class IslandUtility
    {
        /// <summary>
        /// Union-FindアルゴリズムのためのNode Structureです。細かいアロケーションの負荷を避けるために、配列で管理する想定で、
        /// ポインターではなくインデックスで親ノードを指定します。
        ///
        /// グループの代表でない限り、parentIndex以外の値は無視されます（古いデータが入る場合があります）
        /// </summary>
        internal struct VertNode
        {
            public int parentIndex;

            public (Vector2, Vector2) boundingBox;

            public int depth;
            public int triCount;

            public Island? island;

            public VertNode(int i, Vector2 uv)
            {
                parentIndex = i;
                boundingBox = (uv, uv);
                depth = 0;
                island = null;
                triCount = 0;
            }

            /// <summary>
            /// 指定したインデックスのノードのグループの代表ノードを調べる
            /// </summary>
            /// <param name="arr"></param>
            /// <param name="index"></param>
            /// <returns></returns>
            public static int Find(VertNode[] arr, int index)
            {
                if (arr[index].parentIndex == index) return index;

                return arr[index].parentIndex = Find(arr, arr[index].parentIndex);
            }

            /// <summary>
            /// 指定したふたつのノードを結合する
            /// </summary>
            /// <param name="arr"></param>
            /// <param name="a"></param>
            /// <param name="b"></param>
            public static void Merge(VertNode[] arr, int a, int b)
            {
                a = Find(arr, a);
                b = Find(arr, b);

                if (a == b) return;

                if (arr[a].depth < arr[b].depth)
                {
                    (a, b) = (b, a);
                }

                if (arr[a].depth == arr[b].depth) arr[a].depth++;
                arr[b].parentIndex = a;

                arr[a].boundingBox = (Vector2.Min(arr[a].boundingBox.Item1, arr[b].boundingBox.Item1),
                    Vector2.Max(arr[a].boundingBox.Item2, arr[b].boundingBox.Item2));
                arr[a].triCount += arr[b].triCount;
            }

            /// <summary>
            /// このグループに該当するIslandに三角面を追加します。Islandが存在しない場合は作成しislandListに追加します。
            /// </summary>
            /// <param name="idx"></param>
            /// <param name="islandList"></param>
            public void AddTriangle(Triangle idx, List<Island> islandList)
            {
                if (island == null)
                {
                    islandList.Add(island = new Island());
                    island.triangles.Capacity = triCount;

                    var min = boundingBox.Item1;
                    var max = boundingBox.Item2;

                    island.MinPos = min;
                    island.MaxPos = max;
                }

                island.triangles.Add(idx);
            }
        }

        public readonly struct Triangle : IEnumerable<Vertex>
        {
            public readonly int UVIndex;
            public readonly Vertex zero;
            public readonly Vertex one;
            public readonly Vertex two;

            public Triangle(int uvIndex, Vertex zero, Vertex one, Vertex two)
            {
                UVIndex = uvIndex;
                this.zero = zero;
                this.one = one;
                this.two = two;
            }

            Enumerator GetEnumerator() => new(this);
            IEnumerator<Vertex> IEnumerable<Vertex>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            struct Enumerator : IEnumerator<Vertex>
            {
                private readonly Triangle triangle;
                private int index;

                public Enumerator(Triangle triangle)
                {
                    this.triangle = triangle;
                    index = -1;
                }

                public bool MoveNext()
                {
                    index++;
                    return index < 3;
                }

                public void Reset() => index = -1;

                public Vertex Current => index switch
                {
                    0 => triangle.zero,
                    1 => triangle.one,
                    2 => triangle.two,
                    _ => throw new InvalidOperationException(),
                };

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                }
            }
        }

        public static List<Island> UVtoIsland(ICollection<Triangle> triangles)
        {
            Profiler.BeginSample("UVtoIsland");
            var islands = UVToIslandImpl(triangles);
            Profiler.EndSample();

            return islands;
        }

        private static List<Island> UVToIslandImpl(ICollection<Triangle> triangles)
        {
            // 同一の位置にある頂点をまず調べて、共通のインデックスを割り当てます
            Profiler.BeginSample("Preprocess vertices");
            var indexToUv = new List<Vector2>();
            var uvToIndex = new Dictionary<Vector2, int>();
            var inputVertToUniqueIndex = new List<int>();
            var vertexToUniqueIndex = new Dictionary<Vertex, int>();
            {
                var uniqueUv = 0;
                foreach (var triangle in triangles)
                {
                    foreach (var vertex in triangle)
                    {
                        var uv = (Vector2)vertex.GetTexCoord(triangle.UVIndex);
                        if (!uvToIndex.TryGetValue(uv, out var uvVert))
                        {
                            uvToIndex.Add(uv, uvVert = uniqueUv++);
                            indexToUv.Add(uv);
                        }

                        inputVertToUniqueIndex.Add(uvVert);
                        vertexToUniqueIndex[vertex] = uvVert;
                    }
                }
            }
            System.Diagnostics.Debug.Assert(indexToUv.Count == uvToIndex.Count);
            System.Diagnostics.Debug.Assert(indexToUv.Count == inputVertToUniqueIndex.Count);
            Profiler.EndSample();

            // Union-Find用のデータストラクチャーを初期化
            Profiler.BeginSample("Init vertNodes");
            var nodes = new VertNode[uvToIndex.Count];
            for (var i = 0; i < nodes.Length; i++)
                nodes[i] = new VertNode(i, indexToUv[i]);
            Profiler.EndSample();

            Profiler.BeginSample("Merge vertices");
            foreach (var tri in triangles)
            {
                int idx_a = vertexToUniqueIndex[tri.zero];
                int idx_b = vertexToUniqueIndex[tri.one];
                int idx_c = vertexToUniqueIndex[tri.two];

                // 三角面に該当するノードを併合
                VertNode.Merge(nodes, idx_a, idx_b);
                VertNode.Merge(nodes, idx_b, idx_c);

                // 際アロケーションを避けるために三角面を数える
                nodes[VertNode.Find(nodes, idx_a)].triCount++;
            }

            Profiler.EndSample();

            var islands = new List<Island>();

            // この時点で代表が決まっているので、三角を追加していきます。
            Profiler.BeginSample("Add triangles to islands");
            foreach (var tri in triangles)
            {
                int idx = vertexToUniqueIndex[tri.zero];

                nodes[VertNode.Find(nodes, idx)].AddTriangle(tri, islands);
            }

            Profiler.EndSample();

            return islands;
        }


        [Serializable]
        public class Island
        {
            public List<Triangle> triangles;
            public Vector2 MinPos;
            public Vector2 MaxPos;

            public Vector2 Pivot;
            public bool Is90Rotation;

            public Vector2 Size => MaxPos - MinPos;

            public Island(Island source)
            {
                triangles = new List<Triangle>(source.triangles);
                MinPos = source.MinPos;
                MaxPos = source.MaxPos;
                Is90Rotation = source.Is90Rotation;
            }

            public Island(Triangle triangle)
            {
                triangles = new List<Triangle> { triangle };
            }

            public Island()
            {
                triangles = new List<Triangle>();
            }

            public Island(List<Triangle> trianglesOfIsland)
            {
                triangles = trianglesOfIsland;
            }
        }
    }
}
