using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace Anatawa12.AvatarOptimizer.Processors
{
    partial class AutomaticConfigurationProcessor
    {
        private void GatherAnimationModifications()
        {
            foreach (var animator in _session.GetComponents<Animator>())
            {
                GatherAnimationModificationsInController(animator.gameObject, animator.runtimeAnimatorController);
            }

            var descriptor = _session.GetRootComponent<VRCAvatarDescriptor>();

            if (descriptor)
            {
                foreach (var layer in descriptor.specialAnimationLayers)
                {
                    GatherAnimationModificationsInController(descriptor.gameObject, layer.animatorController);
                }

                if (descriptor.customizeAnimationLayers)
                {
                    foreach (var layer in descriptor.baseAnimationLayers)
                    {
                        GatherAnimationModificationsInController(descriptor.gameObject, layer.animatorController);
                    }
                }

                switch (descriptor.lipSync)
                {
                    // AvatarDescriptorから収集
                    case VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape when descriptor.VisemeSkinnedMesh != null:
                    {
                        var skinnedMeshRenderer = descriptor.VisemeSkinnedMesh;
                        if (!_modifiedProperties.TryGetValue(skinnedMeshRenderer, out var set))
                            _modifiedProperties.Add(skinnedMeshRenderer, set = new HashSet<string>());
                        set.UnionWith(descriptor.VisemeBlendShapes.Select(x => $"blendShapes.{x}"));
                        break;
                    }
                    case VRC_AvatarDescriptor.LipSyncStyle.JawFlapBlendShape when descriptor.VisemeSkinnedMesh != null:
                    {
                        var skinnedMeshRenderer = descriptor.VisemeSkinnedMesh;
                        var shape = descriptor.MouthOpenBlendShapeName;

                        if (!_modifiedProperties.TryGetValue(skinnedMeshRenderer, out var set))
                            _modifiedProperties.Add(skinnedMeshRenderer, set = new HashSet<string>());
                        set.Add($"blendShapes.{shape}");
                        break;
                    }
                }

                if (
                    descriptor.customEyeLookSettings.eyelidType == VRCAvatarDescriptor.EyelidType.Blendshapes &&
                    descriptor.customEyeLookSettings.eyelidsSkinnedMesh != null
                )
                {
                    var skinnedMeshRenderer = descriptor.customEyeLookSettings.eyelidsSkinnedMesh;
                    var mesh = skinnedMeshRenderer.sharedMesh;

                    if (!_modifiedProperties.TryGetValue(skinnedMeshRenderer, out var set))
                        _modifiedProperties.Add(skinnedMeshRenderer, set = new HashSet<string>());

                    set.UnionWith(
                        from index in descriptor.customEyeLookSettings.eyelidsBlendshapes
                        where 0 <= index && index < mesh.blendShapeCount
                        let name = mesh.GetBlendShapeName(index)
                        select $"blendShapes.{name}");
                }

                var bodySkinnedMesh = descriptor.transform.Find("Body")?.GetComponent<SkinnedMeshRenderer>();

                if (bodySkinnedMesh)
                {
                    if (!_modifiedProperties.TryGetValue(bodySkinnedMesh, out var set))
                        _modifiedProperties.Add(bodySkinnedMesh, set = new HashSet<string>());

                    set.UnionWith(new[]
                    {
                        // https://booth.pm/ja/items/3341221
                        // https://images-wixmp-ed30a86b8c4ca887773594c2.wixmp.com/i/0b7b5e4b-c62e-41f7-8ced-1f3e58c4f5bf/d5nbmvp-5779f5ac-d476-426c-8ee6-2111eff8e76c.png
                        "まばたき",
                        "笑い",
                        "ウィンク",
                        "ウィンク右",
                        "ウィンク２",
                        "ｳｨﾝｸ２右",
                        "なごみ",
                        "はぅ",
                        "びっくり",
                        "じと目",
                        "ｷﾘｯ",
                        "はちゅ目",

                        "星目",
                        "はぁと",
                        "瞳小",
                        "瞳縦潰れ",
                        "光下",
                        "恐ろしい子！",
                        "ハイライト消",
                        "映り込み消",
                        "喜び",
                        "わぉ!?",
                        "なごみω",
                        "悲しむ",
                        "敵意",

                        "あ",
                        "い",
                        "う",
                        "え",
                        "お",
                        "あ２",
                        "ん",
                        "▲",
                        "∧",
                        "□",
                        "ワ",
                        "ω",

                        "ω□",
                        "にやり",
                        "にやり２",
                        "にっこり",
                        "ぺろっ",
                        "てへぺろ",
                        "てへぺろ２",
                        "口角上げ",
                        "口角下げ",
                        "口横広げ",
                        "歯無し上",
                        "歯無し下",

                        "真面目",
                        "困る",
                        "にこり",
                        "怒り",
                        "下",
                        "上",

                        // english
                        "Blink",
                        "Smile",
                        "Wink",
                        "Wink-a",
                        "Wink-b",
                        "Wink-c",
                        "Howawa",
                        "> <",
                        "Ha!!!",
                        "Jito-eye",
                        "Kiri-eye",
                        "O O",

                        "EyeStar",
                        "EyeHeart",
                        "EyeSmall",
                        "EyeSmall-v",
                        "EyeUnderli",
                        "EyeFunky",
                        "EyHi-Off",
                        "EyeRef-off",
                        "Joy",
                        "Wao!?",
                        "Howawa ω",
                        "Wail",
                        "Hostility",

                        "a",
                        "i",
                        "u",
                        "e",
                        "o",
                        "a 2",
                        "n",
                        "Mouse_1",
                        "Mouse_2",
                        //"□",
                        "Wa",
                        "Omega",

                        // "ω□",
                        "Niyari",
                        "Niyari2",
                        "Smile",
                        "Pero",
                        "Bero-tehe",
                        "Bero-tehe2",
                        "MouseUP",
                        "MouseDW",
                        "MouseWD",
                        "ToothAnon",
                        "ToothBnon",

                        "Serious",
                        "Trouble",
                        "Smily",
                        "Get angry",
                        "Up",
                        "Down",

                        // https://gist.github.com/lilxyzw/80608d9b16bf3458c61dec6b090805c5
                        "しいたけ",
                        "照れ",
                        "涙",
                    });
                }
            }
        }

        private void GatherAnimationModificationsInController(GameObject root, RuntimeAnimatorController controller)
        {
            if (controller == null) return;
            foreach (var clip in controller.animationClips)
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (!typeof(Component).IsAssignableFrom(binding.type)) continue;
                    var obj = (Component)AnimationUtility.GetAnimatedObject(root, binding);

                    if (!_modifiedProperties.TryGetValue(obj, out var set))
                        _modifiedProperties.Add(obj, set = new HashSet<string>());
                    set.Add(binding.propertyName);
                }
            }
        }
    }
}