using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    public class PrefabSafeSetTestComponent : MonoBehaviour
    {
        [SerializeField] internal MaterialsSet materials;

        public PrefabSafeSetTestComponent()
        {
            materials = new MaterialsSet(this);
        }

        [Serializable]
        internal class MaterialsSet : PrefabSafeSet.PrefabSafeSet<Material>
        {
            public MaterialsSet(Object outerObject) : base(outerObject)
            {
            }
        }

        [Serializable]
        internal class MaterialsLayer : PrefabSafeSet.PrefabLayer<Material>
        {
        }
    }
}
