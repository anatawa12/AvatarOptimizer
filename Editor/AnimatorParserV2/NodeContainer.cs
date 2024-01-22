using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    internal interface INodeContainer
    {
        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<float>> FloatNodes { get; }
    }

    internal interface INodeContainer<TFloatNode>
    {
        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), TFloatNode> FloatNodes { get; }
    }

    internal class RootPropModNodeContainer : INodeContainer<RootPropModNode<float>>, INodeContainer
    {
        private readonly Dictionary<(ComponentOrGameObject target, string prop), RootPropModNode<float>> _floatNodes =
            new Dictionary<(ComponentOrGameObject, string), RootPropModNode<float>>();

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<float>> INodeContainer.FloatNodes => 
            Utils.CastDic<PropModNode<float>>().CastedDic(FloatNodes);

        public IReadOnlyDictionary<(ComponentOrGameObject target, string prop), RootPropModNode<float>> FloatNodes => _floatNodes;

        public void Add(ComponentNodeContainer container, bool alwaysApplied)
        {
            foreach (var (key, value) in container.FloatNodes)
            {
                if (!FloatNodes.TryGetValue(key, out var node))
                    _floatNodes.Add(key, node = new RootPropModNode<float>());
                node.Add(value, alwaysApplied);
            }
        }

        public void Add(Component component, string prop, ComponentPropModNode<float> node, bool alwaysApplied)
        {
            var key = (component, prop);
            if (!FloatNodes.TryGetValue(key, out var root))
                _floatNodes.Add(key, root = new RootPropModNode<float>());
            root.Add(node, alwaysApplied);
        }

        public bool? GetConstantValue(ComponentOrGameObject gameObject, string property, bool currentValue) =>
            FloatNodes.TryGetValue((gameObject, property), out var node)
                ? node.AsConstantValue(currentValue)
                : currentValue;
    }

    internal class NodeContainerBase<TFloatNode> : INodeContainer<TFloatNode>, INodeContainer
        where TFloatNode : PropModNode<float>
    {
        private readonly Dictionary<(ComponentOrGameObject target, string prop), TFloatNode> _floatNodes = new Dictionary<(ComponentOrGameObject, string), TFloatNode>();

        public IReadOnlyDictionary<(ComponentOrGameObject target, string prop), TFloatNode> FloatNodes => _floatNodes;

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<float>> INodeContainer.
            FloatNodes => Utils.CastDic<PropModNode<float>>().CastedDic(_floatNodes);

        public void Add(ComponentOrGameObject target, string prop, [NotNull] TFloatNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            _floatNodes.Add((target, prop), node);
        }
        
        public void Set(ComponentOrGameObject target, string prop, [NotNull] TFloatNode node)
        {
            _floatNodes[(target, prop)] = node ?? throw new ArgumentNullException(nameof(node));
        }
    }

    internal class AnimatorControllerNodeContainer : NodeContainerBase<AnimatorControllerPropModNode<float>>
    {
    }

    internal class ComponentNodeContainer : NodeContainerBase<ComponentPropModNode<float>>
    {
    }

    internal class ImmutableNodeContainer : NodeContainerBase<ImmutablePropModNode<float>>
    {
    }
}
