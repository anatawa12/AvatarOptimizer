using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    internal interface INodeContainer
    {
        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<FloatValueInfo>> FloatNodes { get; }
        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<ObjectValueInfo>> ObjectNodes { get; }
    }

    internal interface INodeContainer<TFloatNode, TObjectNode>
    {
        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), TFloatNode> FloatNodes { get; }
        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), TObjectNode> ObjectNodes { get; }
    }

    internal class RootPropModNodeContainer : INodeContainer<RootPropModNode<FloatValueInfo>, RootPropModNode<ObjectValueInfo>>, INodeContainer
    {
        private readonly Dictionary<(ComponentOrGameObject target, string prop), RootPropModNode<FloatValueInfo>> _floatNodes =
            new Dictionary<(ComponentOrGameObject, string), RootPropModNode<FloatValueInfo>>();
        private readonly Dictionary<(ComponentOrGameObject target, string prop), RootPropModNode<ObjectValueInfo>> _objectNodes =
            new Dictionary<(ComponentOrGameObject, string), RootPropModNode<ObjectValueInfo>>();

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<FloatValueInfo>> INodeContainer.
            FloatNodes =>
            Utils.CastDic<PropModNode<FloatValueInfo>>().CastedDic(FloatNodes);

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<ObjectValueInfo>> INodeContainer.
            ObjectNodes =>
            Utils.CastDic<PropModNode<ObjectValueInfo>>().CastedDic(ObjectNodes);

        public IReadOnlyDictionary<(ComponentOrGameObject target, string prop), RootPropModNode<FloatValueInfo>> FloatNodes =>
            _floatNodes;
        
        public IReadOnlyDictionary<(ComponentOrGameObject target, string prop), RootPropModNode<ObjectValueInfo>> ObjectNodes =>
            _objectNodes;

        public void Add(ComponentNodeContainer? container, bool alwaysApplied)
        {
            if (container == null) return;

            var applyState = alwaysApplied ? ApplyState.Always : ApplyState.Partially;

            foreach (var (key, value) in container.FloatNodes)
            {
                if (!FloatNodes.TryGetValue(key, out var node))
                    _floatNodes.Add(key, node = new RootPropModNode<FloatValueInfo>());
                node.Add(value, applyState);
            }
            
            foreach (var (key, value) in container.ObjectNodes)
            {
                if (!ObjectNodes.TryGetValue(key, out var node))
                    _objectNodes.Add(key, node = new RootPropModNode<ObjectValueInfo>());
                node.Add(value, applyState);
            }
        }

        public void Add(Component component, string prop, ComponentPropModNodeBase<FloatValueInfo> node, ApplyState applyState)
        {
            var key = (component, prop);
            if (!FloatNodes.TryGetValue(key, out var root))
                _floatNodes.Add(key, root = new RootPropModNode<FloatValueInfo>());
            root.Add(node, applyState);
        }

        public void Add(Component component, string prop, ComponentPropModNodeBase<ObjectValueInfo> node, ApplyState applyState)
        {
            var key = (component, prop);
            if (!_objectNodes.TryGetValue(key, out var root))
                _objectNodes.Add(key, root = new RootPropModNode<ObjectValueInfo>());
            root.Add(node, applyState);
        }

        public bool? GetConstantValue(ComponentOrGameObject gameObject, string property, bool currentValue) =>
            FloatNodes.TryGetValue((gameObject, property), out var node)
                ? node.AsConstantValue(currentValue)
                : currentValue;
    }

    internal class NodeContainerBase<TFloatNode, TObjectNode> : INodeContainer<TFloatNode, TObjectNode>, INodeContainer
        where TFloatNode : PropModNode<FloatValueInfo>
        where TObjectNode : PropModNode<ObjectValueInfo>
    {
        private readonly Dictionary<(ComponentOrGameObject target, string prop), TFloatNode> _floatNodes =
            new Dictionary<(ComponentOrGameObject, string), TFloatNode>();
        private readonly Dictionary<(ComponentOrGameObject target, string prop), TObjectNode> _objectNodes =
            new Dictionary<(ComponentOrGameObject, string), TObjectNode>();

        public IReadOnlyDictionary<(ComponentOrGameObject target, string prop), TFloatNode> FloatNodes => _floatNodes;
        public IReadOnlyDictionary<(ComponentOrGameObject target, string prop), TObjectNode> ObjectNodes =>
            _objectNodes;

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<FloatValueInfo>> INodeContainer.
            FloatNodes => Utils.CastDic<PropModNode<FloatValueInfo>>().CastedDic(_floatNodes);

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<ObjectValueInfo>> INodeContainer.
            ObjectNodes => Utils.CastDic<PropModNode<ObjectValueInfo>>().CastedDic(_objectNodes);

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

    internal class AnimatorLayerNodeContainer : NodeContainerBase<AnimatorLayerPropModNode<FloatValueInfo>,
        AnimatorLayerPropModNode<ObjectValueInfo>>
    {
    }

    internal class AnimatorControllerNodeContainer : NodeContainerBase<AnimatorControllerPropModNode<FloatValueInfo>,
        AnimatorControllerPropModNode<ObjectValueInfo>>
    {
    }

    internal class ComponentNodeContainer
        : NodeContainerBase<ComponentPropModNodeBase<FloatValueInfo>, ComponentPropModNodeBase<ObjectValueInfo>>
    {
    }

    internal class ImmutableNodeContainer : NodeContainerBase<ImmutablePropModNode<FloatValueInfo>, ImmutablePropModNode<ObjectValueInfo>>
    {
    }
}
