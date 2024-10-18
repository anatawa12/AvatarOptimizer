using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal abstract class EditSkinnedMeshProcessor<TComponent> : IEditSkinnedMeshProcessor
        where TComponent : EditSkinnedMeshComponent
    {
        public abstract EditSkinnedMeshProcessorOrder ProcessOrder { get; }
        public virtual IEnumerable<SkinnedMeshRenderer> Dependencies => Array.Empty<SkinnedMeshRenderer>();
        protected TComponent Component { get; }
        public SkinnedMeshRenderer Target { get; }

        EditSkinnedMeshComponent IEditSkinnedMeshProcessor.Component => Component;

        protected EditSkinnedMeshProcessor(TComponent component)
        {
            Component = component;
            Target = component.GetComponent<SkinnedMeshRenderer>();
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
        SkinnedMeshRenderer Target { get; }
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
        private readonly SkinnedMeshRenderer _target;

        public SourceMeshInfoComputer(SkinnedMeshRenderer target) => _target = target;


        public static (string, float)[] BlendShapes(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            if (mesh == null) return Array.Empty<(string, float)>();
            var array = new (string, float)[mesh.blendShapeCount];
            for (var i = 0; i < mesh.blendShapeCount; i++)
                array[i] = (mesh.GetBlendShapeName(i), renderer.GetBlendShapeWeight(i));
            return array;
        }

        public static Material[] Materials(SkinnedMeshRenderer renderer) => renderer.sharedMaterials;

        public (string, float)[] BlendShapes() => BlendShapes(_target);
        public Material[] Materials(bool fast = true) => Materials(_target);
    }
}
