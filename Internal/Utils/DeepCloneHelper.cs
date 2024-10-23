using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    public abstract class DeepCloneHelper
    {
        private readonly Dictionary<Object, Object> _cache = new();
        private bool _mapped = false;

        [return:NotNullIfNotNull("obj")]
        public T? MapObject<T>(T? obj) where T : Object
        {
            Profiler.BeginSample("MapObject", obj);
            T? result;
            using (ErrorReport.WithContextObject(obj))
                result = DeepClone(obj);
            Profiler.EndSample();
            return result;
        }

        protected virtual Dictionary<Object, Object> GetCache(Type type) => _cache;

        // https://github.com/bdunderscore/modular-avatar/blob/db49e2e210bc070671af963ff89df853ae4514a5/Packages/nadena.dev.modular-avatar/Editor/AnimatorMerger.cs#L199-L241
        // Originally under MIT License
        // Copyright (c) 2022 bd_
        protected abstract Object? CustomClone(Object o);

        protected enum ComponentSupport
        {
            Unsupported,
            NoClone,
            Clone,
        }

        protected abstract ComponentSupport GetComponentSupport(Object o);

        protected readonly struct MappedScope : IDisposable
        {
            private readonly DeepCloneHelper _mapper;
            private readonly bool _previous;

            public MappedScope(DeepCloneHelper mapper)
            {
                _mapper = mapper;
                _previous = mapper._mapped;
                mapper._mapped = false;
            }

            public void Dispose()
            {
                _mapper._mapped |= _previous;
            }
        }
        
        protected void Changed() => _mapped = true;
        protected bool HasChanged() => _mapped;

        protected void RegisterNotCloned(Object original) => GetCache(original.GetType())[original] = original;

        // https://github.com/bdunderscore/modular-avatar/blob/db49e2e210bc070671af963ff89df853ae4514a5/Packages/nadena.dev.modular-avatar/Editor/AnimatorMerger.cs#LL242-L340C10
        // Originally under MIT License
        // Copyright (c) 2022 bd_
        [return:NotNullIfNotNull("original")]
        protected T? DeepClone<T>(T? original) where T : Object
        {
            if (original == null) return null;

            switch (GetComponentSupport(original))
            {
                case ComponentSupport.Unsupported:
                    throw new Exception($"Unknown type referenced from animator: {original.GetType()}");
                case ComponentSupport.NoClone:
                    return original;
                case ComponentSupport.Clone:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var cache = GetCache(original.GetType());
            if (cache.TryGetValue(original, out var cached)) return (T)cached;

            var obj = CustomClone(original);
            if (obj == null) return DefaultDeepCloneImpl(original, cache);

            cache[original] = obj;
            cache[obj] = obj;
            return (T)obj;
        }

        protected T DefaultDeepClone<T>(T original) where T : Object =>
            DefaultDeepCloneImpl(original, GetCache(original.GetType()));

        private T DefaultDeepCloneImpl<T>(T original, Dictionary<Object, Object> cache) where T : Object
        {
            Object obj;
            var ctor = original.GetType().GetConstructor(Type.EmptyTypes);
            if (ctor == null || original is ScriptableObject)
            {
                obj = Object.Instantiate(original);
            }
            else
            {
                Profiler.BeginSample("DeepCloneHelper.CopySerialized");
                obj = (T)ctor.Invoke(Array.Empty<object>());
                EditorUtility.CopySerialized(original, obj);
                Profiler.EndSample();
            }

            ObjectRegistry.RegisterReplacedObject(original, obj);
            cache[original] = obj;
            cache[obj] = obj;

            using (var so = new SerializedObject(obj))
            {
                foreach (var prop in so.ObjectReferenceProperties())
                    prop.objectReferenceValue = DeepClone(prop.objectReferenceValue);

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return (T)obj;
        }
    }
}
