using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal static class RequireComponentCache
    {
        private static Dictionary<Type, HashSet<Type>> _requireComponentCache = new Dictionary<Type, HashSet<Type>>();
        private static Dictionary<Type, HashSet<Type>> _dependantComponentCache = new Dictionary<Type, HashSet<Type>>();

        public static HashSet<Type> GetRequiredComponents(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (_requireComponentCache.TryGetValue(type, out var result)) return result;

            result = new HashSet<Type>();

            foreach (var requireComponent in type.GetCustomAttributes<RequireComponent>(true))
            {
                if (requireComponent.m_Type0 != null) result.Add(requireComponent.m_Type0);
                if (requireComponent.m_Type1 != null) result.Add(requireComponent.m_Type1);
                if (requireComponent.m_Type2 != null) result.Add(requireComponent.m_Type2);
            }

            // add to dependantComponentCache
            foreach (var requiredComponent in result)
            {
                if (!_dependantComponentCache.TryGetValue(requiredComponent, out var dependants))
                    _dependantComponentCache.Add(requiredComponent, dependants = new HashSet<Type>());
                dependants.Add(type);
            }

            _requireComponentCache.Add(type, result);
            return result;
        }

        public static HashSet<Type> GetDependantComponents(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!_dependantComponentCache.TryGetValue(type, out var result))
                _dependantComponentCache.Add(type, result = new HashSet<Type>());
            return result;
        }
    }
}
