using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.AnimatorParsers
{
    interface IModificationSource {}

    class AnimationSource : IModificationSource, IErrorContext
    {
        public AnimationClip Clip { get; }
        public EditorCurveBinding Binding { get; }

        public AnimationSource(AnimationClip clip, EditorCurveBinding binding)
        {
            Clip = clip;
            Binding = binding;
        }

        IEnumerable<ObjectReference> IErrorContext.ContextReferences => new[] { ObjectRegistry.GetReference(Clip) };
    }
    
    internal class ComponentAnimationSource : IModificationSource, IErrorContext
    {
        public Component Component { get; }

        public ComponentAnimationSource(Component component) => Component = component;

        IEnumerable<ObjectReference> IErrorContext.ContextReferences =>
            new[] { ObjectRegistry.GetReference(Component) };
    }
}