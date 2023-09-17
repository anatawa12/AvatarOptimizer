#if !NADEMOFU
using UnityEditor.Build;
using UnityEngine;

namespace Anatawa12.ApplyOnPlay
{
    /**
     * The callback will be called on entering play mode.
     *
     * The instance of callback can be reused but may not be reused.
     *
     * If your class that implements this interface has public constructor without parameters,
     * the class will automatically registered.
     * If not, you have to register to <see cref="ApplyOnPlayCallbackRegistry"/> manually yourself.
     */
    public interface IApplyOnPlayCallback : IOrderedCallback
    {
        /// <summary>
        ///  The name of callback will be shown in the configuration window
        /// </summary>
        string CallbackName { get; }
        
        /// <summary>
        /// Callback identifier. This ID is used as part of the name of the EditorPrefs key.
        /// </summary>
        string CallbackId { get; }

        bool ApplyOnPlay(GameObject avatarGameObject, ApplyReason reason);
    }

    public enum ApplyReason
    {
        EnteringPlayMode,
        ManualBake,
    }
}
#endif