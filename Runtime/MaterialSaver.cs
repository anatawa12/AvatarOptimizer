using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/internal not should be used manually/MaterialSaver")]
    [ExecuteInEditMode]
    public class MaterialSaver : AvatarTagComponent
    {
        public Material[] sharedMaterials;

        private int _tickCount = 0;

        private void Update()
        {
            if (GetComponent<Renderer>() == null)
            {
                DestroyImmediate(this);
            }
            else if (!RuntimeUtil.isPlaying)
            {
                _tickCount++;
                if (_tickCount > 2)
                    DestroyImmediate(this);
            }
        }
    }
}
