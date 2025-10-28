using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.Runtime
{
    public class PrefabSafeSetComponent : MonoBehaviour, ISerializationCallbackReceiver
    {
        public PrefabSafeSet.PrefabSafeSet<string> stringSet;

        public PrefabSafeSetComponent()
        {
            stringSet = new PrefabSafeSet.PrefabSafeSet<string>(this);
        }

        private void ValidatePSUC()
        {
            PrefabSafeSet.PrefabSafeSet.OnValidate(this, x => x.stringSet);
        }

        private void OnValidate() => ValidatePSUC();
        void ISerializationCallbackReceiver.OnBeforeSerialize() => ValidatePSUC();

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }
    }
}
