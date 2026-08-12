using System;
using System.Collections.Generic;
using System.Reflection;

namespace Anatawa12.AvatarOptimizer
{
    public static class AssemblyCollector
    {
        public static IReadOnlyList<Assembly> GetAssemblies()
        {
#if UNITY_6000_6_OR_NEWER
            return UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies();
#else
            return AppDomain.CurrentDomain.GetAssemblies();
#endif
        }
    }
}
