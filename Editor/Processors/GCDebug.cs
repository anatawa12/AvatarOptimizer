using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using nadena.dev.ndmf;
using nadena.dev.ndmf.runtime;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class GCDebugPass : Pass<GCDebugPass>
    {
        private readonly InternalGcDebugPosition _position;

        public GCDebugPass()
        {
            throw new NotSupportedException("GCDebugPass should not be instantiated directly. Use constructors instead.");
        }

        public GCDebugPass(InternalGcDebugPosition position)
        {
            _position = position;
        }

        public override string QualifiedName => typeof(GCDebugPass).FullName + "." + _position;
        public override string DisplayName => $"GC Debug Component Creation ({_position})";

        protected override void Execute(BuildContext context)
        {
            if (context.GetState<TraceAndOptimizeState>().GCDebug == (int)_position)
            {
                AddGCDebugInfo(context);
            }
        }

        public static void AddGCDebugInfo(BuildContext context)
        {
            var componentInfos = context.Extension<GCComponentInfoContext>();
            var avatarRootObject = context.AvatarRootObject;
            var entrypointMap = DependantMap.CreateEntrypointsMap(context);
            var rootObjectGcInfo = componentInfos.GetInfo(avatarRootObject.transform);

            foreach (var componentInfo in componentInfos.AllInformation)
            {
                var gcDebugInfo = componentInfo.Component.gameObject.AddComponent<GCDebugInfo>();
                gcDebugInfo.component = componentInfo.Component;
                gcDebugInfo.activeness = GCDebugInfo.ActivenessFromBool(componentInfo.Activeness);
                gcDebugInfo.isEntryPoint = componentInfo.EntrypointComponent;
                gcDebugInfo.entryPoints = entrypointMap[componentInfo].Select(GCDebugInfo.ComponentTypePair.From).ToArray();
                gcDebugInfo.dependencies = componentInfo.Dependencies.Select(GCDebugInfo.ComponentTypePair.From).ToArray();
            }

            // Register all GCDebugInfo components
            foreach (var gcDebugInfo in avatarRootObject.GetComponents<GCDebugInfo>())
            {
                componentInfos.NewComponent(gcDebugInfo);
                rootObjectGcInfo.AddDependency(gcDebugInfo);
                rootObjectGcInfo.AddDependency(gcDebugInfo.component);
            }

            var debugRoot = avatarRootObject.AddComponent<GCDebugRoot>();
            componentInfos.NewComponent(debugRoot);
            rootObjectGcInfo.AddDependency(debugRoot);
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
                        if (gcData.component is null) continue; // use is instead of null to get type information of missing component
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
            public Component? component;
            public Activeness activeness;
            public bool isEntryPoint;
            public ComponentTypePair[] dependencies = Array.Empty<ComponentTypePair>();
            public ComponentTypePair[] entryPoints = Array.Empty<ComponentTypePair>();

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

                    EditorGUI.ObjectField(labelPosition, property.FindPropertyRelative("component"), GUIContent.none);
                    GUI.Label(rect, ((GCComponentInfo.DependencyType)property.FindPropertyRelative("type").intValue).ToString());
                }
            }
        }
    }
}
