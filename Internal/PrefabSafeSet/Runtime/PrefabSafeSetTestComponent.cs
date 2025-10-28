using UnityEngine;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    public class PrefabSafeSetTestComponent : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField] internal PrefabSafeSet<Material> materials;

        public PrefabSafeSetTestComponent()
        {
            materials = new PrefabSafeSet<Material>(this);
        }

        private void ValidatePSUC()
        {
            PrefabSafeSet.OnValidate(this, x => x.materials);
        }

        private void OnValidate() => ValidatePSUC();
        void ISerializationCallbackReceiver.OnBeforeSerialize() => ValidatePSUC();

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }
    }
}
