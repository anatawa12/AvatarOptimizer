using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeList
{
    public class PrefabSafeListTestComponent : MonoBehaviour
    {
        [SerializeField] internal MaterialsSet materials;
        [SerializeField] internal ComplexSet complex;

        public PrefabSafeListTestComponent()
        {
            materials = new MaterialsSet(this);
        }

        [Serializable]
        public class Complex
        {
            public string value;
            public Vector3 zero;
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
        
        [Serializable]
        internal class ComplexSet : PrefabSafeList.PrefabSafeList<Complex, ComplexSet.Layer, ComplexSet.Container>
        {
            public ComplexSet(Object outerObject) : base(outerObject)
            {
            }

            [Serializable]
            internal class Layer : PrefabSafeList.PrefabLayer<Complex, Container>
            {
            }

            [Serializable]
            internal class Container : PrefabSafeList.ValueContainer<Complex>
            {
            }
        }

    }
}
