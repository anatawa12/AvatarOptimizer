using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
#pragma warning disable CS0618
    class UnusedBonesByReferencesToolEarlyProcessor : Pass<UnusedBonesByReferencesToolEarlyProcessor>
    {
        public override string DisplayName => "Early: UnusedBonesByReference";

        protected override void Execute(BuildContext context)
        {
            var configuration = context.AvatarRootObject.GetComponent<UnusedBonesByReferencesTool>();
            if (!configuration) return;

            using (ErrorReport.WithContextObject(configuration))
            {
                UnusedBonesByReferences.Make(BoneReference.Make(configuration.transform,
                        configuration.detectExtraChild), configuration.preserveEndBone)
                    .SetEditorOnlyToBones();
            }
        }

        #region UnusedBonesByReferencesTool
        // This region contains source code copied from UnusedBonesByReferencesTool
        // which is published at https://narazaka.booth.pm/items/3831781 under zlib license.
        // Copyright (c) 2022 Narazaka
        //
        // This software is provided 'as-is', without any express or implied
        // warranty. In no event will the authors be held liable for any damages
        // arising from the use of this software.
        //
        // Permission is granted to anyone to use this software for any purpose,
        // including commercial applications, and to alter it and redistribute it
        // freely, subject to the following restrictions:
        //
        // 1. The origin of this software must not be misrepresented; you must not
        // claim that you wrote the original software. If you use this software
        // in a product, an acknowledgment in the product documentation would be
        // appreciated but is not required.
        //
        // 2. Altered source versions must be plainly marked as such, and must not be
        // misrepresented as being the original software.
        //
        // 3. This notice may not be removed or altered from any source
        // distribution.

        public class BoneReference
        {
            const string EditorOnlyTag = "EditorOnly";
            static Regex EndBoneRe = new Regex("end$", RegexOptions.IgnoreCase);

            public static List<BoneReference> Make(Transform root, bool detectExtraChild = false)
            {
                var boneHierarchy = new Dictionary<Transform, BoneReference>();
                var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var renderer in renderers)
                {
                    if (renderer.bones == null) continue;
                    if (renderer.sharedMesh == null) continue;
                    var weights = renderer.sharedMesh.GetAllBoneWeights();
                    var boneIndexes = weights.Where(w => w.weight > 0).Select(w => w.boneIndex).ToArray();
                    foreach (var boneIndex in boneIndexes)
                    {
                        var bone = renderer.bones[boneIndex];
                        if (bone == null) continue;
                        if (boneHierarchy.TryGetValue(bone, out var info))
                        {
                            info.References.Add(renderer.transform);
                        }
                        else
                        {
                            boneHierarchy.Add(bone,
                                new BoneReference(bone) { References = new HashSet<Transform> { renderer.transform } });
                        }
                    }
                }

                foreach (var bone in boneHierarchy.Keys)
                {
                    var info = boneHierarchy[bone];
                    if (detectExtraChild)
                    {
                        for (var i = 0; i < bone.childCount; i++)
                        {
                            var child = bone.GetChild(i);
                            if (!boneHierarchy.ContainsKey(child))
                            {
                                info.HasExtraChild = true;
                                break;
                            }
                        }
                    }

                    var parent = bone.parent;
                    while (parent != root)
                    {
                        info.Parents.Add(parent);
                        parent = parent.parent;
                    }

                    info.Parents.Reverse();
                }

                return boneHierarchy.Values.ToList();
            }

            public Transform Bone;
            public List<Transform> Parents = new List<Transform>();
            public HashSet<Transform> References = new HashSet<Transform>();
            public bool HasExtraChild = false;

            public BoneReference(Transform bone)
            {
                Bone = bone;
            }

            public bool ReferencesAllEditorOnly
            {
                get => References.All(t => t.CompareTag(EditorOnlyTag));
            }

            public bool IsEnd
            {
                get => EndBoneRe.IsMatch(Bone.name);
            }

            public string BonePath
            {
                get => $"{BoneParentPath}/{Bone.name}";
            }

            public string BoneParentPath
            {
                get => string.Join("/", Parents.Select(b => b.name));
            }
        }

        public class UnusedBonesByReferences
        {
            public IList<BoneReference> BoneReferences { get; set; } = null!; // Initialized later
            public bool PreserveEndBone { get; set; }

            public HashSet<Transform> UnusedBones { get; private set; } = null!; // Initialized later
            public HashSet<Transform> DisabledBones { get; private set; } = null!; // Initialized later
            public HashSet<Transform> ForceEnabledBones { get; private set; } = null!; // Initialized later

            public static UnusedBonesByReferences Make(IList<BoneReference> boneReferences,
                bool preserveEndBone = false)
            {
                var info = new UnusedBonesByReferences
                    { BoneReferences = boneReferences, PreserveEndBone = preserveEndBone };
                info.DetectUnusedBones();
                return info;
            }

            void DetectUnusedBones()
            {
                DisabledBones = new HashSet<Transform>();

                // 全ての参照オブジェクトがEditorOnlyならボーンもEditorOnly
                foreach (var boneReference in BoneReferences)
                {
                    if (boneReference.ReferencesAllEditorOnly)
                    {
                        DisabledBones.Add(boneReference.Bone);
                    }
                }

                ForceEnabledBones = new HashSet<Transform>();
                foreach (var boneReference in BoneReferences)
                {
                    var active = !DisabledBones.Contains(boneReference.Bone);
                    if (!active && boneReference.HasExtraChild) // ボーン以外の子があったらactive
                    {
                        ForceEnabledBones.Add(boneReference.Bone);
                        active = true;
                    }

                    if (active) // 親は全てactive
                    {
                        ForceEnabledBones.UnionWith(boneReference.Parents);
                    }
                }

                UnusedBones = new HashSet<Transform>(DisabledBones);

                UnusedBones.ExceptWith(ForceEnabledBones);

                if (PreserveEndBone)
                {
                    // 親がactiveなEndボーンはactive
                    foreach (var boneReference in BoneReferences)
                    {
                        if (boneReference.IsEnd && !UnusedBones.Contains(boneReference.Parents.LastOrDefault()))
                        {
                            ForceEnabledBones.Add(boneReference.Bone);
                        }
                    }
                }

                UnusedBones.ExceptWith(ForceEnabledBones);
            }

            public void SetEditorOnlyToBones()
            {

                foreach (var boneReference in BoneReferences)
                {
                    var disabled = UnusedBones.Contains(boneReference.Bone);
                    var currentDisabled = boneReference.Bone.CompareTag("EditorOnly");
                    if (currentDisabled != disabled)
                    {
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(boneReference.Bone);
#endif
                        boneReference.Bone.tag = disabled ? "EditorOnly" : "Untagged";
                    }
                }
            }

            public void PrintDebug()
            {
                foreach (var boneReference in BoneReferences.OrderBy(p => p.BonePath))
                {
                    var d = DisabledBones.Contains(boneReference.Bone);
                    var f = ForceEnabledBones.Contains(boneReference.Bone);
                    Debug.Log(
                        $"[{(UnusedBones.Contains(boneReference.Bone) ? "OFF" : "ON")}] [{boneReference.Bone.name}] [{boneReference.BonePath}] D={d} F={f} || {string.Join(",", boneReference.References.Select(t => t.name))}");
                }
            }
        }

        #endregion
    }
#pragma warning restore CS0618
}
