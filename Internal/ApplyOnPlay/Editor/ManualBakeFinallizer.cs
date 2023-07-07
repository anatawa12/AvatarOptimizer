using UnityEditor.Build;
using UnityEngine;

namespace Anatawa12.ApplyOnPlay
{
    /**
     * The callback will be called on finalizing manual bake.
     *
     * The instance of callback can be reused but may not be reused.
     *
     * If your class that implements this interface has public constructor without parameters,
     * the class will automatically registered.
     * If not, you have to register to <see cref="ApplyOnPlayCallbackRegistry"/> manually yourself.
     */
    public interface IManualBakeFinalizer : IOrderedCallback
    {
        /// <summary>
        ///  The name of callback will be shown in the configuration window
        /// </summary>
        string CallbackName { get; }
        
        /// <summary>
        /// Callback identifier. This ID is used as part of the name of the EditorPrefs key.
        /// </summary>
        string CallbackId { get; }

        void FinalizeManualBake(GameObject original, GameObject cloned);
    }
}
