using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using Anatawa12.AvatarOptimizer.API;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;

internal class OptimizeTexture : TraceAndOptimizePass<OptimizeTexture>
{
    public override string DisplayName => "T&O: OptimizeTexture";

    protected override void Execute(BuildContext context, TraceAndOptimizeState state)
    {
        if (!state.OptimizeTexture) return;
        new OptimizeTextureImpl().Execute(context, state);
    }
}

internal struct OptimizeTextureImpl {
    private Dictionary<(Color c, bool isSrgb), Texture2D>? _colorTextures;
    private Dictionary<(Color c, bool isSrgb), Texture2D> ColorTextures => _colorTextures ??= new Dictionary<(Color c, bool isSrgb), Texture2D>();

    internal readonly struct UVID: IEquatable<UVID>
    {
        public readonly MeshInfo2? MeshInfo2;
        public readonly int SubMeshIndex;
        public readonly UVChannel UVChannel;

        public SubMeshId SubMeshId => MeshInfo2 != null ? new SubMeshId(MeshInfo2!, SubMeshIndex) : throw new InvalidOperationException();

        public UVID(SubMeshId subMeshId, UVChannel uvChannel)
            : this(subMeshId.MeshInfo2, subMeshId.SubMeshIndex, uvChannel)
        {
        }

        public UVID(MeshInfo2 meshInfo2, int subMeshIndex, UVChannel uvChannel)
        {
            MeshInfo2 = meshInfo2;
            SubMeshIndex = subMeshIndex;
            UVChannel = uvChannel;
            switch (uvChannel)
            {
                case UVChannel.UV0:
                case UVChannel.UV1:
                case UVChannel.UV2:
                case UVChannel.UV3:
                case UVChannel.UV4:
                case UVChannel.UV5:
                case UVChannel.UV6:
                case UVChannel.UV7:
                    break;
                case UVChannel.NonMeshRelated:
                    MeshInfo2 = null;
                    SubMeshIndex = -1;
                    break;
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

    internal readonly struct SubMeshId : IEquatable<SubMeshId>
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

    internal void Execute(BuildContext context, TraceAndOptimizeState state)
    {
        if (!state.OptimizeTexture) return;

        // collect all connected components of materialSlot - material - texture graph
        var connectedComponentInfos = CollectMaterialsUnionFind(context);

        // atlas texture for each connected component
        var uvInformation = connectedComponentInfos.SelectMany(AtlasConnectedComponent);

        RemapVertexUvs(uvInformation.ToList());
    }

    internal abstract class UnionFindNodeBase : UnionFindNode<UnionFindNodeBase>
    {
    }

    internal class ConnectedComponentInfo
    {
        public List<SubMeshNode> SubMeshes { get; } = new();
        public List<MaterialNode> Materials { get; } = new();
        public List<TextureNode> Textures { get; } = new();
    }

    internal class SubMeshNode : UnionFindNodeBase
    {
        // If we cannot get materials from animation, we cannot merge
        public bool SafeToMergeByAnimation { get; set; }

        public SubMeshId SubMeshId { get; }

        public SubMeshNode(SubMeshId subMeshId) => SubMeshId = subMeshId;
    }

    internal class MaterialNode : UnionFindNodeBase
    {
        // If we cannot collect textures from shader information (if null), unable to merge
        public List<TextureUsageInformation>? TextureUsageInformations { get; set; }

        // The list of submeshes that use this material
        public List<SubMeshNode> Users { get; } = new();

        public Material Material { get; }

        public MaterialNode(Material material) => Material = material;

        public void UseThis(SubMeshNode subMeshNode)
        {
            Users.Add(subMeshNode);
            UnionFind.Merge<UnionFindNodeBase>(this, subMeshNode);
        }
    }

    internal class TextureNode : UnionFindNodeBase
    {
        public Dictionary<MaterialNode, List<TextureUsageInformation>> Users { get; } = new();

        public Texture2D Texture { get; }

        public TextureNode(Texture2D texture) => Texture = texture;

        public void UseTexture(MaterialNode material, TextureUsageInformation? usage)
        {
            UnionFind.Merge<UnionFindNodeBase>(this, material);

            if (!Users.TryGetValue(material, out var usages))
                Users.Add(material, usages = new List<TextureUsageInformation>());
            if (usage != null) usages.Add(usage);
        }
    }

    internal IEnumerable<ConnectedComponentInfo> CollectMaterialsUnionFind(
        BuildContext context
    )
    {
        var subMeshes = new Dictionary<SubMeshId, SubMeshNode>();
        var materials = new Dictionary<Material, MaterialNode>();

        MaterialNode? GetMaterialNode(Material? material)
        {
            if (material == null) return null;
            if (!materials.TryGetValue(material, out var node))
                materials.Add(material, node = new MaterialNode(material));
            return node;
        }

        var texturs = new Dictionary<Texture2D, TextureNode>();

        TextureNode? GetTextureNode(Texture2D? texture)
        {
            if (texture == null) return null;
            if (!texturs.TryGetValue(texture, out var node))
                texturs.Add(texture, node = new TextureNode(texture));
            return node;
        }

        // first, process all submeshes and corresponding materials
        foreach (var renderer in context.GetComponents<SkinnedMeshRenderer>())
        {
            var meshInfo = context.GetMeshInfoFor(renderer);
            for (var submeshIndex = 0; submeshIndex < meshInfo.SubMeshes.Count; submeshIndex++)
            {
                var subMeshId = new SubMeshId(meshInfo, submeshIndex);
                var subMeshNode = new SubMeshNode(subMeshId);
                subMeshes.Add(subMeshId, subMeshNode);

                var subMesh = meshInfo.SubMeshes[submeshIndex];

                foreach (var material in subMesh.SharedMaterials)
                    GetMaterialNode(material)?.UseThis(subMeshNode);

                var (safeToMerge, animatedMaterials) = GetAnimatedMaterialsForSubMesh(context,
                    meshInfo.SourceRenderer, submeshIndex);

                subMeshNode.SafeToMergeByAnimation = safeToMerge;

                foreach (var material in animatedMaterials)
                    GetMaterialNode(material)?.UseThis(subMeshNode);
            }
        }

        // them, process textures
        foreach (var (material, materialNode) in materials)
        {
            var users = materialNode.Users;

            var provider = new TextureUsageInformationCallbackImpl(
                material,
                users.Select(x => context.GetAnimationComponent(x.SubMeshId.MeshInfo2.SourceRenderer))
                    .ToList());

            IEnumerable<(Texture2D?, TextureUsageInformation?)> textures;

            // collect texture usage information
            if (ShaderInformationRegistry.GetShaderInformation(material.shader) is { } information
                && information.GetTextureUsageInformationForMaterial(provider)
                && provider.TextureUsageInformations is { } informations)
            {
                materialNode.TextureUsageInformations = informations.ToList();

                textures = informations.Select(x =>
                    ((Texture2D?)material.GetTexture(x.MaterialPropertyName), (TextureUsageInformation?)x));
            }
            else
            {
                // failed to retrive texture information, just link texture node
                textures = material.GetTexturePropertyNames().Select(x => material.GetTexture(x)).OfType<Texture2D?>()
                    .Select(t => (t, (TextureUsageInformation?)null));
            }

            // merge texture nodes

            foreach (var (texture2D, usage) in textures)
                GetTextureNode(texture2D)?.UseTexture(materialNode, usage);
        }

        // We've finished merging nodes.
        // Now, we collect all information onto the connected component info
        var rootNodes = new Dictionary<UnionFindNodeBase, ConnectedComponentInfo>();

        foreach (var subMeshNode in subMeshes.Values)
        {
            var rootNode = subMeshNode.FindRoot();
            if (!rootNodes.TryGetValue(rootNode, out var rootInfo))
                rootNodes.Add(rootNode, rootInfo = new ConnectedComponentInfo());

            rootInfo.SubMeshes.Add(subMeshNode);
        }

        foreach (var materialNode in materials.Values)
        {
            var rootNode = materialNode.FindRoot();
            if (!rootNodes.TryGetValue(rootNode, out var rootInfo))
                rootNodes.Add(rootNode, rootInfo = new ConnectedComponentInfo());

            rootInfo.Materials.Add(materialNode);
        }

        foreach (var textureNode in texturs.Values)
        {
            var rootNode = textureNode.FindRoot();
            if (!rootNodes.TryGetValue(rootNode, out var rootInfo))
                rootNodes.Add(rootNode, rootInfo = new ConnectedComponentInfo());

            rootInfo.Textures.Add(textureNode);
        }

        // now we have all connected components, process each connected component

        return rootNodes.Values;
    }

    private IEnumerable<(EqualsHashSet<UVID>, AtlasResult)> AtlasConnectedComponent(
        ConnectedComponentInfo info)
    {
        // all material slot must successfully determine which materials will be used
        if (info.SubMeshes.Any(x => !x.SafeToMergeByAnimation))
            return Array.Empty<(EqualsHashSet<UVID>, AtlasResult)>();

        // all material must have valid TextureUsageInformation, otherwise UV packing would cause invalid data
        if (info.Materials.Any(mat => mat.TextureUsageInformations == null))
            return Array.Empty<(EqualsHashSet<UVID>, AtlasResult)>();

        var textureByUvId = new Dictionary<UVID, List<TextureNode>>();
        var uvidByTexture = new Dictionary<TextureNode, List<UVID>>();

        foreach (var texture in info.Textures)
        {
            var uvids = texture.Users.SelectMany((pair) =>
            {
                var (material, usages) = pair;
                return material.Users.SelectMany(x => usages.Select(y => new UVID(x.SubMeshId, y.UVChannel)));
            }).ToList();

            foreach (var uvid in uvids)
            {
                if (!textureByUvId.TryGetValue(uvid, out var list))
                    textureByUvId.Add(uvid, list = new List<TextureNode>());
                list.Add(texture);
            }

            uvidByTexture.Add(texture, uvids);
        }

        var badUvIds = new HashSet<UVID>();

        // texture to uvid graph must be (multiple set of) complete bipartite graph
        foreach (var (_, uvIds) in uvidByTexture)
        {
            // if the texture UV is not related to mesh, it's bad texture / UV
            if (uvIds.Any(x => x.UVChannel == UVChannel.NonMeshRelated))
            {
                badUvIds.UnionWith(uvIds);
                continue;
            }

            // if uvIDs for the textures using any of the uvIds is not equals to the uvIds, it's bad texture / UV
            if (uvIds
                .SelectMany(uvId => textureByUvId[uvId])
                .Select(inner => uvidByTexture[inner])
                .Any(innerUvIds => !uvIds.SequenceEqual(innerUvIds)))
            {
                badUvIds.UnionWith(uvIds);
                continue;
            }
        }

        // convert badUvIds to badTextures

        var badTextures = uvidByTexture
            .Where(x => x.Value.Any(badUvIds.Contains))
            .Select(x => x.Key).ToHashSet();

        var validTextures = info.Textures.Where(textureNode => !badTextures.Contains(textureNode));

        var textureUvIdsPairs = validTextures.Select(textureNode =>
            {
                var uvIds = textureNode.Users.SelectMany(pair =>
                    {
                        var (material, usages) = pair;
                        return usages.SelectMany(usage => material.Users.Select(userSubMesh => (usage, userSubMesh)))
                            .Select(pair => new UVID(pair.userSubMesh.SubMeshId, pair.usage.UVChannel));
                    })
                    .ToEqualsHashSet();

                return (uvIds, texture: textureNode);
            }).GroupBy(k => k.uvIds)
            .Select(grouping => (uvids: grouping.Key, textures: grouping.Select(x => x.texture).ToHashSet()));

        var atlasResults = new List<(EqualsHashSet<UVID>, AtlasResult)>();

        foreach (var (uvids, textureNodes) in textureUvIdsPairs)
        {
            var textures = textureNodes.Select(x => x.Texture).ToList();
            var atlasResult = MayAtlasTexture(textures, uvids.backedSet);

            if (atlasResult.IsEmpty()) continue;

            atlasResults.Add((uvids, atlasResult));
            foreach (var textureNode in textureNodes)
            {
                var newTexture = atlasResult.TextureMapping[textureNode.Texture];

                foreach (var (material, usages) in textureNode.Users)
                foreach (var usage in usages)
                    material.Material.SetTexture(usage.MaterialPropertyName, newTexture);
            }
        }

        return atlasResults;
    }

    internal void RemapVertexUvs(ICollection<(EqualsHashSet<UVID>, AtlasResult)> atlasResults)
    {
        {
            var used = new HashSet<Vertex>();

            // collect vertices used by non-atlas submeshes
            {
                var uvids = atlasResults.SelectMany(x => x.Item1.backedSet);
                var mergingSubMeshes = uvids.Select(x => x.MeshInfo2!.SubMeshes[x.SubMeshIndex]).ToHashSet();
                var meshes = uvids.Select(x => x.MeshInfo2!).Distinct();

                foreach (var mesh in meshes)
                foreach (var subMesh in mesh.SubMeshes)
                    if (!mergingSubMeshes.Contains(subMesh))
                        used.UnionWith(subMesh.Vertices);
            }

            foreach (var (uvids, result) in atlasResults)
            {
                var newVertexMap = new Dictionary<Vertex, Vertex>();

                foreach (var uvid in uvids.backedSet)
                {
                    var meshInfo2 = uvid.MeshInfo2!;
                    var submesh = meshInfo2.SubMeshes[uvid.SubMeshIndex];
                    for (var i = 0; i < submesh.Vertices.Count; i++)
                    {
                        var originalVertex = submesh.Vertices[i];
                        var newUVList = result.NewUVs[originalVertex];
                        
                        Vertex vertex;
                        if (newVertexMap.TryGetValue(originalVertex, out vertex))
                        {
                            // use cloned vertex
                        }
                        else
                        {
                            if (used.Add(originalVertex))
                            {
                                vertex = originalVertex;
                                newVertexMap.Add(originalVertex, vertex);
                            }
                            else
                            {
                                vertex = originalVertex.Clone();
                                newVertexMap.Add(originalVertex, vertex);
                                meshInfo2.Vertices.Add(vertex);
                                TraceLog("Duplicating vertex");
                            }

                            foreach (var (uvChannel, newUV) in newUVList)
                                vertex.SetTexCoord(uvChannel, newUV);
                        }
                        submesh.Vertices[i] = vertex;
                    }
                }
            }
        }
    }

    (bool safeToMerge, IEnumerable<Material> materials) GetAnimatedMaterialsForSubMesh(
        BuildContext context, Renderer renderer, int materialSlotIndex)
    {
        var component = context.GetAnimationComponent(renderer);

        var animation = component.GetObjectNode($"m_Materials.Array.data[{materialSlotIndex}]");

        if (animation.ComponentNodes.All(x => x is AnimatorPropModNode<ObjectValueInfo>))
        {
            var possibleValues = animation.Value.PossibleValues;

            if (possibleValues.All(x => x is Material))
                return (safeToMerge: true, materials: possibleValues.Cast<Material>());

            return (safeToMerge: false, materials: possibleValues.OfType<Material>());
        }
        else
        {
            var possibleValues = animation.Value.PossibleValues;
            return (safeToMerge: false, materials: possibleValues.OfType<Material>());
        }
    }

    class TextureUsageInformationCallbackImpl : TextureUsageInformationCallback
    {
        private readonly Material _material;
        private readonly List<AnimationComponentInfo<PropertyInfo>> _infos;
        private List<TextureUsageInformation>? _textureUsageInformations = new();

        public List<TextureUsageInformation>? TextureUsageInformations => _textureUsageInformations;

        public TextureUsageInformationCallbackImpl(Material material, List<AnimationComponentInfo<PropertyInfo>> infos)
        {
            _material = material;
            _infos = infos;
        }

        public Shader Shader => _material.shader;

        private T? GetValue<T>(string propertyName, Func<string, T> computer, bool considerAnimation) where T : struct
        {
            // animated; return null
            if (considerAnimation)
            {
                var animationProperty = $"material.{propertyName}";
                if (_infos.Any(x => x.GetFloatNode(animationProperty).ComponentNodes.Any()))
                    return null;
            }

            return computer(propertyName);
        }

        public override int? GetInteger(string propertyName, bool considerAnimation = true) => GetValue(propertyName, _material.GetInt, considerAnimation);

        public override float? GetFloat(string propertyName, bool considerAnimation = true) => GetValue(propertyName, _material.GetFloat, considerAnimation);

        public override Vector4? GetVector(string propertyName, bool considerAnimation = true) => GetValue(propertyName, _material.GetVector, considerAnimation);

        public override void RegisterOtherUVUsage(UsingUVChannels uvChannel)
        {
            // no longer atlasing is not supported
            _textureUsageInformations = null;
        }

        public override void RegisterTextureUVUsage(
            string textureMaterialPropertyName, 
            SamplerStateInformation samplerState,
            UsingUVChannels uvChannels, 
            Matrix2x3? uvMatrix)
        {
            if (_textureUsageInformations == null) return;
            UVChannel uvChannel;
            switch (uvChannels)
            {
                case UsingUVChannels.NonMesh:
                    uvChannel = UVChannel.NonMeshRelated;
                    break;
                case UsingUVChannels.UV0:
                    uvChannel = UVChannel.UV0;
                    break;
                case UsingUVChannels.UV1:
                    uvChannel = UVChannel.UV1;
                    break;
                case UsingUVChannels.UV2:
                    uvChannel = UVChannel.UV2;
                    break;
                case UsingUVChannels.UV3:
                    uvChannel = UVChannel.UV3;
                    break;
                case UsingUVChannels.UV4:
                    uvChannel = UVChannel.UV4;
                    break;
                case UsingUVChannels.UV5:
                    uvChannel = UVChannel.UV5;
                    break;
                case UsingUVChannels.UV6:
                    uvChannel = UVChannel.UV6;
                    break;
                case UsingUVChannels.UV7:
                    uvChannel = UVChannel.UV7;
                    break;
                default:
                    _textureUsageInformations = null;
                    return;
            }

            if (uvMatrix != Matrix2x3.Identity && uvChannel != UVChannel.NonMeshRelated) {
                _textureUsageInformations = null;
                return;
            }

            _textureUsageInformations?.Add(new TextureUsageInformation(textureMaterialPropertyName, uvChannel));
        }
    }

    internal class TextureUsageInformation
    {
        public string MaterialPropertyName { get; }
        public UVChannel UVChannel { get; }

        internal TextureUsageInformation(string materialPropertyName, UVChannel uvChannel)
        {
            MaterialPropertyName = materialPropertyName;
            UVChannel = uvChannel;
        }
    }
    
    public enum UVChannel
    {
        UV0 = 0,
        UV1 = 1,
        UV2 = 2,
        UV3 = 3,
        UV4 = 4,
        UV5 = 5,
        UV6 = 6,
        UV7 = 7,
        // For example, ScreenSpace (dither) or MatCap
        NonMeshRelated = 0x100 + 0,
    }

    [Conditional("AAO_OPTIMIZE_TEXTURE_TRACE_LOG")]
    private static void TraceLog(string message)
    {
        Debug.Log(message);
    }

    internal struct AtlasResult
    {
        public Dictionary<Texture2D, Texture2D> TextureMapping;
        public Dictionary<Vertex, List<(int uvChannel, Vector2 newUV)>> NewUVs;

        public AtlasResult(Dictionary<Texture2D, Texture2D> textureMapping, Dictionary<Vertex, List<(int uvChannel, Vector2 newUV)>> newUVs)
        {
            TextureMapping = textureMapping;
            NewUVs = newUVs;
        }

        public static AtlasResult Empty = new(new Dictionary<Texture2D, Texture2D>(),
            new Dictionary<Vertex, List<(int uvChannel, Vector2 newUV)>>());

        public bool IsEmpty() =>
            (TextureMapping == null || TextureMapping.Count == 0) &&
            (NewUVs == null || NewUVs.Count == 0);
    }

    AtlasResult MayAtlasTexture(ICollection<Texture2D> textures, ICollection<UVID> users)
    {
        if (users.Any(uvid => uvid.UVChannel == UVChannel.NonMeshRelated))
            return AtlasResult.Empty;

        if (CreateIslands(users) is not {} islands)
            return AtlasResult.Empty;

        MergeIslands(islands);

        if (ComputeBlockSize(textures) is not var (blockSizeRatioX, blockSizeRatioY, paddingRatio))
            return AtlasResult.Empty;

        FitToBlockSizeAndAddPadding(islands, blockSizeRatioX, blockSizeRatioY, paddingRatio);

        var atlasIslands = islands.Select(x => new AtlasIsland(x)).ToArray();
        Array.Sort(atlasIslands, (a, b) => b.Size.y.CompareTo(a.Size.y));

        if (AfterAtlasSizesSmallToBig(atlasIslands) is not {} atlasSizes)
        {
            TraceLog($"{string.Join(", ", textures)} will not merged because island size does not fit criteria");
            return AtlasResult.Empty;
        }

        foreach (var atlasSize in atlasSizes)
        {
            if (TryAtlasTexture(atlasIslands, atlasSize))
            {
                // Good News! We did it!
                TraceLog($"Good News! We did it!: {atlasSize}");
                return BuildAtlasResult(atlasIslands, atlasSize, textures, useBlockCopying: true);
            }
            else
            {
                TraceLog($"Failed to atlas with {atlasSize}");
            }
        }


        return AtlasResult.Empty;
    }

    private List<IslandUtility.Island>? CreateIslands(ICollection<UVID> users)
    {
        foreach (var user in users)
        {
            var submesh = user.MeshInfo2!.SubMeshes[user.SubMeshIndex];
            // currently non triangle topology is not supported
            if (submesh.Topology != MeshTopology.Triangles)
                return null;
            foreach (var vertex in submesh.Vertices)
            {
                var coord = vertex.GetTexCoord((int)user.UVChannel);

                // UV Tiling is currently not supported
                // TODO: if entire island is in n.0<=x<n+1, y.0<=y<n+1, then it might be safe to atlas (if TextureWrapMode is repeat)
                if (coord.x is not (>= 0 and < 1) || coord.y is not (>= 0 and < 1))
                    return null;
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

        return islands;
    }

    private void MergeIslands(List<IslandUtility.Island> islands)
    {
        // TODO: We may merge islands >N% wrapped (heuristic merge islands)
        // This mage can be after fitting to block size

        for (var i = 0; i < islands.Count; i++)
        {
            var islandI = islands[i];
            for (var j = 0; j < islands.Count; j++)
            {
                if (i == j) continue;

                var islandJ = islands[j];

                // if islandJ is completely inside islandI, merge islandJ to islandI
                if (islandI.MinPos.x <= islandJ.MinPos.x && islandJ.MaxPos.x <= islandI.MaxPos.x &&
                    islandI.MinPos.y <= islandJ.MinPos.y && islandJ.MaxPos.y <= islandI.MaxPos.y)
                {
                    islandI.triangles.AddRange(islandJ.triangles);
                    islands.RemoveAt(j);
                    j--;
                    if (j < i) i--;
                }
            }
        }
    }

    private (float blockSizeRatioX, float blockSizeRatioY, float paddingRatio)? ComputeBlockSize(ICollection<Texture2D> textures)
    {
        var maxResolution = -1;
        var minResolution = int.MaxValue;

        foreach (var texture2D in textures)
        {
            var width = texture2D.width;
            var height = texture2D.height;

            if (!width.IsPowerOfTwo() || !height.IsPowerOfTwo())
            {
                TraceLog($"{string.Join(", ", textures)} will not merged because {texture2D} is not power of two");
                return null;
            }

            maxResolution = Mathf.Max(maxResolution, width, height);
            minResolution = Mathf.Min(minResolution, width, height);
        }

        var xBlockSizeInMaxResolutionLCM = 1;
        var yBlockSizeInMaxResolutionLCM = 1;

        foreach (var texture2D in textures)
        {
            var width = texture2D.width;
            var height = texture2D.height;

            var xBlockSizeInMaxResolution =
                (int)(GraphicsFormatUtility.GetBlockWidth(texture2D.format) * (maxResolution / width));
            var yBlockSizeInMaxResolution =
                (int)(GraphicsFormatUtility.GetBlockHeight(texture2D.format) * (maxResolution / height));

            xBlockSizeInMaxResolutionLCM =
                Utils.LeastCommonMultiple(xBlockSizeInMaxResolutionLCM, xBlockSizeInMaxResolution);
            yBlockSizeInMaxResolutionLCM =
                Utils.LeastCommonMultiple(yBlockSizeInMaxResolutionLCM, yBlockSizeInMaxResolution);
        }

        // padding is at least 4px with max resolution, 1px in min resolution
        var minResolutionPixelSizeInMaxResolution = maxResolution / minResolution;
        var paddingSize = Mathf.Max(minResolutionPixelSizeInMaxResolution, maxResolution / 100);

        if (minResolution <= paddingSize || maxResolution <= paddingSize)
        {
            TraceLog(
                $"{string.Join(", ", textures)} will not merged because min resolution is less than {paddingSize} ({minResolution})");
            return null;
        }

        var blockSizeX = Utils.LeastCommonMultiple(xBlockSizeInMaxResolutionLCM, minResolutionPixelSizeInMaxResolution);
        var blockSizeY = Utils.LeastCommonMultiple(yBlockSizeInMaxResolutionLCM, minResolutionPixelSizeInMaxResolution);

        var blockSizeRatioX = (float)blockSizeX / maxResolution;
        var blockSizeRatioY = (float)blockSizeY / maxResolution;
        var paddingRatio = (float)paddingSize / maxResolution;

        TraceLog($"blockSizeX: {blockSizeX}, blockSizeY: {blockSizeY}");

        return (blockSizeRatioX, blockSizeRatioY, paddingRatio);
    }

    private void FitToBlockSizeAndAddPadding(List<IslandUtility.Island> islands, float blockSizeRatioX, float blockSizeRatioY, float paddingRatio)
    {
        foreach (var island in islands)
        {
            ref var min = ref island.MinPos;
            ref var max = ref island.MaxPos;

            // fit to block size
            min.x = Mathf.Floor(min.x / blockSizeRatioX - paddingRatio) * blockSizeRatioX;
            min.y = Mathf.Floor(min.y / blockSizeRatioY - paddingRatio) * blockSizeRatioY;
            max.x = Mathf.Ceil(max.x / blockSizeRatioX + paddingRatio) * blockSizeRatioX;
            max.y = Mathf.Ceil(max.y / blockSizeRatioY + paddingRatio) * blockSizeRatioY;
        }
    }

    private static Material? _helperMaterial;

    private static Material HelperMaterial =>
        _helperMaterial != null ? _helperMaterial : _helperMaterial = new Material(Assets.MergeTextureHelper);
    private static readonly int MainTexProp = Shader.PropertyToID("_MainTex");
    private static readonly int RectProp = Shader.PropertyToID("_Rect");
    private static readonly int SrcRectProp = Shader.PropertyToID("_SrcRect");
    private static readonly int NoClipProp = Shader.PropertyToID("_NoClip");

    private AtlasResult BuildAtlasResult(AtlasIsland[] atlasIslands, Vector2 atlasSize, ICollection<Texture2D> textures, bool useBlockCopying = false)
    {
        var textureMapping = new Dictionary<Texture2D, Texture2D>();

        foreach (var texture2D in textures)
        {
            var newWidth = (int)(atlasSize.x * texture2D.width);
            var newHeight = (int)(atlasSize.y * texture2D.height);
            Texture2D newTexture;

            var mipmapCount = Mathf.Min(Utils.MostSignificantBit(Mathf.Min(newWidth, newHeight)), texture2D.mipmapCount);
            // Crunch compressed texture will return empty array for `GetRawTextureData`
            if (useBlockCopying && GraphicsFormatUtility.IsCompressedFormat(texture2D.format) && !GraphicsFormatUtility.IsCrunchFormat(texture2D.format) && mipmapCount == 1)
            {
                var destMipmapSize = GraphicsFormatUtility.ComputeMipmapSize(newWidth, newHeight, texture2D.format);
                var sourceMipmapSize = GraphicsFormatUtility.ComputeMipmapSize(texture2D.width, texture2D.height, texture2D.format);

                Texture2D readableVersion;
                if (texture2D.isReadable)
                {
                    readableVersion = texture2D;
                }
                else
                {
                    readableVersion = new Texture2D(texture2D.width, texture2D.height, texture2D.format, texture2D.mipmapCount, !texture2D.isDataSRGB);
                    Graphics.CopyTexture(texture2D, readableVersion);
                    readableVersion.Apply(false);
                }
                var sourceTextureData = readableVersion.GetRawTextureData<byte>();
                var sourceTextureDataSpan = sourceTextureData.AsSpan().Slice(0, (int)sourceMipmapSize);

                var destTextureData = new byte[(int)destMipmapSize];
                var destTextureDataSpan = destTextureData.AsSpan();

                TraceLog($"MipmapSize for {newWidth}x{newHeight} is {destMipmapSize} and data is {sourceTextureData.Length}");

                var blockWidth = (int)GraphicsFormatUtility.GetBlockWidth(texture2D.format);
                var blockHeight = (int)GraphicsFormatUtility.GetBlockHeight(texture2D.format);
                var blockSize = (int)GraphicsFormatUtility.GetBlockSize(texture2D.format);

                var destTextureBlockStride = (newWidth + blockWidth - 1) / blockWidth * blockSize;
                var sourceTextureBlockStride = (texture2D.width + blockWidth - 1) / blockWidth * blockSize;

                foreach (var atlasIsland in atlasIslands)
                {
                    var xPixelCount = (int)(atlasIsland.Size.x * texture2D.width);
                    var yPixelCount = (int)(atlasIsland.Size.y * texture2D.height);
                    // in most cases same as xPixelCount / blockWidth but if block size is not 2^n, it may be different
                    var xBlockCount = (xPixelCount + blockWidth - 1) / blockWidth;
                    var yBlockCount = (yPixelCount + blockHeight - 1) / blockHeight;

                    var sourceXPixelPosition = (int)(atlasIsland.OriginalIsland.MinPos.x * texture2D.width);
                    var sourceYPixelPosition = (int)(atlasIsland.OriginalIsland.MinPos.y * texture2D.height);
                    var sourceXBlockPosition = sourceXPixelPosition / blockWidth;
                    var sourceYBlockPosition = sourceYPixelPosition / blockHeight;

                    var destXPixelPosition = (int)(atlasIsland.Pivot.x * texture2D.width);
                    var destYPixelPosition = (int)(atlasIsland.Pivot.y * texture2D.height);
                    var destXBlockPosition = destXPixelPosition / blockWidth;
                    var destYBlockPosition = destYPixelPosition / blockHeight;

                    var xBlockByteCount = xBlockCount * blockSize;
                    for (var y = 0; y < yBlockCount; y++)
                    {
                        var sourceY = sourceYBlockPosition + y;
                        var destY = destYBlockPosition + y;

                        var sourceSpan = sourceTextureDataSpan.Slice(sourceY * sourceTextureBlockStride + sourceXBlockPosition * blockSize, xBlockByteCount);
                        var destSpan = destTextureDataSpan.Slice(destY * destTextureBlockStride + destXBlockPosition * blockSize, xBlockByteCount);

                        sourceSpan.CopyTo(destSpan);
                    }
                }

                newTexture = new Texture2D(newWidth, newHeight, texture2D.format, mipmapCount, !texture2D.isDataSRGB);
                newTexture.SetPixelData(destTextureData, 0);
                newTexture.Apply(true, !texture2D.isReadable);
            }
            else
            {
                var format = SystemInfo.GetCompatibleFormat(
                    GraphicsFormatUtility.GetGraphicsFormat(texture2D.format, isSRGB: texture2D.isDataSRGB),
                    FormatUsage.Render);
                TraceLog($"Using format {format} ({texture2D.format})");
                using var tempTexture = Utils.TemporaryRenderTexture(newWidth, newHeight, depthBuffer: 0, format: format);
                HelperMaterial.SetTexture(MainTexProp, texture2D);
                HelperMaterial.SetInt(NoClipProp, 1);

                bool isBlack = true;

                foreach (var atlasIsland in atlasIslands)
                {
                    HelperMaterial.SetVector(SrcRectProp,
                        new Vector4(atlasIsland.OriginalIsland.MinPos.x, atlasIsland.OriginalIsland.MinPos.y,
                            atlasIsland.OriginalIsland.Size.x, atlasIsland.OriginalIsland.Size.y));

                    var pivot = atlasIsland.Pivot / atlasSize;
                    var size = atlasIsland.Size / atlasSize;

                    HelperMaterial.SetVector(RectProp, new Vector4(pivot.x, pivot.y, size.x, size.y));

                    Graphics.Blit(texture2D, tempTexture.RenderTexture, HelperMaterial);
                }

                newTexture = CopyFromRenderTarget(tempTexture.RenderTexture, texture2D);

                if (IsSingleColor(newTexture, atlasIslands, atlasSize, out var color))
                {
                    // if color is consist of 0 or 1, isSrgb not matters so assume it's linear
                    var isSrgb = texture2D.isDataSRGB && color is not { r: 0 or 1, g: 0 or 1, b: 0 or 1 };

                    if (ColorTextures.TryGetValue((color, isSrgb), out var cachedTexture))
                        newTexture = cachedTexture;
                    else
                    {
                        newTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, !isSrgb);
                        newTexture.SetPixel(0, 0, color);
                        newTexture.Apply(false, true);
                        newTexture.name = $"AAO Monotone {color} {(isSrgb ? "sRGB" : "Linear")}";
                        ColorTextures.Add((color, isSrgb), newTexture);
                    }
                }
                else
                {
                    if (GraphicsFormatUtility.IsCompressedFormat(texture2D.format))
                        EditorUtility.CompressTexture(newTexture, texture2D.format, TextureCompressionQuality.Normal);
                    newTexture.name = texture2D.name + " (AAO UV Packed)";
                    newTexture.Apply(true, !texture2D.isReadable);
                }
            }

            textureMapping.Add(texture2D, newTexture);
        }

        var newUVs = new Dictionary<Vertex, List<(int uvChannel, Vector2 newUV)>>();

        foreach (var atlasIsland in atlasIslands)
        foreach (var triangle in atlasIsland.OriginalIsland.triangles)
        foreach (var vertex in triangle)
        {
            var uv = (Vector2)vertex.GetTexCoord(triangle.UVIndex);

            uv -= atlasIsland.OriginalIsland.MinPos;
            uv += atlasIsland.Pivot;
            uv /= atlasSize;

            if (!newUVs.TryGetValue(vertex, out var newUVList))
                newUVs.Add(vertex, newUVList = new List<(int uvChannel, Vector2 newUV)>());
            newUVList.Add((triangle.UVIndex, uv));
        }

        return new AtlasResult(textureMapping, newUVs);
    }

    private static bool IsSingleColor(Texture2D texture, AtlasIsland[] islands, Vector2 atlasSize, out Color color)
    {
        Color? commonColor = null;

        var texSize = new Vector2(texture.width, texture.height);

        var colors = texture.GetPixels();

        foreach (var atlasIsland in islands)
        {
            var pivot = atlasIsland.Pivot / atlasSize * texSize;
            var pivotInt = new Vector2Int(Mathf.FloorToInt(pivot.x), Mathf.FloorToInt(pivot.y));
            var size = atlasIsland.Size / atlasSize * texSize;
            var sizeInt = new Vector2Int(Mathf.CeilToInt(size.x), Mathf.CeilToInt(size.y));

            for (var dy = 0; dy < sizeInt.y; dy++)
            for (var dx = 0; dx < sizeInt.x; dx++)
            {
                var y = pivotInt.y + dy;
                var x = pivotInt.x + dx;
                var colorAt = colors[y * texture.width + x];

                if (commonColor is not { } c)
                {
                    commonColor = colorAt;
                }
                else if (c != colorAt)
                {
                    color = default;
                    return false;
                }
            }
        }

        color = commonColor ?? default;
        return true;
    }

    private static Texture2D CopyFromRenderTarget(RenderTexture source, Texture2D original)
    {
        var prev = RenderTexture.active;
        var format = SystemInfo.GetCompatibleFormat(original.graphicsFormat, FormatUsage.ReadPixels);
        var textureFormat = GraphicsFormatUtility.GetTextureFormat(format);
        var texture = new Texture2D(source.width, source.height, textureFormat, true, linear: !source.isDataSRGB);

        try
        {
            RenderTexture.active = source;
            texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            texture.Apply();
        }
        finally
        {
            RenderTexture.active = prev;
        }

        return texture;
    }

    static IEnumerable<Vector2>? AfterAtlasSizesSmallToBig(AtlasIsland[] islands)
    {
        // Check for island size before trying to atlas
        var totalIslandSize = islands.Sum(x => x.Size.x * x.Size.y);
        if (totalIslandSize >= 0.5) return null;

        var maxIslandLength = islands.Max(x => Mathf.Max(x.Size.x, x.Size.y));
        if (maxIslandLength >= 0.5) return null;

        var maxIslandSizeX = islands.Max(x => x.Size.x);
        var maxIslandSizeY = islands.Max(x => x.Size.y);

        TraceLog($"Starting Atlas with maxX: {maxIslandSizeX}, maxY: {maxIslandSizeY}");

        return AfterAtlasSizesSmallToBigGenerator(totalIslandSize, new Vector2(maxIslandSizeX, maxIslandSizeY));
    }

    internal static IEnumerable<Vector2> AfterAtlasSizesSmallToBigGenerator(float useRatio, Vector2 maxIslandSize)
    {
        var maxHalfCount = 0;
        {
            var currentSize = 1f;
            while (currentSize > useRatio)
            {
                maxHalfCount++;
                currentSize /= 2;
            }
        }

        var minXSize = Utils.MinPowerOfTwoGreaterThan(maxIslandSize.x);
        var minYSize = Utils.MinPowerOfTwoGreaterThan(maxIslandSize.y);

        for (var halfCount = maxHalfCount; halfCount >= 0; halfCount--)
        {
            var size = 1f / (1 << halfCount);

            for (var xSize = minXSize; xSize <= 1; xSize *= 2)
            {
                var ySize = size / xSize;
                if (ySize < minYSize) break;
                if (ySize > 1) continue;
                if (ySize >= 1 && xSize >= 1) break;

                yield return new Vector2(xSize, ySize);
            }
        }
    }

    // expecting y size sorted
    static bool TryAtlasTexture(AtlasIsland[] islands, Vector2 size)
    {
        var done = new bool[islands.Length];
        var doneCount = 0;

        var yCursor = 0f;

        while (true)
        {
            var firstNotFinished = Array.FindIndex(done, x => !x);

            // this means all islands are finished
            if (firstNotFinished == -1) break;

            var firstNotFinishedIsland = islands[firstNotFinished];

            if (yCursor + firstNotFinishedIsland.Size.y > size.y)
                return false; // failed to atlas


            var xCursor = 0f;

            firstNotFinishedIsland.Pivot = new Vector2(xCursor, yCursor);
            xCursor += firstNotFinishedIsland.Size.x;
            done[firstNotFinished] = true;
            doneCount++;

            for (var i = firstNotFinished + 1; i < islands.Length; i++)
            {
                if (done[i]) continue;

                var island = islands[i];
                if (xCursor + island.Size.x > size.x) continue;

                island.Pivot = new Vector2(xCursor, yCursor);
                xCursor += island.Size.x;
                done[i] = true;
                doneCount++;
            }

            yCursor += firstNotFinishedIsland.Size.y;
        }

        // all islands are placed
        return true;
    }

    internal class AtlasIsland
    {
        //TODO: rotate
        public IslandUtility.Island OriginalIsland;
        public Vector2 Pivot;

        public Vector2 Size => OriginalIsland.Size;

        public AtlasIsland(IslandUtility.Island originalIsland)
        {
            OriginalIsland = originalIsland;
        }
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

            public Vector2 Size => MaxPos - MinPos;

            public Island(Island source)
            {
                triangles = new List<Triangle>(source.triangles);
                MinPos = source.MinPos;
                MaxPos = source.MaxPos;
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
