using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    internal interface INodeContainer
    {
        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<ValueInfo<float>>> FloatNodes { get; }
        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<ValueInfo<Object>>> ObjectNodes { get; }
    }

    internal interface INodeContainer<TFloatNode, TObjectNode>
    {
        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), TFloatNode> FloatNodes { get; }
        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), TObjectNode> ObjectNodes { get; }
    }

    internal class RootPropModNodeContainer : INodeContainer<RootPropModNode<ValueInfo<float>>, RootPropModNode<ValueInfo<Object>>>, INodeContainer
    {
        private readonly Dictionary<(ComponentOrGameObject target, string prop), RootPropModNode<ValueInfo<float>>> _floatNodes =
            new Dictionary<(ComponentOrGameObject, string), RootPropModNode<ValueInfo<float>>>();
        private readonly Dictionary<(ComponentOrGameObject target, string prop), RootPropModNode<ValueInfo<Object>>> _objectNodes =
            new Dictionary<(ComponentOrGameObject, string), RootPropModNode<ValueInfo<Object>>>();

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<ValueInfo<float>>> INodeContainer.
            FloatNodes =>
            Utils.CastDic<PropModNode<ValueInfo<float>>>().CastedDic(FloatNodes);

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<ValueInfo<Object>>> INodeContainer.
            ObjectNodes =>
            Utils.CastDic<PropModNode<ValueInfo<Object>>>().CastedDic(ObjectNodes);

        public IReadOnlyDictionary<(ComponentOrGameObject target, string prop), RootPropModNode<ValueInfo<float>>> FloatNodes =>
            _floatNodes;
        
        public IReadOnlyDictionary<(ComponentOrGameObject target, string prop), RootPropModNode<ValueInfo<Object>>> ObjectNodes =>
            _objectNodes;

        public void Add(ComponentNodeContainer? container, bool alwaysApplied)
        {
            if (container == null) return;

            foreach (var (key, value) in container.FloatNodes)
            {
                if (!FloatNodes.TryGetValue(key, out var node))
                    _floatNodes.Add(key, node = new RootPropModNode<ValueInfo<float>>());
                node.Add(value, alwaysApplied);
            }
            
            foreach (var (key, value) in container.ObjectNodes)
            {
                if (!ObjectNodes.TryGetValue(key, out var node))
                    _objectNodes.Add(key, node = new RootPropModNode<ValueInfo<Object>>());
                node.Add(value, alwaysApplied);
            }
        }

        public void Add(Component component, string prop, ComponentPropModNodeBase<ValueInfo<float>> node, bool alwaysApplied)
        {
            var key = (component, prop);
            if (!FloatNodes.TryGetValue(key, out var root))
                _floatNodes.Add(key, root = new RootPropModNode<ValueInfo<float>>());
            root.Add(node, alwaysApplied);
        }

        public void Add(Component component, string prop, ComponentPropModNodeBase<ValueInfo<Object>> node, bool alwaysApplied)
        {
            var key = (component, prop);
            if (!_objectNodes.TryGetValue(key, out var root))
                _objectNodes.Add(key, root = new RootPropModNode<ValueInfo<Object>>());
            root.Add(node, alwaysApplied);
        }

        public bool? GetConstantValue(ComponentOrGameObject gameObject, string property, bool currentValue) =>
            FloatNodes.TryGetValue((gameObject, property), out var node)
                ? node.AsConstantValue(currentValue)
                : currentValue;
    }

    internal class NodeContainerBase<TFloatNode, TObjectNode> : INodeContainer<TFloatNode, TObjectNode>, INodeContainer
        where TFloatNode : PropModNode<ValueInfo<float>>
        where TObjectNode : PropModNode<ValueInfo<Object>>
    {
        private readonly Dictionary<(ComponentOrGameObject target, string prop), TFloatNode> _floatNodes =
            new Dictionary<(ComponentOrGameObject, string), TFloatNode>();
        private readonly Dictionary<(ComponentOrGameObject target, string prop), TObjectNode> _objectNodes =
            new Dictionary<(ComponentOrGameObject, string), TObjectNode>();

        public IReadOnlyDictionary<(ComponentOrGameObject target, string prop), TFloatNode> FloatNodes => _floatNodes;
        public IReadOnlyDictionary<(ComponentOrGameObject target, string prop), TObjectNode> ObjectNodes =>
            _objectNodes;

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<ValueInfo<float>>> INodeContainer.
            FloatNodes => Utils.CastDic<PropModNode<ValueInfo<float>>>().CastedDic(_floatNodes);

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<ValueInfo<Object>>> INodeContainer.
            ObjectNodes => Utils.CastDic<PropModNode<ValueInfo<Object>>>().CastedDic(_objectNodes);

        public void Add(ComponentOrGameObject target, string prop, TFloatNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            _floatNodes.Add((target, prop), node);
        }

        public void Add(ComponentOrGameObject target, string prop, TObjectNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            _objectNodes.Add((target, prop), node);
        }

        public void Set(ComponentOrGameObject target, string prop, TFloatNode node)
        {
            _floatNodes[(target, prop)] = node ?? throw new ArgumentNullException(nameof(node));
        }

        public void Set(ComponentOrGameObject target, string prop, TObjectNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            _objectNodes[(target, prop)] = node;
        }
    }

    internal class AnimatorLayerNodeContainer : NodeContainerBase<AnimatorLayerPropModNode<ValueInfo<float>>,
        AnimatorLayerPropModNode<ValueInfo<Object>>>
    {
    }

    internal class AnimatorControllerNodeContainer : NodeContainerBase<AnimatorControllerPropModNode<ValueInfo<float>>,
        AnimatorControllerPropModNode<ValueInfo<Object>>>
    {
    }

    internal class ComponentNodeContainer
        : NodeContainerBase<ComponentPropModNodeBase<ValueInfo<float>>, ComponentPropModNodeBase<ValueInfo<Object>>>
    {
    }

    internal class ImmutableNodeContainer : NodeContainerBase<ImmutablePropModNode<ValueInfo<float>>, ImmutablePropModNode<ValueInfo<Object>>>
    {
    }
}
