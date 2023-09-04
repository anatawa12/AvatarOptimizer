using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    internal readonly struct DynamicBone : IEquatable<DynamicBone>
    {
        private readonly Component _object;

        // ReSharper disable once PossibleNullReferenceException
        public List<Transform> Exclusions => _object == null
            ? throw new NullReferenceException()
            : (List<Transform>)ExternalLibraryAccessor.DynamicBone.Exclusions.GetValue(_object);

        // ReSharper disable once PossibleNullReferenceException
        [CanBeNull]
        public Transform Root => _object == null
            ? throw new NullReferenceException()
            : (Transform)ExternalLibraryAccessor.DynamicBone.Root.GetValue(_object);

        private DynamicBone(Component o)
        {
            _object = o;
        }

        public static bool TryCast(Object component, out DynamicBone dynamicBone)
        {
            dynamicBone = default;
            var classes = ExternalLibraryAccessor.DynamicBone;
            if (classes == null) return false;
            if (!classes.DynamicBoneType.IsInstanceOfType(component)) return false;
            dynamicBone = new DynamicBone((Component)component);
            return true;
        }

        public IEnumerable<Transform> GetAffectedTransforms()
        {
            var ignores = new HashSet<Transform>(Exclusions);
            var queue = new Queue<Transform>();
            queue.Enqueue(Root ? Root : _object.transform);

            while (queue.Count != 0)
            {
                var transform = queue.Dequeue();
                yield return transform;

                foreach (var child in transform.DirectChildrenEnumerable())
                    if (!ignores.Contains(child))
                        queue.Enqueue(child);
            }
        }

        public bool Equals(DynamicBone other) => Equals(_object, other._object);
        public override bool Equals(object obj) => obj is DynamicBone other && Equals(other);
        public override int GetHashCode() => _object != null ? _object.GetHashCode() : 0;

        public static Type Type => ExternalLibraryAccessor.DynamicBone?.DynamicBoneType;
    }

    static class ExternalLibraryAccessor
    {
        [CanBeNull] public static readonly DynamicBoneClasses DynamicBone = DynamicBoneClasses.Create();

        public class DynamicBoneClasses
        {
            [NotNull] public readonly Type DynamicBoneType;
            [NotNull] public readonly FieldInfo Exclusions;
            [NotNull] public readonly FieldInfo Root;

            public DynamicBoneClasses([NotNull] Type dynamicBoneType, [NotNull] FieldInfo exclusions,
                [NotNull] FieldInfo root)
            {
                DynamicBoneType = dynamicBoneType;
                Exclusions = exclusions;
                Root = root;
            }


            [CanBeNull]
            public static DynamicBoneClasses Create()
            {
                var dynamicBoneType = Utils.GetTypeFromName("DynamicBone");
                if (dynamicBoneType == null) return null;

                var exclusions =
                    dynamicBoneType.GetField("m_Exclusions", BindingFlags.Instance | BindingFlags.Public);
                if (exclusions == null) return null;
                if (exclusions.FieldType != typeof(List<Transform>)) return null;

                var root = dynamicBoneType.GetField("m_Root", BindingFlags.Instance | BindingFlags.Public);
                if (root == null) return null;
                if (root.FieldType != typeof(Transform)) return null;

                return new DynamicBoneClasses(dynamicBoneType, exclusions, root);
            }
        }
    }
}