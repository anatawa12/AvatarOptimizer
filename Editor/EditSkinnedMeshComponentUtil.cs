using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anatawa12.AvatarOptimizer
{
    [InitializeOnLoad]
    internal static class EditSkinnedMeshComponentUtil
    {
        static EditSkinnedMeshComponentUtil()
        {
            RuntimeUtil.OnAwakeEditSkinnedMesh += OnAwake;
            RuntimeUtil.OnDestroyEditSkinnedMesh += OnDestroy;

            AssemblyReloadEvents.afterAssemblyReload += AfterAssemblyReload;
        }

        private static void AfterAssemblyReload()
        {
            foreach (var component in Enumerable.Range(0, SceneManager.sceneCount)
                         .Select(SceneManager.GetSceneAt)
                         .SelectMany(x => x.GetRootGameObjects())
                         .SelectMany(x => x.GetComponentsInChildren<EditSkinnedMeshComponent>(true)))
                OnAwake(component);
        }

        // might be called by Processor to make sure the component is registered
        internal static void OnAwake(EditSkinnedMeshComponent component)
        {
            EditorShared.AddComponent(component);
        }

        private static void OnDestroy(EditSkinnedMeshComponent component)
        {
            EditorShared.RemoveComponent(component);
        }

        public static bool IsModifiedByEditComponent(SkinnedMeshRenderer renderer) =>
            EditorShared.IsModifiedByEditComponent(renderer);

        public static string[] GetBlendShapes(SkinnedMeshRenderer renderer) => EditorShared.GetBlendShapes(renderer);

        public static string[] GetBlendShapes(SkinnedMeshRenderer renderer, EditSkinnedMeshComponent before) =>
            EditorShared.GetBlendShapes(renderer, before);

        public static Material[] GetMaterials(SkinnedMeshRenderer renderer) => EditorShared.GetMaterials(renderer);

        // Rider's problem, to call GetMaterials(renderer, fast: false)
        // ReSharper disable once MethodOverloadWithOptionalParameter
        public static Material[] GetMaterials(SkinnedMeshRenderer renderer, EditSkinnedMeshComponent before = null,
            bool fast = true) => EditorShared.GetMaterials(renderer, before, fast);

        private static readonly SkinnedMeshEditorSorter EditorShared = new SkinnedMeshEditorSorter();
    }

    internal class SkinnedMeshEditorSorter
    {
        // might be called by Processor to make sure the component is registered
        public void AddComponent(EditSkinnedMeshComponent component)
        {
            var processor = CreateProcessor(component);
            if (!ProcessorsByRenderer.TryGetValue(processor.Target, out var processors))
                processors = ProcessorsByRenderer[processor.Target] = new SkinnedMeshProcessors(processor.Target);
            processors.AddProcessor(processor);
        }

        public void RemoveComponent(EditSkinnedMeshComponent component)
        {
            var target = component.GetComponent<SkinnedMeshRenderer>();
            GetProcessors(target)?.RemoveProcessorOf(component);
        }

        public bool IsModifiedByEditComponent(SkinnedMeshRenderer renderer) =>
            ProcessorsByRenderer.ContainsKey(renderer);

        readonly Dictionary<SkinnedMeshRenderer, SkinnedMeshProcessors> ProcessorsByRenderer =
            new Dictionary<SkinnedMeshRenderer, SkinnedMeshProcessors>();

        public string[] GetBlendShapes(SkinnedMeshRenderer renderer) => GetBlendShapes(renderer, null);

        public string[] GetBlendShapes(SkinnedMeshRenderer renderer, EditSkinnedMeshComponent before) =>
            GetProcessors(renderer)?.GetBlendShapes(before) ?? SourceMeshInfoComputer.BlendShapes(renderer);

        public Material[] GetMaterials(SkinnedMeshRenderer renderer) => GetMaterials(renderer, null);

        // Rider's problem, to call GetMaterials(renderer, fast: false)
        // ReSharper disable once MethodOverloadWithOptionalParameter
        public Material[] GetMaterials(SkinnedMeshRenderer renderer, EditSkinnedMeshComponent before = null,
            bool fast = true) =>
            GetProcessors(renderer)?.GetMaterials(before, fast) ?? SourceMeshInfoComputer.Materials(renderer);

        [CanBeNull]
        private SkinnedMeshProcessors GetProcessors(SkinnedMeshRenderer target)
        {
            ProcessorsByRenderer.TryGetValue(target, out var processors);
            return processors;
        }

        public IEnumerable<SkinnedMeshProcessors> GetSortedProcessors(
            IEnumerable<SkinnedMeshRenderer> targets)
        {
            var processors = new LinkedList<SkinnedMeshProcessors>(targets.Select(GetProcessors).Where(x => x != null));

            var proceed = new HashSet<SkinnedMeshRenderer>();
            foreach (var renderer in processors
                         .SelectMany(p => p.GetSorted())
                         .SelectMany(x => x.Dependencies))
                proceed.Add(renderer);
            foreach (var renderer in processors.Select(p => p.Target))
                proceed.Remove(renderer);

            while (processors.Count != 0)
            {
                var iterator = processors.First;
                while (!iterator.Value.GetSorted().SelectMany(x => x.Dependencies).All(proceed.Contains))
                    iterator = iterator.Next ?? throw new InvalidOperationException("Circular Dependencies Detected");

                var found = iterator.Value;
                processors.Remove(iterator);
                proceed.Add(found.Target);
                yield return found;
            }
        }

        internal class SkinnedMeshProcessors
        {
            internal readonly SkinnedMeshRenderer Target;
            private readonly HashSet<IEditSkinnedMeshProcessor> _processors = new HashSet<IEditSkinnedMeshProcessor>();
            private List<IEditSkinnedMeshProcessor> _sorted = new List<IEditSkinnedMeshProcessor>();
            private IMeshInfoComputer[] _computers;

            public SkinnedMeshProcessors(SkinnedMeshRenderer target)
            {
                Target = target;
            }

            private void PurgeCache(bool removing = false)
            {
                _sorted = null;
                _computers = null;
            }

            public void AddProcessor(IEditSkinnedMeshProcessor processor)
            {
                if (_processors.Add(processor))
                    PurgeCache();
            }

            public void RemoveProcessorOf(EditSkinnedMeshComponent component)
            {
                var removed = _processors.RemoveWhere(x => x.Component == component);
                if (removed == 0) return;
                PurgeCache(removing: true);
            }

            internal List<IEditSkinnedMeshProcessor> GetSorted()
            {
                if (_sorted != null) return _sorted;

                _sorted = new List<IEditSkinnedMeshProcessor>(_processors);
                _sorted.Sort((a, b) =>
                    a.ProcessOrder.CompareTo(b.ProcessOrder));

                return _sorted;
            }

            private IMeshInfoComputer[] GetComputers()
            {
                if (_computers != null) return _computers;
                var sorted = GetSorted();
                _computers = new IMeshInfoComputer[sorted.Count + 1];
                var computer = _computers[0] = new SourceMeshInfoComputer(Target);
                for (var i = 0; i < sorted.Count; i++)
                    computer = _computers[i + 1] = sorted[i].GetComputer(computer);
                return _computers;
            }

            private IMeshInfoComputer GetComputer(EditSkinnedMeshComponent before = null) => !before
                ? GetComputers().Last()
                : GetComputers()[_sorted.FindIndex(x => x.Component == before)];

            public string[] GetBlendShapes(EditSkinnedMeshComponent before = null) => GetComputer(before).BlendShapes();

            public Material[] GetMaterials(EditSkinnedMeshComponent before = null, bool fast = true) =>
                GetComputer(before).Materials(fast);
        }

        private static readonly Dictionary<Type, Func<EditSkinnedMeshComponent, IEditSkinnedMeshProcessor>> Creators =
            new Dictionary<Type, Func<EditSkinnedMeshComponent, IEditSkinnedMeshProcessor>>
            {
                [typeof(MergeSkinnedMesh)] = x => new MergeSkinnedMeshProcessor((MergeSkinnedMesh)x),
                [typeof(FreezeBlendShape)] = x => new FreezeBlendShapeProcessor((FreezeBlendShape)x),
                [typeof(MergeToonLitMaterial)] = x => new MergeToonLitMaterialProcessor((MergeToonLitMaterial)x),
                [typeof(RemoveMeshInBox)] = x => new RemoveMeshInBoxProcessor((RemoveMeshInBox)x),
            };

        private static IEditSkinnedMeshProcessor CreateProcessor(EditSkinnedMeshComponent mergePhysBone) =>
            Creators[mergePhysBone.GetType()].Invoke(mergePhysBone);
    }
}
