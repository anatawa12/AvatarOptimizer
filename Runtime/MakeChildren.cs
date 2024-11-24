using UnityEngine;
using UnityEngine.Animations;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Make Children")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/make-children/")]
    internal class MakeChildren : AvatarTagComponent, ISerializationCallbackReceiver
    {
        [NotKeyable, AAOLocalized("MakeChildren:prop:executeEarly", "MakeChildren:tooltip:executeEarly")]
        public bool executeEarly;
        [NotKeyable, AAOLocalized("MakeChildren:prop:children")]
        public PrefabSafeSet.PrefabSafeSet<Transform> children;

        internal MakeChildren()
        {
            children = new PrefabSafeSet.PrefabSafeSet<Transform>(this);
        }

        private void ValidatePSUC()
        {
            PrefabSafeSet.PrefabSafeSet.OnValidate(this, x => x.children);
        }

        private void OnValidate() => ValidatePSUC();
        void ISerializationCallbackReceiver.OnBeforeSerialize() => ValidatePSUC();

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }
    }
}
