using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    /// <summary>
    /// Apart from the name, this processor class may work for both SkinnedMeshRenderer and MeshRenderer depending on <typeparamref name="TComponent"/>.
    /// </summary>
    /// <typeparam name="TComponent"></typeparam>
    internal abstract class EditSkinnedMeshProcessor<TComponent> : IEditSkinnedMeshProcessor
        where TComponent : EditSkinnedMeshComponent
    {
        public abstract EditSkinnedMeshProcessorOrder ProcessOrder { get; }
        public virtual IEnumerable<SkinnedMeshRenderer> Dependencies => Array.Empty<SkinnedMeshRenderer>();
        protected TComponent Component { get; }
        public Renderer TargetGeneric { get; }
        public SkinnedMeshRenderer Target => (SkinnedMeshRenderer)TargetGeneric;

        EditSkinnedMeshComponent IEditSkinnedMeshProcessor.Component => Component;

        protected EditSkinnedMeshProcessor(TComponent component)
        {
            Component = component;
            TargetGeneric = component.GetComponent<Renderer>();
        }

        public abstract void Process(BuildContext context, MeshInfo2 target);

        public abstract IMeshInfoComputer GetComputer(IMeshInfoComputer upstream);

        protected bool Equals(EditSkinnedMeshProcessor<TComponent> other) => Component == other.Component;

        public override bool Equals(object? obj) =>
            obj != null &&
            (ReferenceEquals(this, obj) || 
             obj.GetType() == this.GetType() && Equals((EditSkinnedMeshProcessor<TComponent>)obj));

        public override int GetHashCode() => Component.GetHashCode();
    }

    internal interface IEditSkinnedMeshProcessor
    {
        EditSkinnedMeshProcessorOrder ProcessOrder { get; }
        IEnumerable<SkinnedMeshRenderer> Dependencies { get; }
        Renderer TargetGeneric { get; }
        EditSkinnedMeshComponent Component { get; }
        void Process(BuildContext context, MeshInfo2 target);

        IMeshInfoComputer GetComputer(IMeshInfoComputer upstream);
    }

    enum EditSkinnedMeshProcessorOrder : int
    {
        Generation = int.MinValue,
        Evacuate = int.MinValue + 1,
        RemovingMesh = -20000,
        RemoveEmpty = -15000,
        AutoConfigureFreezeBlendShape = -10000 - 1,
        AfterRemoveMesh = -10000,
        AfterFreezeBlendShape = -10000 + 1,
        ReverseEvacuate = int.MaxValue,
    }

    internal interface IMeshInfoComputer
    {
        (string name, float weight)[] BlendShapes();
        Material?[] Materials(bool fast = true);
    }

    internal class AbstractMeshInfoComputer : IMeshInfoComputer
    {
        private readonly IMeshInfoComputer? _upstream;

        public AbstractMeshInfoComputer(IMeshInfoComputer? upstream)
        {
            _upstream = upstream;
        }

        public virtual (string name, float weight)[] BlendShapes() => _upstream?.BlendShapes() ?? Array.Empty<(string, float)>();

        public virtual Material?[] Materials(bool fast = true) => _upstream?.Materials(fast) ?? Array.Empty<Material>();
    }


    internal class SourceMeshInfoComputer : IMeshInfoComputer
    {
        private readonly Renderer _target;

        public SourceMeshInfoComputer(Renderer target)
        {
            _target = target;
        }


        public static (string, float)[] BlendShapes(Renderer renderer)
        {
            if (renderer is not SkinnedMeshRenderer skinned) return Array.Empty<(string, float)>();
            var mesh = skinned.sharedMesh;
            if (mesh == null) return Array.Empty<(string, float)>();
            var array = new (string, float)[mesh.blendShapeCount];
            for (var i = 0; i < mesh.blendShapeCount; i++)
                array[i] = (mesh.GetBlendShapeName(i), skinned.GetBlendShapeWeight(i));
            return array;
        }

        public static Material[] Materials(Renderer renderer) => renderer.sharedMaterials;

        public (string, float)[] BlendShapes() => BlendShapes(_target);
        public Material[] Materials(bool fast = true) => Materials(_target);
    }
}
