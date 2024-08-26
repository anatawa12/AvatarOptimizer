using System;
using Anatawa12.AvatarOptimizer.API;
using JetBrains.Annotations;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.APIInternal
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    [MeansImplicitUse]
    [BaseTypeRequired(typeof(ComponentInformation<>))]
    internal sealed class ComponentInformationWithGUIDAttribute : ComponentInformationAttributeBase
    {
        public string Guid { get; }
        public int FileID { get; }

        public ComponentInformationWithGUIDAttribute(string guid, int fileID)
        {
            Guid = guid;
            FileID = fileID;
        }

        internal override Type? GetTargetType()
        {
            if (!GlobalObjectId.TryParse($"GlobalObjectId_V1-{1}-{Guid}-{(uint)FileID}-{0}", out var id)) return null;
            var script = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as MonoScript;
            if (script == null) return null;
            return script.GetClass();
        }
    }
}
