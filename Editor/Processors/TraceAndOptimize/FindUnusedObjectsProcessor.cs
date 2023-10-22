using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class FindUnusedObjects : Pass<FindUnusedObjects>
    {
        public override string DisplayName => "T&O: FindUnusedObjects";

        protected override void Execute(BuildContext context)
        {
            var state = context.GetState<TraceAndOptimizeState>();
            if (!state.RemoveUnusedObjects) return;

            if (state.UseLegacyGC)
            {
                new LegacyGC().Process();
            }
            else
            {
                var processor = new FindUnusedObjectsProcessor(context, state);
                processor.ProcessNew();
            }
        }
    }

    internal readonly struct MarkObjectContext {
        private readonly GCComponentInfoHolder _componentInfos;

        private readonly Queue<Component> _processPending;
        private readonly Component _entrypoint;

        public MarkObjectContext(GCComponentInfoHolder componentInfos, Component entrypoint)
        {
            _componentInfos = componentInfos;
            _processPending = new Queue<Component>();
            _entrypoint = entrypoint;
        }

        public void MarkComponent(Component component,
            GCComponentInfo.DependencyType type)
        {
            var dependencies = _componentInfos.TryGetInfo(component);
            if (dependencies == null) return;

            if (dependencies.DependantEntrypoint.TryGetValue(_entrypoint, out var existingFlags))
            {
                dependencies.DependantEntrypoint[_entrypoint] = existingFlags | type;
            }
            else
            {
                _processPending.Enqueue(component);
                dependencies.DependantEntrypoint.Add(_entrypoint, type);
            }
        }

        public void MarkRecursively()
        {
            while (_processPending.Count != 0)
            {
                var component = _processPending.Dequeue();
                var dependencies = _componentInfos.TryGetInfo(component);
                if (dependencies == null) continue; // not part of this Hierarchy Tree

                foreach (var (dependency, type) in dependencies.Dependencies)
                    MarkComponent(dependency, type);
            }
        }
    }

    internal readonly struct FindUnusedObjectsProcessor
    {
        private readonly ImmutableModificationsContainer _modifications;
        private readonly BuildContext _context;
        private readonly HashSet<GameObject> _exclusions;
        private readonly bool _preserveEndBone;
        private readonly bool _noConfigureMergeBone;
        private readonly bool _gcDebug;

        public FindUnusedObjectsProcessor(BuildContext context, TraceAndOptimizeState state)
        {
            _context = context;

            _modifications = state.Modifications;
            _preserveEndBone = state.PreserveEndBone;
            _noConfigureMergeBone = state.NoConfigureMergeBone;
            _gcDebug = state.GCDebug;
            _exclusions = state.Exclusions;
        }

        public void ProcessNew()
        {
            var componentInfos = new GCComponentInfoHolder(_modifications, _context.AvatarRootObject);
            Mark(componentInfos);
            if (_gcDebug)
            {
                GCDebug(componentInfos);
                return;
            }
            Sweep(componentInfos);
            if (!_noConfigureMergeBone)
                MergeBone(componentInfos);
        }

        private void Mark(GCComponentInfoHolder componentInfos)
        {
            // first, collect usages
            var collector = new ComponentDependencyCollector(_context, _preserveEndBone, componentInfos);
            collector.CollectAllUsages();

            // then, mark and sweep.

            // entrypoint for mark & sweep is active-able GameObjects
            foreach (var gameObject in CollectAllActiveAbleGameObjects())
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (componentInfos.GetInfo(component).EntrypointComponent)
                {
                    var markContext = new MarkObjectContext(componentInfos, component);
                    markContext.MarkComponent(component, GCComponentInfo.DependencyType.Normal);
                    markContext.MarkRecursively();
                }
            }

            if (_exclusions.Count != 0) {
                // excluded GameObjects must be exists
                var markContext = new MarkObjectContext(componentInfos, _context.AvatarRootTransform);

                foreach (var gameObject in _exclusions)
                foreach (var component in gameObject.GetComponents<Component>())
                    markContext.MarkComponent(component, GCComponentInfo.DependencyType.Normal);

                markContext.MarkRecursively();
            }

        }

        private void Sweep(GCComponentInfoHolder componentInfos)
        {
            foreach (var component in _context.GetComponents<Component>())
            {
                // null values are ignored
                if (!component) continue;

                if (componentInfos.GetInfo(component).DependantEntrypoint.Count == 0)
                {
                    if (component is Transform)
                    {
                        // Treat Transform Component as GameObject because they are two sides of the same coin
                        Object.DestroyImmediate(component.gameObject);
                    }
                    else
                    {
                        Object.DestroyImmediate(component);
                    }
                }
            }
        }

        private void MergeBone(GCComponentInfoHolder componentInfos)
        {
            ConfigureRecursive(_context.AvatarRootTransform, _modifications);

            // returns (original mergedChildren, list of merged children if merged, and null if not merged)
            //[CanBeNull]
            (bool, List<Transform>) ConfigureRecursive(Transform transform, ImmutableModificationsContainer modifications)
            {
                var mergedChildren = true;
                var afterChildren = new List<Transform>();
                foreach (var child in transform.DirectChildrenEnumerable())
                {
                    var (newMergedChildren, newChildren) = ConfigureRecursive(child, modifications);
                    if (newChildren == null)
                    {
                        mergedChildren = false;
                        afterChildren.Add(child);
                    }
                    else
                    {
                        mergedChildren &= newMergedChildren;
                        afterChildren.AddRange(newChildren);
                    }
                }

                const GCComponentInfo.DependencyType AllowedUsages =
                    GCComponentInfo.DependencyType.Bone
                    | GCComponentInfo.DependencyType.Parent
                    | GCComponentInfo.DependencyType.ComponentToTransform;

                // functions for make it easier to know meaning of result
                (bool, List<Transform>) YesMerge() => (mergedChildren, afterChildren);
                (bool, List<Transform>) NotMerged() => (mergedChildren, null);

                // Already Merged
                if (transform.GetComponent<MergeBone>()) return YesMerge();
                // Components must be Transform Only
                if (transform.GetComponents<Component>().Length != 1) return NotMerged();
                // The bone cannot be used generally
                if ((componentInfos.GetInfo(transform).AllUsages & ~AllowedUsages) != 0) return NotMerged();
                // must not be animated
                if (TransformAnimated(transform, modifications)) return NotMerged();

                if (!mergedChildren)
                {
                    if (GameObjectAnimated(transform, modifications)) return NotMerged();

                    var localScale = transform.localScale;
                    var identityTransform = localScale == Vector3.one && transform.localPosition == Vector3.zero &&
                                            transform.localRotation == Quaternion.identity;

                    if (!identityTransform)
                    {
                        var childrenTransformAnimated = afterChildren.Any(x => TransformAnimated(x, modifications));
                        if (childrenTransformAnimated)
                            // if this is not identity transform, animating children is not good
                            return NotMerged();

                        if (!MergeBoneProcessor.ScaledEvenly(localScale))
                            // non even scaling is not possible to reproduce in children
                            return NotMerged();
                    }
                }

                if (!transform.gameObject.GetComponent<MergeBone>())
                    transform.gameObject.AddComponent<MergeBone>().avoidNameConflict = true;

                return YesMerge();
            }

            bool TransformAnimated(Transform transform, ImmutableModificationsContainer modifications)
            {
                var transformProperties = modifications.GetModifiedProperties(transform);
                if (transformProperties.Count != 0)
                {
                    // TODO: constant animation detection
                    foreach (var transformProperty in TransformProperties)
                        if (transformProperties.ContainsKey(transformProperty))
                            return true;
                }

                return false;
            }

            bool GameObjectAnimated(Transform transform, ImmutableModificationsContainer modifications)
            {
                var objectProperties = modifications.GetModifiedProperties(transform.gameObject);

                if (objectProperties.ContainsKey("m_IsActive"))
                    return true;

                return false;
            }
        }

        private static readonly string[] TransformProperties =
        {
            "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w",
            "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", 
            "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z", 
            "localEulerAnglesRaw.x", "localEulerAnglesRaw.y", "localEulerAnglesRaw.z"
        };

        private IEnumerable<GameObject> CollectAllActiveAbleGameObjects()
        {
            var queue = new Queue<GameObject>();
            queue.Enqueue(_context.AvatarRootTransform.gameObject);

            while (queue.Count != 0)
            {
                var gameObject = queue.Dequeue();
                var activeNess = _modifications.GetConstantValue(gameObject, "m_IsActive", gameObject.activeSelf);
                switch (activeNess)
                {
                    case null:
                    case true:
                        // This GameObject can be active
                        yield return gameObject;
                        foreach (var transform in gameObject.transform.DirectChildrenEnumerable())
                            queue.Enqueue(transform.gameObject);
                        break;
                    case false:
                        // This GameObject and their children will never be active
                        break;
                }
            }
        }
        
        private void GCDebug(GCComponentInfoHolder componentInfos)
        {
            foreach (var componentInfo in componentInfos.AllInformation)
            {
                var gcDebugInfo = componentInfo.Component.gameObject.AddComponent<GCDebugInfo>();
                gcDebugInfo.component = componentInfo.Component;
                gcDebugInfo.activeness = GCDebugInfo.ActivenessFromBool(componentInfo.Activeness);
                gcDebugInfo.isEntryPoint = componentInfo.EntrypointComponent;
                gcDebugInfo.entryPoints = componentInfo.DependantEntrypoint.Select(GCDebugInfo.ComponentTypePair.From).ToArray();
                gcDebugInfo.dependencies = componentInfo.Dependencies.Select(GCDebugInfo.ComponentTypePair.From).ToArray();
            }

            throw new System.NotImplementedException();
        }

        class GCDebugRoot : MonoBehaviour { }

        [CustomEditor(typeof(GCDebugRoot))]
        class GCDebugRootEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                if (GUILayout.Button("Copy All Data"))
                {
                    GUIUtility.systemCopyBuffer = CreateData();
                }

                if (GUILayout.Button("Save All Data"))
                {
                    var path = EditorUtility.SaveFilePanel("DebugGCData", "", "data.txt", "txt");
                    if (!string.IsNullOrEmpty(path))
                    {
                        System.IO.File.WriteAllText(path, CreateData());
                    }
                }

                string CreateData()
                {
                    var root = ((Component)target).gameObject;
                    var collect = new StringBuilder();
                    foreach (var gcData in root.GetComponentsInChildren<GCDebugInfo>(true))
                    {
                        collect.Append(RuntimeUtil.RelativePath(root, gcData.gameObject))
                            .Append("(").Append(gcData.component.GetType().Name).Append("):\n");
                        collect.Append("  IsEntryPoint: ").Append(gcData.isEntryPoint).Append('\n');
                        collect.Append("  ActiveNess: ").Append(gcData.activeness).Append('\n');
                        collect.Append("  Dependencies:\n");
                        foreach (var line in PairsToStrings(root, gcData.dependencies))
                            collect.Append("    ").Append(line).Append("\n");
                        collect.Append("  EntryPoints:\n");
                        foreach (var line in PairsToStrings(root, gcData.entryPoints))
                            collect.Append("    ").Append(line).Append("\n");
                        collect.Append("\n");
                    }

                    return collect.ToString();
                }

                IEnumerable<string> PairsToStrings(GameObject root, GCDebugInfo.ComponentTypePair[] pairs) =>
                    from pair in pairs
                    where pair.component != null
                    let path = RuntimeUtil.RelativePath(root, pair.component.gameObject)
                    let type = pair.component.GetType().Name
                    let processing = $"{path}({type}): {pair.type}"
                    orderby processing
                    select processing;
            }
        }

        class GCDebugInfo : MonoBehaviour
        {
            public Component component;
            public Activeness activeness;
            public bool isEntryPoint;
            public ComponentTypePair[] dependencies;
            public ComponentTypePair[] entryPoints;

            public static Activeness ActivenessFromBool(bool? activeness)
            {
                if (activeness == true)
                    return Activeness.Active;
                if (activeness == false)
                    return Activeness.Inactive;
                return Activeness.Variable;
            }

            public enum Activeness
            {
                Active,
                Inactive,
                Variable
            }

            [Serializable]
            public struct ComponentTypePair
            {
                public Component component;
                public GCComponentInfo.DependencyType type;

                public static ComponentTypePair From(KeyValuePair<Component, GCComponentInfo.DependencyType> arg)
                {
                    return new ComponentTypePair
                    {
                        component = arg.Key,
                        type = arg.Value,
                    };
                }
            }

            [CustomPropertyDrawer(typeof(ComponentTypePair))]
            private class ComponentTypePairEditor : PropertyDrawer
            {
                public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
                {
                    var indent = EditorGUI.indentLevel * 15f;
                    const float spacing = 2.0f;
                    var labelPosition = new Rect(position.x + indent, position.y, EditorGUIUtility.labelWidth - indent, position.height);
                    var rect = new Rect(position.x + EditorGUIUtility.labelWidth + spacing, position.y, 
                        position.width - EditorGUIUtility.labelWidth - spacing, position.height);

                    EditorGUI.ObjectField(labelPosition, property.FindPropertyRelative("component"));
                    GUI.Label(rect, ((GCComponentInfo.DependencyType)property.FindPropertyRelative("type").intValue).ToString());
                }
            }
        }
    }
}
