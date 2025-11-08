using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Freeze BlendShapes")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/freeze-blendshape/")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    internal class FreezeBlendShape : EditSkinnedMeshComponent, ISerializationCallbackReceiver
    {
        public PrefabSafeSet.PrefabSafeSet<string> shapeKeysSet;

        public FreezeBlendShape()
        {
            shapeKeysSet = new PrefabSafeSet.PrefabSafeSet<string>(this);
        }

        public HashSet<string> FreezingShapeKeys => shapeKeysSet.GetAsSet();

        private void ValidatePSUC()
        {
            PrefabSafeSet.PrefabSafeSet.OnValidate(this, x => x.shapeKeysSet);
        }

        private void OnValidate() => ValidatePSUC();
        void ISerializationCallbackReceiver.OnBeforeSerialize() => ValidatePSUC();

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }
    }
}
