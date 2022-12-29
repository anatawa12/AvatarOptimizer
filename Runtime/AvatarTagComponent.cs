using UnityEngine;

namespace Anatawa12.Merger
{
    public abstract class AvatarTagComponent : MonoBehaviour
    {
        private void Awake()
        {
            if (!Application.isPlaying) return;
            Debug.Log("Start: MergePhysBone");
            if (IsValid())
            {
                Debug.Log("Start: Before DoMerge");
                Apply();
                DestroyImmediate(this);
            }
        }

        protected internal abstract bool IsValid();
        protected internal abstract void Apply();
    }
}
