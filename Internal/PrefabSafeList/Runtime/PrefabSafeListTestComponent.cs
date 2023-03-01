using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeList
{
    public class PrefabSafeListTestComponent : MonoBehaviour
    {
        [SerializeField] internal MaterialsSet materials;

        public PrefabSafeListTestComponent()
        {
            materials = new MaterialsSet(this);
        }

        [Serializable]
        internal class MaterialsSet : PrefabSafeList.PrefabSafeList<Material, MaterialsLayer, MaterialContainer>
        {
            public MaterialsSet(Object outerObject) : base(outerObject)
            {
            }
        }

        [Serializable]
        internal class MaterialsLayer : PrefabSafeList.PrefabLayer<Material, MaterialContainer>
        {
        }

        [Serializable]
        internal class MaterialContainer : PrefabSafeList.ValueContainer<Material>
        {
        }
    }
}
