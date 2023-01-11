using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class EditSkinnedMeshComponentProcessor
    {
        public void Process(OptimizerSession session)
        {
            var processorsByRenderer = new Dictionary<SkinnedMeshRenderer, List<IEditSkinnedMeshProcessor>>();
            foreach (var mergePhysBone in session.GetComponents<EditSkinnedMeshComponent>())
            {
                var processor = CreateProcessor(mergePhysBone);
                if (!processorsByRenderer.TryGetValue(processor.Target, out var processors))
                    processors = processorsByRenderer[processor.Target] = new List<IEditSkinnedMeshProcessor>();
                processors.Add(processor);
            }

            var proceed = new HashSet<SkinnedMeshRenderer>();

            while (proceed.Count != processorsByRenderer.Count)
            {
                var prevProceedCount = proceed.Count;
                foreach (var keyValuePair in processorsByRenderer
                             .Where(p => !proceed.Contains(p.Key))
                             .Where(p => p.Value.SelectMany(x => x.Dependencies).All(proceed.Contains)))
                {
                    foreach (var processor in keyValuePair.Value)
                        processor.Process(session);

                    proceed.Add(keyValuePair.Key);
                }

                if (prevProceedCount == proceed.Count)
                    throw new InvalidOperationException("Circular Dependencies Detected");
            }
        }

        private readonly Dictionary<Type, Func<EditSkinnedMeshComponent, IEditSkinnedMeshProcessor>> _creators =
            new Dictionary<Type, Func<EditSkinnedMeshComponent, IEditSkinnedMeshProcessor>>
            {
                [typeof(MergeSkinnedMesh)] = x => new MergeSkinnedMeshProcessor((MergeSkinnedMesh)x),
            };

        private IEditSkinnedMeshProcessor CreateProcessor(EditSkinnedMeshComponent mergePhysBone) =>
            _creators[mergePhysBone.GetType()].Invoke(mergePhysBone);
    }
}
