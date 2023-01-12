using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [InitializeOnLoad]
    internal static class EditSkinnedMeshComponentUtil
    {
        static EditSkinnedMeshComponentUtil()
        {
            RuntimeUtil.OnAwakeEditSkinnedMesh += OnAwake;
            RuntimeUtil.OnDestroyEditSkinnedMesh += OnDestroy;
        }

        private static void OnAwake(EditSkinnedMeshComponent component)
        {
            var processor = CreateProcessor(component);
            if (!ProcessorsByRenderer.TryGetValue(processor.Target, out var processors))
                processors = ProcessorsByRenderer[processor.Target] = new SkinnedMeshProcessors(processor.Target);
            processors.AddProcessor(processor);
        }

        private static void OnDestroy(EditSkinnedMeshComponent component)
        {
            var target = component.GetComponent<SkinnedMeshRenderer>();
            GetProcessors(target)?.RemoveProcessorOf(component);
        }

        public static bool IsModifiedByEditComponent(SkinnedMeshRenderer renderer) =>
            ProcessorsByRenderer.ContainsKey(renderer);

        static readonly Dictionary<SkinnedMeshRenderer, SkinnedMeshProcessors> ProcessorsByRenderer =
            new Dictionary<SkinnedMeshRenderer, SkinnedMeshProcessors>();

        public static string[] GetBlendShapes(SkinnedMeshRenderer renderer) =>
            GetProcessors(renderer)?.GetBlendShapes() ?? SourceMeshInfoComputer.BlendShapes(renderer);

        public static Material[] GetMaterials(SkinnedMeshRenderer renderer) =>
            GetProcessors(renderer)?.GetMaterials() ?? SourceMeshInfoComputer.Materials(renderer);

        [CanBeNull]
        private static SkinnedMeshProcessors GetProcessors(SkinnedMeshRenderer target)
        {
            ProcessorsByRenderer.TryGetValue(target, out var processors);
            return processors;
        }

        private class SkinnedMeshProcessors
        {
            private readonly SkinnedMeshRenderer _target;
            private readonly List<IEditSkinnedMeshProcessor> _processors = new List<IEditSkinnedMeshProcessor>();
            private bool _sorted;
            private IMeshInfoComputer _computer;

            public SkinnedMeshProcessors(SkinnedMeshRenderer target)
            {
                _target = target;
            }

            private void PurgeCache(bool removing = false)
            {
                if (!removing)
                    _sorted = false;
                _computer = null;
            }

            public void AddProcessor(IEditSkinnedMeshProcessor processor)
            {
                _processors.Add(processor);
                PurgeCache();
            }

            public void RemoveProcessorOf(EditSkinnedMeshComponent component)
            {
                var index = _processors.FindIndex(x => x.Component == component);
                if (index == -1) return;
                _processors.RemoveAt(index);
                PurgeCache(removing: true);
            }

            private List<IEditSkinnedMeshProcessor> GetSorted()
            {
                if (!_sorted)
                {
                    _processors.Sort((a, b) => 
                        a.ProcessOrder.CompareTo(b.ProcessOrder));
                    _sorted = true;
                }

                return _processors;
            }

            private IMeshInfoComputer GetComputer()
            {
                if (_computer != null) return _computer;
                var sorted = GetSorted();
                IMeshInfoComputer computer = new SourceMeshInfoComputer(_target);
                for (var i = sorted.Count - 1; i >= 0; i--)
                    computer = sorted[i].GetComputer(computer);
                return _computer = computer;
            }

            public string[] GetBlendShapes() => GetComputer().BlendShapes();
            public Material[] GetMaterials() => GetComputer().Materials();
        }

        private static readonly Dictionary<Type, Func<EditSkinnedMeshComponent, IEditSkinnedMeshProcessor>> _creators =
            new Dictionary<Type, Func<EditSkinnedMeshComponent, IEditSkinnedMeshProcessor>>
            {
                [typeof(MergeSkinnedMesh)] = x => new MergeSkinnedMeshProcessor((MergeSkinnedMesh)x),
            };

        private static IEditSkinnedMeshProcessor CreateProcessor(EditSkinnedMeshComponent mergePhysBone) =>
            _creators[mergePhysBone.GetType()].Invoke(mergePhysBone);
    }
}
