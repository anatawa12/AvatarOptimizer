using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.Runtime
{
    public class PrefabSafeSetComponent : MonoBehaviour
    {
        public PrefabSafeSet.StringSet stringSet;

        public PrefabSafeSetComponent()
        {
            stringSet = new PrefabSafeSet.StringSet(this);
        }
    }
}
