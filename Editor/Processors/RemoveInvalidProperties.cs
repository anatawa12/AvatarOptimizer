using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using nadena.dev.ndmf;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer
{
    class RemoveInvalidProperties : TraceAndOptimizePass<RemoveInvalidProperties>
    {
        public override string DisplayName => "T&O: AnimOpt: Remove Invalid Properties";

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            if (!state.OptimizeAnimator) return;
            if (state.SkipRemoveUnusedAnimatingProperties) return;

            var mappingBuilder = context.GetMappingBuilder();

            foreach (var component in mappingBuilder.GetAllAnimationComponents())
            {
                var instance = component.TargetComponent;
                if (!instance) continue; // destroyed
                if (component.Properties.Length == 0) continue; // no properties
                var check = AnimatablePropertyRegistry.Get(context, instance);
                if (check == null) continue; // we don't know if it's animatable

                foreach (var property in component.Properties)
                {
                    if (check(property)) continue;
                    component.RemoveProperty(property);
                }
            }
        }
    }

    static class AnimatablePropertyRegistry
    {
        delegate Func<string, bool> TypeInfo<T>(BuildContext context, T component);

        private static readonly Dictionary<Type, TypeInfo<Object>> _properties =
            new Dictionary<Type, TypeInfo<Object>>();

        public static Func<string, bool> Get(BuildContext context, Object component)
        {
            if (!_properties.TryGetValue(component.GetType(), out var func))
                return null;

            return func(context, component);
        }

        private static void Register<T>(TypeInfo<T> func) where T : Component =>
            _properties.Add(typeof(T), (context, x) => func(context, (T)x));

        static AnimatablePropertyRegistry()
        {
            Register<SkinnedMeshRenderer>((ctx, x) =>
            {
                var mesh = ctx.GetMeshInfoFor(x);
                var materialSlots = mesh.SubMeshes.Sum(y => y.SharedMaterials.Length);
                return (prop) =>
                {
                    if (prop.StartsWith("blendShape."))
                    {
                        var name = prop.Substring("blendShape.".Length);
                        return mesh.BlendShapes.FindIndex(b => b.name == name) != -1;
                    }

                    if (prop.StartsWith("m_Materials.Array.data["))
                    {
                        var index = int.Parse(prop.Substring("m_Materials.Array.data[".Length).TrimEnd(']'));
                        return index < materialSlots;
                    }

                    if (VProp.IsBlendShapeIndex(prop))
                    {
                        var index = VProp.ParseBlendShapeIndex(prop);
                        return index < mesh.BlendShapes.Count;
                    }

                    return true;
                };
            });
        }
    }
}
