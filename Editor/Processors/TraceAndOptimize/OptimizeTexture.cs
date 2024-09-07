using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;

internal class OptimizeTexture : TraceAndOptimizePass<OptimizeTexture>
{
    public override string DisplayName => "T&O: OptimizeTexture";

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
        {
            var usageInformations = new Dictionary<Material, ShaderKnowledge.TextureUsageInformation[]>();

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

        foreach (var (material, value) in materialUsers)
        {
            Debug.Log($"material: {material.name} users: {string.Join(", ", value.Select(x => x.MeshInfo2.SourceRenderer.name))}", material);
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
}
