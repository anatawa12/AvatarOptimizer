using System;
using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal abstract class EditSkinnedMeshProcessor<TComponent> : IEditSkinnedMeshProcessor
        where TComponent : EditSkinnedMeshComponent
    {
        public IEnumerable<SkinnedMeshRenderer> Dependencies => Array.Empty<SkinnedMeshRenderer>();
        protected TComponent Component { get; }
        public SkinnedMeshRenderer Target { get; }

        protected EditSkinnedMeshProcessor(TComponent component)
        {
            Component = component;
            Target = component.GetComponent<SkinnedMeshRenderer>();
        }

        public abstract void Process(OptimizerSession session);
    }

    internal interface IEditSkinnedMeshProcessor
    {
        IEnumerable<SkinnedMeshRenderer> Dependencies { get; }
        SkinnedMeshRenderer Target { get; }
        void Process(OptimizerSession session);
    }
}
