using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Rename BlendShape")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/rename-blend-shape/")]
    internal class RenameBlendShape : EditSkinnedMeshComponent, ISerializationCallbackReceiver
    {
        [SerializeField] internal PrefabSafeMap.PrefabSafeMap<string, string> nameMap;

        public RenameBlendShape()
        {
            nameMap = new PrefabSafeMap.PrefabSafeMap<string, string>(this);
        }

        private void ValidatePSUC()
        {
            PrefabSafeMap.PrefabSafeMap.OnValidate(this, x => x.nameMap);
        }

        private void OnValidate() => ValidatePSUC();
        void ISerializationCallbackReceiver.OnBeforeSerialize() => ValidatePSUC();

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }
    }
}
