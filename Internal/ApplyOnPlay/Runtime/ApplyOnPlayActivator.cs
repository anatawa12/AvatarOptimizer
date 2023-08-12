using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly:InternalsVisibleTo("com.anatawa12.avatar-optimizer.internal.apply-on-play.editor")]

namespace Anatawa12.ApplyOnPlay
{
    [DefaultExecutionOrder(-100000)]
    [ExecuteAlways]
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    internal class ApplyOnPlayActivator : MonoBehaviour
    {
#if UNITY_EDITOR
        internal static Action<ApplyOnPlayActivator> processAvatar;

        private void Awake()
        {
            UnityEditor.EditorApplication.delayCall += () => DestroyIfNeeded();
        }

        private void Start()
        {
            if (!this) return;
            if (DestroyIfNeeded()) return;
            processAvatar?.Invoke(this);
        }

        private bool DestroyIfNeeded()
        {
            var shouldDestroy = !UnityEditor.EditorApplication.isPlaying ||
                                UnityEditor.EditorUtility.IsPersistent(this);
            if (!shouldDestroy) return false;

            Debug.Log("Destroying ApplyOnPlayActivator in Start");
            DestroyImmediate(this);
            return true;
        }
#endif
    }
}