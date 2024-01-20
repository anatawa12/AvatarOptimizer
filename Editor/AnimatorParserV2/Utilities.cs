using System;
using System.Collections.Generic;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    static class NodesMerger
    {
        public static ImmutableNodeContainer Merge<Merge>(IEnumerable<ImmutableNodeContainer> sources, Merge merger)
            where Merge : struct, IMergeProperty
        {
            var floats = new Dictionary<(ComponentOrGameObject, string), List<ImmutablePropModNode<float>>>();
            var sourceCount = 0;

            foreach (var container in sources)
            {
                sourceCount++;
                foreach (var (key, value) in container.FloatNodes)
                {
                    if (!floats.TryGetValue(key, out var list))
                        floats.Add(key, list = new List<ImmutablePropModNode<float>>());
                    list.Add(value);
                }
            }

            var nodes = new ImmutableNodeContainer();

            foreach (var (key, value) in floats)
                nodes.FloatNodes.Add(key, merger.MergeNode(value, sourceCount));

            return nodes;
        }
    }
    
    interface IMergeProperty
    {
        ImmutablePropModNode<T> MergeNode<T>(List<ImmutablePropModNode<T>> nodes, int sourceCount);
    }
}
