using UnityEngine;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    public class PrefabSafeSetTestComponent : MonoBehaviour
    {
        [SerializeField] internal PrefabSafeSet<Material> materials;

        public PrefabSafeSetTestComponent()
        {
            materials = new PrefabSafeSet<Material>(this);
        }

        private void OnValidate()
        {
            PrefabSafeSet.OnValidate(this, x => x.materials);
        }
    }
}
