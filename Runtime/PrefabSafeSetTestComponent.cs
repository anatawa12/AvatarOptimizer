using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    public class PrefabSafeSetTestComponent : MonoBehaviour
    {
        public MaterialsSet materials;

        public PrefabSafeSetTestComponent()
        {
            materials = new MaterialsSet(this);
        }

        [Serializable]
        public class MaterialsSet : PrefabSafeSet.Objects<Material, MaterialsLayer>
        {
            public MaterialsSet(Object outerObject) : base(outerObject)
            {
            }
        }

        [Serializable]
        public class MaterialsLayer : PrefabSafeSet.PrefabLayer<Material>
        {
        }
    }
}
