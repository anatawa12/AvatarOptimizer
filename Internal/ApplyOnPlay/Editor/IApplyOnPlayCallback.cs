using UnityEditor.Build;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.ApplyOnPlay
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
        bool ApplyOnPlay(GameObject avatarGameObject);
    }
}
