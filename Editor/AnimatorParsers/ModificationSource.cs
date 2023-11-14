using Anatawa12.AvatarOptimizer.ErrorReporting;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.AnimatorParsers
{
    interface IModificationSource {}

    class AnimationSource : IModificationSource, IContextProvider
    {
        public AnimationClip Clip { get; }
        public EditorCurveBinding Binding { get; }

        public AnimationSource(AnimationClip clip, EditorCurveBinding binding)
        {
            Clip = clip;
            Binding = binding;
        }

        public object ProvideContext() => Clip;
    }
    
    internal class ComponentAnimationSource : IModificationSource, IContextProvider
    {
        public Component Component { get; }

        public ComponentAnimationSource(Component component) => Component = component;

        public object ProvideContext() => Component;
    }
}