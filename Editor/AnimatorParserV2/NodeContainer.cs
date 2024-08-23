using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    internal interface INodeContainer
    {
        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<float>> FloatNodes { get; }
        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<Object>> ObjectNodes { get; }
    }

    internal interface INodeContainer<TFloatNode, TObjectNode>
    {
        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), TFloatNode> FloatNodes { get; }
        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), TObjectNode> ObjectNodes { get; }
    }

    internal class RootPropModNodeContainer : INodeContainer<RootPropModNode<float>, RootPropModNode<Object>>, INodeContainer
    {
        private readonly Dictionary<(ComponentOrGameObject target, string prop), RootPropModNode<float>> _floatNodes =
            new Dictionary<(ComponentOrGameObject, string), RootPropModNode<float>>();
        private readonly Dictionary<(ComponentOrGameObject target, string prop), RootPropModNode<Object>> _objectNodes =
            new Dictionary<(ComponentOrGameObject, string), RootPropModNode<Object>>();

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<float>> INodeContainer.
            FloatNodes =>
            Utils.CastDic<PropModNode<float>>().CastedDic(FloatNodes);

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<Object>> INodeContainer.
            ObjectNodes =>
            Utils.CastDic<PropModNode<Object>>().CastedDic(ObjectNodes);

        public IReadOnlyDictionary<(ComponentOrGameObject target, string prop), RootPropModNode<float>> FloatNodes =>
            _floatNodes;
        
        public IReadOnlyDictionary<(ComponentOrGameObject target, string prop), RootPropModNode<Object>> ObjectNodes =>
            _objectNodes;

        public void Add(ComponentNodeContainer? container, bool alwaysApplied)
        {
            if (container == null) return;

            foreach (var (key, value) in container.FloatNodes)
            {
                if (!FloatNodes.TryGetValue(key, out var node))
                    _floatNodes.Add(key, node = new RootPropModNode<float>());
                node.Add(value, alwaysApplied);
            }
            
            foreach (var (key, value) in container.ObjectNodes)
            {
                if (!ObjectNodes.TryGetValue(key, out var node))
                    _objectNodes.Add(key, node = new RootPropModNode<Object>());
                node.Add(value, alwaysApplied);
            }
        }

        public void Add(Component component, string prop, ComponentPropModNodeBase<float> node, bool alwaysApplied)
        {
            var key = (component, prop);
            if (!FloatNodes.TryGetValue(key, out var root))
                _floatNodes.Add(key, root = new RootPropModNode<float>());
            root.Add(node, alwaysApplied);
        }

        public void Add(Component component, string prop, ComponentPropModNodeBase<Object> node, bool alwaysApplied)
        {
            var key = (component, prop);
            if (!_objectNodes.TryGetValue(key, out var root))
                _objectNodes.Add(key, root = new RootPropModNode<Object>());
            root.Add(node, alwaysApplied);
        }

        public bool? GetConstantValue(ComponentOrGameObject gameObject, string property, bool currentValue) =>
            FloatNodes.TryGetValue((gameObject, property), out var node)
                ? node.AsConstantValue(currentValue)
                : currentValue;
    }

    internal class NodeContainerBase<TFloatNode, TObjectNode> : INodeContainer<TFloatNode, TObjectNode>, INodeContainer
        where TFloatNode : PropModNode<float>
        where TObjectNode : PropModNode<Object>
    {
        private readonly Dictionary<(ComponentOrGameObject target, string prop), TFloatNode> _floatNodes =
            new Dictionary<(ComponentOrGameObject, string), TFloatNode>();
        private readonly Dictionary<(ComponentOrGameObject target, string prop), TObjectNode> _objectNodes =
            new Dictionary<(ComponentOrGameObject, string), TObjectNode>();

        public IReadOnlyDictionary<(ComponentOrGameObject target, string prop), TFloatNode> FloatNodes => _floatNodes;
        public IReadOnlyDictionary<(ComponentOrGameObject target, string prop), TObjectNode> ObjectNodes =>
            _objectNodes;

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<float>> INodeContainer.
            FloatNodes => Utils.CastDic<PropModNode<float>>().CastedDic(_floatNodes);

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<Object>> INodeContainer.
            ObjectNodes => Utils.CastDic<PropModNode<Object>>().CastedDic(_objectNodes);

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

    internal class AnimatorLayerNodeContainer : NodeContainerBase<AnimatorLayerPropModNode<float>,
        AnimatorLayerPropModNode<Object>>
    {
    }

    internal class AnimatorControllerNodeContainer : NodeContainerBase<AnimatorControllerPropModNode<float>,
        AnimatorControllerPropModNode<Object>>
    {
    }

    internal class ComponentNodeContainer
        : NodeContainerBase<ComponentPropModNodeBase<float>, ComponentPropModNodeBase<Object>>
    {
    }

    internal class ImmutableNodeContainer : NodeContainerBase<ImmutablePropModNode<float>, ImmutablePropModNode<Object>>
    {
    }
}
