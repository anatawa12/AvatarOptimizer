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
        protected override bool Enabled(TraceAndOptimizeState state) => state.RemoveUnusedAnimatingProperties;

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
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

        public static Func<string, bool>? Get(BuildContext context, Object component)
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
                    int index;

                    if (TrySubProp(prop, "blendShape", out var name))
                        return mesh.BlendShapes.FindIndex(b => b.name == name) != -1;

                    if (TryArrayIndex(prop, "materials", out index))
                        return index < materialSlots;

                    if (VProp.IsBlendShapeIndex(prop))
                        return VProp.ParseBlendShapeIndex(prop) < mesh.BlendShapes.Count;

                    return true;
                };
            });
        }

        private static bool TrySubProp(string prop, string subProp, out string? sub)
        {
            sub = null;
            if (!prop.StartsWith(subProp + '.')) return false;
            sub = prop.Substring(subProp.Length + 1);
            return true;
        }

        private static bool TryArrayIndex(string prop, string arrayPropertyName, out int index)
        {
            index = -1; 
            if (!prop.StartsWith(arrayPropertyName)) return false;
            prop = prop.Substring(arrayPropertyName.Length);
            if (!prop.StartsWith(".Array.data[")) return false;
            prop = prop.Substring(".Array.data[".Length);
            var close = prop.IndexOf(']');
            if (close == -1) return false;
            if (!int.TryParse(prop.Substring(0, close), out index)) return false;
            return true;
        }
    }
}
