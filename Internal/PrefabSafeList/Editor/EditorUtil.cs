using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.PrefabSafeList
{
    /// <summary>
    /// Utility to edit PrefabSafeList in CustomEditor with SerializedProperty
    /// </summary>
    public abstract partial class EditorUtil
    {
        public abstract IReadOnlyList<IElement> Elements { get; }
        public abstract int ElementsCount { get; }
        public virtual int Count => Elements.Count(x => x.Contains);

        public static EditorUtil Create(SerializedProperty property, int nestCount)
        {
            if (nestCount == 0)
                return new Root(property);
            return new PrefabModification(property, nestCount);
        }

        private EditorUtil()
        {
        }

        public abstract void Clear();

        public abstract IElement AddElement();

        private static SerializedProperty AddArrayElement([NotNull] SerializedProperty array)
        {
            array.arraySize += 1;
            return array.GetArrayElementAtIndex(array.arraySize - 1);
        }
    }

    internal static class ElementErrMsg
    {
        internal const string AddOnInvalid = "Add is called for Invalid element";
    }

    public interface IElement
    {
        EditorUtil Container { get; }
        ElementStatus Status { get; }
        SerializedProperty ValueProperty { get; }
        SerializedProperty RemovedProperty { get; }
        bool Contains { get; }
        void Add();
        void Remove();
    }
    
    public enum ElementStatus
    {
        Invalid = -1,

        AddedUpper,
        RemovedUpper,
        NewElement,
    }
}
