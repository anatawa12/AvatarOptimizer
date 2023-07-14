using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.ApplyOnPlay
{
    internal class RemoveEditorOnlyOnPlay : IApplyOnPlayCallback
    {
        public int callbackOrder => -1024;
        public string CallbackName => "Remove Editor Only";
        public string CallbackId => "com.anatawa12.apply-on-play.remove-editor-only";

        public bool ApplyOnPlay(GameObject avatarGameObject, ApplyReason reason)
        {
            foreach (var transform in avatarGameObject.GetComponentsInChildren<Transform>(true))
                if (transform && transform != avatarGameObject.transform && transform.gameObject.CompareTag("EditorOnly"))
                    Object.DestroyImmediate(transform.gameObject);
            return true;
        }
    }
}