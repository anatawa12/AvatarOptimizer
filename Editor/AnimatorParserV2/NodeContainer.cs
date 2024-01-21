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
        Dictionary<(ComponentOrGameObject target, string prop), TFloatNode> FloatNodes { get; }
    }

    internal class AnimatorControllerNodeContainer : INodeContainer<AnimatorControllerPropModNode<float>>, INodeContainer
    {
        public Dictionary<(ComponentOrGameObject target, string prop), AnimatorControllerPropModNode<float>> FloatNodes { get; } =
            new Dictionary<(ComponentOrGameObject, string), AnimatorControllerPropModNode<float>>();

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<float>> INodeContainer.
            FloatNodes => Utils.CastDic<PropModNode<float>>().CastedDic(FloatNodes);

        public void Add(ComponentOrGameObject target, string prop, [NotNull] AnimatorControllerPropModNode<float> node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            FloatNodes.Add((target, prop), node);
        }
    }

    internal class RootPropModNodeContainer : INodeContainer<RootPropModNode<float>>, INodeContainer
    {
        public Dictionary<(ComponentOrGameObject target, string prop), RootPropModNode<float>> FloatNodes { get; } =
            new Dictionary<(ComponentOrGameObject, string), RootPropModNode<float>>();

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<float>> INodeContainer.FloatNodes => 
            Utils.CastDic<PropModNode<float>>().CastedDic(FloatNodes);

        public void Add(ComponentNodeContainer container, bool alwaysApplied)
        {
            foreach (var (key, value) in container.FloatNodes)
            {
                if (!FloatNodes.TryGetValue(key, out var node))
                    FloatNodes.Add(key, node = new RootPropModNode<float>());
                node.Add(value, alwaysApplied);
            }
        }

        public void Add(Component component, string prop, ComponentPropModNode<float> node, bool alwaysApplied)
        {
            var key = (component, prop);
            if (!FloatNodes.TryGetValue(key, out var root))
                FloatNodes.Add(key, root = new RootPropModNode<float>());
            root.Add(node, alwaysApplied);
        }

        public bool? GetConstantValue(ComponentOrGameObject gameObject, string property, bool gameObjectActiveSelf)
        {
            if (!FloatNodes.TryGetValue((gameObject, property), out var node))
                return gameObjectActiveSelf;

            if (node.Constant.TryGetValue(out var value))
            {
                var constValue = value == 0;
                if (node.AppliedAlways || constValue == gameObjectActiveSelf)
                    return constValue;
            }

            return null;
        }
    }

    internal class ComponentNodeContainer : INodeContainer<ComponentPropModNode<float>>, INodeContainer
    {
        public Dictionary<(ComponentOrGameObject target, string prop), ComponentPropModNode<float>> FloatNodes { get; } =
            new Dictionary<(ComponentOrGameObject, string), ComponentPropModNode<float>>();

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<float>> INodeContainer.
            FloatNodes => Utils.CastDic<PropModNode<float>>().CastedDic(FloatNodes);

        public void Add(ComponentOrGameObject target, string prop, [NotNull] ComponentPropModNode<float> node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            FloatNodes.Add((target, prop), node);
        }
    }

    internal class ImmutableNodeContainer : INodeContainer<ImmutablePropModNode<float>>, INodeContainer
    {
        public Dictionary<(ComponentOrGameObject target, string prop), ImmutablePropModNode<float>> FloatNodes { get; } =
            new Dictionary<(ComponentOrGameObject, string), ImmutablePropModNode<float>>();

        IReadOnlyDictionary<(ComponentOrGameObject target, string prop), PropModNode<float>> INodeContainer.
            FloatNodes => Utils.CastDic<PropModNode<float>>().CastedDic(FloatNodes);

        public void Add(ComponentOrGameObject target, string prop, [NotNull] ImmutablePropModNode<float> node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            FloatNodes.Add((target, prop), node);
        }
    }
}
