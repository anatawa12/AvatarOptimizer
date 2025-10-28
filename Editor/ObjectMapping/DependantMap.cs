using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer;

internal readonly struct DependantMap
{
    private readonly Dictionary<GCComponentInfo, Dictionary<Component, GCComponentInfo.DependencyType>> _getDependantMap;

    public DependantMap()
    {
        _getDependantMap = new Dictionary<GCComponentInfo, Dictionary<Component, GCComponentInfo.DependencyType>>();
    }

    public static DependantMap CreateEntrypointsMap(BuildContext context) =>
        CreateMap(context, x => x.IsEntrypoint);

    public static DependantMap CreateDependantsMap(BuildContext context) => 
        CreateMap(context, x => x.BehaviourComponent || x.EntrypointComponent);

    private static DependantMap CreateMap(BuildContext context, Predicate<GCComponentInfo> filter)
    {
        var map = new DependantMap();

        var componentInfos = context.Extension<GCComponentInfoContext>();
        var state = context.GetState<TraceAndOptimizeState>();
        var exclusions = state.Exclusions;

        // entrypoint for mark & sweep is active-able GameObjects
        foreach (var componentInfo in componentInfos.AllInformation)
        {
            if (componentInfo.Component != null && filter(componentInfo))
            {
                var markContext = new MarkObjectContext(componentInfos, componentInfo.Component, map);
                markContext.MarkComponent(componentInfo.Component, GCComponentInfo.DependencyType.Normal);
                markContext.MarkRecursively();
            }
        }

        if (exclusions.Count != 0) {
            // excluded GameObjects must be exists
            var markContext = new MarkObjectContext(componentInfos, context.AvatarRootTransform, map);

            foreach (var gameObject in exclusions)
                if (gameObject != null)
                    foreach (var component in gameObject.GetComponents<Component>())
                        markContext.MarkComponent(component, GCComponentInfo.DependencyType.Normal);

            markContext.MarkRecursively();
        }

        return map;
    }

    public Dictionary<Component, GCComponentInfo.DependencyType> Get(GCComponentInfo info)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        if (_getDependantMap.TryGetValue(info, out var map))
            return map;

        map = new Dictionary<Component, GCComponentInfo.DependencyType>();
        _getDependantMap.Add(info, map);
        return map;
    }

    public Dictionary<Component, GCComponentInfo.DependencyType> this[GCComponentInfo info] => Get(info);

    public GCComponentInfo.DependencyType MergedUsages(GCComponentInfo info)
    {
        GCComponentInfo.DependencyType type = default;
        foreach (var usage in this[info].Values)
            type |= usage;
        return type;
    }
}

internal readonly struct MarkObjectContext {
    private readonly GCComponentInfoContext _componentInfos;

    private readonly DependantMap _map;
    private readonly Queue<Component> _processPending;
    private readonly Component _entrypoint;

    public MarkObjectContext(GCComponentInfoContext componentInfos, Component entrypoint, DependantMap map)
    {
        if (entrypoint == null) throw new ArgumentNullException(nameof(entrypoint));
        _componentInfos = componentInfos;
        _processPending = new Queue<Component>();
        _entrypoint = entrypoint;
        _map = map;
    }

    public void MarkComponent(Component component,
        GCComponentInfo.DependencyType type)
    {
        if (component == null) return; // typically means destroyed
        var dependencies = _componentInfos.TryGetInfo(component);
        if (dependencies == null) return;

        var dependantMap = _map[dependencies];
        if (dependantMap.TryGetValue(_entrypoint, out var existingFlags))
        {
            dependantMap[_entrypoint] = existingFlags | type;
        }
        else
        {
            _processPending.Enqueue(component);
            dependantMap.Add(_entrypoint, type);
        }
    }

    public void MarkRecursively()
    {
        while (_processPending.Count != 0)
        {
            var component = _processPending.Dequeue();
            var dependencies = _componentInfos.TryGetInfo(component);
            if (dependencies == null) continue; // not part of this Hierarchy Tree
            if (dependencies.Component == null) continue; // already destroyed

            foreach (var (dependency, type) in dependencies.Dependencies)
                MarkComponent(dependency, type);
        }
    }
}
