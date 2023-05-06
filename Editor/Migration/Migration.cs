using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Anatawa12.AvatarOptimizer.PrefabSafeSet;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using VRC.Dynamics;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Migration
{
    [InitializeOnLoad]
    public static class Migration
    {
        static Migration()
        {
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (PrereleaseStateDetector.AutoDetectionInProgress) return; // try next tick
            try
            {
                if (PrereleaseStateDetector.MigrationRequired())
                {
                    if (DoMigrate())
                        PrereleaseStateDetector.MigrationFinished();
                }
                else
                {
                    PrereleaseStateDetector.MigrationFinished();
                }

            }
            finally
            {
                EditorApplication.update -= Update;
            }
        }

        private static bool DoMigrate()
        {
            var result = EditorUtility.DisplayDialogComplex("Migration Required!",
                @"Upgrading AvatarOptimizer between major versions detected!
BACK UP YOUR PROJECT!
You have to migrate all scenes and prefabs to make AvatarOptimizer works correctly!
This may took long time and may break your project!
If you don't have backup, please migrate after creating backup!
Until migration is proceed, this tool will be ask you for migration for each time relaunching Unity.

Do you want to migrate project now?",
                "Migrate everything",
                "Cancel",
                "Migrate Prefabs Only");
            switch (result)
            {
                case 0: // migrate all
                    MigrateEverything();
                    return true;
                case 1: // do not migrate
                    return false;
                case 2: // migrate prefabs
                    MigratePrefabsOnly();
                    return true;
                default: // unknown
                    return false; 
            }
        }

        [MenuItem("Tools/Avatar Optimizer/Migrate Prefabs")]
        private static void MigratePrefabsOnly()
        {
            try
            {
                PreMigration();
                var prefabs = GetPrefabs();

                MigratePrefabs(prefabs, (name, i) => EditorUtility.DisplayProgressBar(
                    "Migrating Prefabs",
                    $"{name} ({i} / {prefabs.Count})",
                    i / (float)prefabs.Count));
            }
            catch
            {
                EditorUtility.DisplayDialog("Error!", "Error in migration process!", "OK");
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                PostMigration();
            }
        }

        [MenuItem("Tools/Avatar Optimizer/Migrate Scenes Only")]
        private static void MigrateScenesOnly()
        {
            try
            {
                PreMigration();
                var scenePaths = AssetDatabase.FindAssets("t:scene", new []{"Assets/"}).Select(AssetDatabase.GUIDToAssetPath).ToList();

                MigrateAllScenes(scenePaths, (name, i) => EditorUtility.DisplayProgressBar(
                    "Migrating Scenes",
                    $"{name} ({i} / {scenePaths.Count})",
                    i / (float)scenePaths.Count));
            }
            catch
            {
                EditorUtility.DisplayDialog("Error!", "Error in migration process!", "OK");
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                PostMigration();
            }
        }

        [MenuItem("Tools/Avatar Optimizer/Migrate Everything")]
        private static void MigrateEverything()
        {
            try
            {
                PreMigration();
                var prefabs = GetPrefabs();
                var scenePaths = AssetDatabase.FindAssets("t:scene", new []{"Assets/"}).Select(AssetDatabase.GUIDToAssetPath).ToList();
                float totalCount = prefabs.Count + scenePaths.Count;

                MigratePrefabs(prefabs, (name, i) => EditorUtility.DisplayProgressBar(
                    "Migrating Everything",
                    $"{name} (Prefabs) ({i} / {totalCount})",
                    i / totalCount));

                MigrateAllScenes(scenePaths, (name, i) => EditorUtility.DisplayProgressBar(
                    "Migrating Everything",
                    $"{name} (Scenes) ({prefabs.Count + i} / {totalCount})",
                    (prefabs.Count + i) / totalCount));
            }
            catch
            {
                EditorUtility.DisplayDialog("Error!", "Error in migration process!", "OK");
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                PostMigration();
            }
        }

        [MenuItem("Tools/Avatar Optimizer/Force v0.1->0.2 Migrate Everything")]
        private static void ForceMigrateEverything()
        {
            if (!EditorUtility.DisplayDialog("WARNING",
                    "Force migration will LOST any changes you made in v0.2.x.\n" +
                    "Do you ACTUALLY want to re-migrate everything?",
                    "Migrate", "Cancel"))
                return; // cancel

            try
            {
                var prefabs = GetPrefabs();
                var scenePaths = AssetDatabase.FindAssets("t:scene", new []{"Assets/"}).Select(AssetDatabase.GUIDToAssetPath).ToList();
                float totalCount = prefabs.Count + scenePaths.Count;

                MigratePrefabs(prefabs, (name, i) => EditorUtility.DisplayProgressBar(
                    "Force Migrating Everything",
                    $"{name} (Prefabs) ({i} / {totalCount})",
                    i / totalCount),
                    forceVersion: 1);

                MigrateAllScenes(scenePaths, (name, i) => EditorUtility.DisplayProgressBar(
                    "Force Migrating Everything",
                    $"{name} (Scenes) ({prefabs.Count + i} / {totalCount})",
                    (prefabs.Count + i) / totalCount),
                    forceVersion: 1);
            }
            catch
            {
                EditorUtility.DisplayDialog("Error!", "Error in migration process!", "OK");
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void MigratePrefabs(List<GameObject> prefabAssets, Action<string, int> progressCallback, 
            int forceVersion = int.MaxValue)
        {
            for (var i = 0; i < prefabAssets.Count; i++)
            {
                var prefabAsset = prefabAssets[i];
                progressCallback(prefabAsset.name, i);

                var modified = false;

                try
                {
                    foreach (var component in prefabAsset.GetComponentsInChildren<AvatarTagComponent>(true))
                        modified |= MigrateComponent(component, forceVersion);
                }
                catch (Exception e)
                {
                    throw new Exception($"Migrating Prefab {prefabAsset.name}: {e.Message}", e);
                }

                if (modified)
                    PrefabUtility.SavePrefabAsset(prefabAsset);
            }
            progressCallback("finish Prefabs", prefabAssets.Count);
        }

        private static void MigrateAllScenes(List<string> scenePaths, Action<string, int> progressCallback,
            int forceVersion = int.MaxValue)
        {
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
            // skip saving
            if (scenes.Any(x => x.isDirty))
                EditorSceneManager.SaveScenes(scenes);
            var openingScenePaths = scenes.Select(x => x.path).ToArray();
            if (openingScenePaths.Any(string.IsNullOrEmpty))
                openingScenePaths = null;
            // load each scene and migrate scene
            for (var i = 0; i < scenePaths.Count; i++)
            {
                var scenePath = scenePaths[i];
                var scene = EditorSceneManager.OpenScene(scenePath);

                progressCallback(scene.name, i);

                var modified = false;

                try
                {
                    foreach (var rootGameObject in scene.GetRootGameObjects())
                    foreach (var component in rootGameObject.GetComponentsInChildren<AvatarTagComponent>(true))
                        modified |= MigrateComponent(component, forceVersion);
                }
                catch (Exception e)
                {
                    throw new Exception($"Migrating Scene {scene.name}: {e.Message}", e);
                }

                if (modified)
                    EditorSceneManager.SaveScene(scene);
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            progressCallback("finish Prefabs", scenePaths.Count);

            if (openingScenePaths != null
                && EditorUtility.DisplayDialog("Reopen?", "Do you want to reopen previously opened scenes?", "Yes",
                    "No"))
            {
                EditorSceneManager.OpenScene(openingScenePaths[0]);
                foreach (var openingScenePath in openingScenePaths.Skip(1))
                    EditorSceneManager.OpenScene(openingScenePath, OpenSceneMode.Additive);
            }
        }

        private static bool MigrateComponent(AvatarTagComponent component, int forceVersion)
        {
            var nestCount = NestCount(component);
            var serialized = new SerializedObject(component);
            var saveVersionsProp = serialized.FindProperty(nameof(AvatarTagComponent.saveVersions));
            var version = nestCount < saveVersionsProp.arraySize
                ? saveVersionsProp.GetArrayElementAtIndex(nestCount).intValue
                : PrereleaseStateDetector.GetCurrentVersion();
            if (forceVersion < version) 
                version = forceVersion;
            saveVersionsProp.arraySize = nestCount + 1;
            var versionProp = saveVersionsProp.GetArrayElementAtIndex(nestCount);
            var modified = false;
            switch (version)
            {
#pragma warning disable CS0618
                case 1:
                    if (serialized.targetObject.GetType() == typeof(RemoveMeshInBox))
                    {
                        // format for RemoveMeshInBox is reverted to v1 in v3 so skip.
                        versionProp.intValue = 3;
                        modified = true;
                        goto case 3;
                    }
                    MigrateV1ToV2(nestCount, serialized);
                    versionProp.intValue = 2;
                    modified = true;
                    goto case 2;
                case 2:
                    MigrateV2ToV3(nestCount, serialized);
                    versionProp.intValue = 3;
                    modified = true;
                    goto case 3;
                case 3:
#pragma warning restore CS0618
                    // it's current version: save
                    serialized.ApplyModifiedProperties();
                    break;
                default:
                    Debug.LogWarning($"Unsupported serialized version {version} detected: {component} at {AssetDatabase.GetAssetPath(component)}");
                    break;
            }

            return modified;
        }

        // migration
#pragma warning disable CS0618
        private static readonly TypeId MigrateV1ToV2Types = new TypeId(new []
        {
            typeof(MergeSkinnedMesh),
            typeof(FreezeBlendShape),
            typeof(MergePhysBone),
            typeof(RemoveMeshInBox),
        });

        [Obsolete("migration process")]
        private static void MigrateV1ToV2(int nestCount, SerializedObject serialized)
        {
            switch (MigrateV1ToV2Types.GetType(serialized.targetObject.GetType()))
            {
                case 1:
                {
                    // MergeSkinnedMesh
                    // renderers -> renderersSet
                    MigrateSet(serialized.FindProperty(nameof(MergeSkinnedMesh.renderers)),
                        serialized.FindProperty(nameof(MergeSkinnedMesh.renderersSet)), nestCount,
                        x => x.objectReferenceValue, (x, y) => x.objectReferenceValue = y
                    );
                    
                    // renderers -> staticRenderersSet
                    MigrateSet(serialized.FindProperty(nameof(MergeSkinnedMesh.staticRenderers)),
                        serialized.FindProperty(nameof(MergeSkinnedMesh.staticRenderersSet)), nestCount,
                        x => x.objectReferenceValue, (x, y) => x.objectReferenceValue = y
                    );

                    // merges migration: do nothing: I recommend to merge materials &
                    // v1 format will list up materials to not be merged.
                    break;
                }
                case 2:
                {
                    // FreezeBlendShape
                    var shapeKeysProp = serialized.FindProperty(nameof(FreezeBlendShape.shapeKeys));
                    var freezeFlagsProp = serialized.FindProperty(nameof(FreezeBlendShape.freezeFlags));
                    var shapeKeysSetProp = serialized.FindProperty(nameof(FreezeBlendShape.shapeKeysSet));

                    if (shapeKeysProp.arraySize == freezeFlagsProp.arraySize)
                    {
                        // new v1 format: check for flags
                        var shapeKeys = ToArray(shapeKeysProp, x => x.stringValue);
                        var freezeFlags = ToArray(freezeFlagsProp, x => x.boolValue);
                        MigrateSet(shapeKeys.Where((_, i) => freezeFlags[i]),
                            shapeKeysSetProp,
                            nestCount,
                            x => x.stringValue,
                            (x, v) => x.stringValue = v);
                    }
                    else
                    {
                        // traditional v1 format: shapeKeysProp will have everything to be merged
                        MigrateSet(shapeKeysProp,
                            shapeKeysSetProp,
                            nestCount,
                            x => x.stringValue,
                            (x, v) => x.stringValue = v);
                    }
                    break;
                }
                case 3:
                {
                    // MergePhysBone
                    MigrateSet(serialized.FindProperty(nameof(MergePhysBone.components)),
                        serialized.FindProperty(nameof(MergePhysBone.componentsSet)), nestCount,
                        x => x.objectReferenceValue, (x, y) => x.objectReferenceValue = y
                    );
                    break;
                }
                case 4:
                {
                    // RemoveMeshInBox
                    throw new Exception("V1 to V2 migration for RemoveMeshInBox has been removed");
                }
            }
        }

        private static readonly TypeId MigrateV2ToV3Types = new TypeId(new []
        {
            typeof(RemoveMeshInBox),
            typeof(MergePhysBone),
        });

        [Obsolete("migration process")]
        private static void MigrateV2ToV3(int nestCount, SerializedObject serialized)
        {
            switch (MigrateV2ToV3Types.GetType(serialized.targetObject.GetType()))
            {
                case 1:
                {
                    // RemoveMeshInBox
                    SerializedProperty[] GetContainers()
                    {
                        var psList = serialized.FindProperty(nameof(RemoveMeshInBox.boxList));
                        // Container[]
                        var firstLayer =
                            psList.FindPropertyRelative(nameof(RemoveMeshInBox.BoundingBoxList.firstLayer));
                        // Layer[]
                        var prefabLayers =
                            psList.FindPropertyRelative(nameof(RemoveMeshInBox.BoundingBoxList.prefabLayers));
                        // Container[][]
                        var layerArrays = new SerializedProperty[prefabLayers.arraySize + 1];

                        layerArrays[0] = firstLayer;

                        for (var i = 0; i < prefabLayers.arraySize; ++i)
                            layerArrays[i + 1] = prefabLayers.GetArrayElementAtIndex(i)
                                .FindPropertyRelative(nameof(RemoveMeshInBox.BoundingBoxList.Layer.elements));

                        var count = layerArrays.Sum(x => x.arraySize);
                        var result = new SerializedProperty[count];

                        var k = 0;
                        foreach (var ary in layerArrays)
                            for (var j = 0; j < ary.arraySize; ++j)
                                result[k++] = ary.GetArrayElementAtIndex(j);

                        return result;
                    }

                    var containers = GetContainers();
                    var array = serialized.FindProperty(nameof(RemoveMeshInBox.boxes));
                    if (array.arraySize != containers.Length)
                        array.arraySize = containers.Length;

                    for (var i = 0; i < containers.Length; i++)
                    {
                        var container = containers[i];
                        var removed =
                            container.FindPropertyRelative(nameof(RemoveMeshInBox.BoundingBoxList.Container.removed));
                        var value = container.FindPropertyRelative(nameof(RemoveMeshInBox.BoundingBoxList.Container
                            .value));
                        var field = array.GetArrayElementAtIndex(i);

                        field.FindPropertyRelative(nameof(RemoveMeshInBox.BoundingBox.size)).vector3Value =
                            removed.boolValue
                                ? Vector3.zero
                                : value.FindPropertyRelative(nameof(RemoveMeshInBox.BoundingBox.size)).vector3Value;

                        field.FindPropertyRelative(nameof(RemoveMeshInBox.BoundingBox.center)).vector3Value =
                            field.FindPropertyRelative(nameof(RemoveMeshInBox.BoundingBox.center)).vector3Value;

                        field.FindPropertyRelative(nameof(RemoveMeshInBox.BoundingBox.rotation)).quaternionValue =
                            field.FindPropertyRelative(nameof(RemoveMeshInBox.BoundingBox.rotation)).quaternionValue;
                    }
                    break;
                }
                case 2:
                {
                    // MergePhysBone

                    // check rootTransform deletion
                    var root = serialized.FindProperty(nameof(MergePhysBone.rootTransform));
                    if (root.objectReferenceValue)
                    {
                        // might be incompatible changes.
                        var parent = (Transform)root.objectReferenceValue;
                        var components = EditorUtil<VRCPhysBoneBase>.Create(
                                serialized.FindProperty(nameof(MergePhysBone.componentsSet)), nestCount,
                                x => (VRCPhysBoneBase)x.objectReferenceValue, (x, y) => x.objectReferenceValue = y)
                            .Values.ToList();

                        if (components.Count != 0)
                        {
                            var targetParent = components[0].GetTarget().parent;
                            if (targetParent != parent || components.Count != parent.childCount)
                                FoundIncompatibleAsset(IncompatibilityKind.MergePhysBoneRootBone,
                                    serialized.targetObject);
                        }
                    }

                    serialized.FindProperty(nameof(MergePhysBone.immobileType)).boolValue
                        = serialized.FindProperty(nameof(MergePhysBone.immobile)).boolValue;

                    // integrationType was previously force
                    if (serialized.FindProperty(nameof(MergePhysBone.integrationType)).boolValue)
                    {
                        serialized.FindProperty(nameof(MergePhysBone.pull)).boolValue = true;
                        // They are force overriden by integrationType so there's no need to override
                        //serialized.FindProperty(nameof(MergePhysBone.spring)).boolValue = true;
                        //serialized.FindProperty(nameof(MergePhysBone.stiffness)).boolValue = true;
                        serialized.FindProperty(nameof(MergePhysBone.gravity)).boolValue = true;
                        serialized.FindProperty(nameof(MergePhysBone.gravityFalloff)).boolValue = true;
                        serialized.FindProperty(nameof(MergePhysBone.immobileType)).boolValue = true;
                        serialized.FindProperty(nameof(MergePhysBone.immobile)).boolValue = true;
                    }
                    break;
                }
            }
        }
#pragma warning restore CS0618

        // incompatibility warnings
        enum IncompatibilityKind
        {
            MergePhysBoneRootBone
        }

        // IncompatibilityKind -> asset path[]
        private static readonly Dictionary<IncompatibilityKind, HashSet<string>> IncompatibleAssets =
            new Dictionary<IncompatibilityKind, HashSet<string>>();

        private static void PreMigration()
        {
            IncompatibleAssets.Clear();
        }

        private static void FoundIncompatibleAsset(IncompatibilityKind kind, Object asset)
        {
            if (!IncompatibleAssets.TryGetValue(kind, out var set))
                IncompatibleAssets[kind] = set = new HashSet<string>();
            var id = GlobalObjectId.GetGlobalObjectIdSlow(asset);
            var path = AssetDatabase.GUIDToAssetPath(id.assetGUID.ToString());
            Debug.Log($"{kind} found for {path}");
            set.Add(path);
        }

        private static void PostMigration()
        {
            (string, string) IncompatibilityMessage(IncompatibilityKind kind)
            {
                switch (kind)
                {
                    case IncompatibilityKind.MergePhysBoneRootBone:
                        return ("We've removed root bone configuration of MergePhysBone in v0.3.0.",
                            "https://github.com/anatawa12/AvatarOptimizer/issues/62#issuecomment-1512586282");
                    default:
                        throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
                }
            }
            foreach (var keyValuePair in IncompatibleAssets)
            {
                if (keyValuePair.Value.Count != 0)
                {
                    var (message, link) = IncompatibilityMessage(keyValuePair.Key);
                    var messageBuilder = new StringBuilder(message);
                    messageBuilder.Append("\n This change affects the following assets:");
                    foreach (var path in keyValuePair.Value)
                        messageBuilder.Append(path).Append("\n");
                    messageBuilder.Append("\n for more details, click 'Read more' to read more details about this changes on github");

                    switch (EditorUtility.DisplayDialogComplex("Incompatibility Detected", messageBuilder.ToString(), 
                                "OK", "Dismiss", "Read More"))
                    {
                        case 0:
                        case 1:
                            break;
                        case 2:
                            Application.OpenURL(link);
                            break;
                    }
                }
            }
            IncompatibleAssets.Clear();
        }

        private static void MigrateSet<T>(
            SerializedProperty arrayProperty,
            SerializedProperty setProperty,
            int nestCount,
            Func<SerializedProperty, T> getValue,
            Action<SerializedProperty, T> setValue)
        {
            MigrateSet(ToArray(arrayProperty, getValue), setProperty, nestCount, getValue, setValue);
        }

        private static T[] ToArray<T>(SerializedProperty arrayProperty, Func<SerializedProperty, T> getValue)
        {
            var values = Enumerable.Range(0, arrayProperty.arraySize)
                .Select(arrayProperty.GetArrayElementAtIndex)
                .Select(getValue)
                .ToArray();

            return values;
        }

        private static void MigrateSet<T>(
            IEnumerable<T> values,
            SerializedProperty setProperty,
            int nestCount,
            Func<SerializedProperty, T> getValue,
            Action<SerializedProperty, T> setValue)
        {
            var renderersSet = EditorUtil<T>.Create(setProperty, nestCount, getValue, setValue);

            renderersSet.Clear();

            var valuesSet = new HashSet<T>(values.Where(value => value != null));

            foreach (var value in valuesSet)
                renderersSet.GetElementOf(value).EnsureAdded();

            Assert.IsTrue(valuesSet.SetEquals(renderersSet.Values));
        }

        private readonly struct TypeId
        {
            [CanBeNull] private readonly Dictionary<Type, int> _map;

            public TypeId(Type[] types)
            {
                _map = new Dictionary<Type, int>(types.Length);
                for (var i = 0; i < types.Length; i++)
                    _map[types[i]] = i + 1;
            }

            public int GetType(Type t) =>
                _map == null ? 0 : _map.TryGetValue(t, out var id) ? id : 0;
        }

        private static int NestCount(Object instance)
        {
            var nestCount = 0;
            while ((bool)(instance = PrefabUtility.GetCorrespondingObjectFromSource(instance)))
                nestCount++;

            return nestCount;
        }

        private class PrefabInfo
        {
            public readonly GameObject Prefab;
            public readonly List<PrefabInfo> Children = new List<PrefabInfo>();
            public readonly List<PrefabInfo> Parents = new List<PrefabInfo>();

            public PrefabInfo(GameObject prefab)
            {
                Prefab = prefab;
            }
        }

        /// <returns>List of prefab assets. parent prefab -> child prefab</returns>
        private static List<GameObject> GetPrefabs()
        {
            bool CheckPrefabType(PrefabAssetType type) =>
                type != PrefabAssetType.MissingAsset && type != PrefabAssetType.Model &&
                type != PrefabAssetType.NotAPrefab;

            var allPrefabRoots = AssetDatabase.FindAssets("t:prefab", new []{"Assets/"})
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(x => x)
                .Where(x => CheckPrefabType(PrefabUtility.GetPrefabAssetType(x)))
                .Where(x => x.GetComponentsInChildren<AvatarTagComponent>(true).Length != 0)
                .ToArray();

            var sortedVertices = new List<GameObject>();

            var vertices = new LinkedList<PrefabInfo>(allPrefabRoots.Select(prefabRoot => new PrefabInfo(prefabRoot)));

            // assign Parents and Children here.
            {
                var vertexLookup = vertices.ToDictionary(x => x.Prefab, x => x);
                foreach (var vertex in vertices)
                {
                    foreach (var parentPrefab in vertex.Prefab
                                 .GetComponentsInChildren<Transform>(true)
                                 .Select(x => x.gameObject)
                                 .Where(PrefabUtility.IsAnyPrefabInstanceRoot)
                                 .Select(PrefabUtility.GetCorrespondingObjectFromSource)
                                 .Select(x => x.transform.root.gameObject))
                    {
                        if (vertexLookup.TryGetValue(parentPrefab, out var parent))
                        {
                            vertex.Parents.Add(parent);
                            parent.Children.Add(vertex);
                        }
                    }
                }
            }

            // Orphaned nodes with no parents or children go first
            {
                var it = vertices.First;
                while (it != null)
                {
                    var cur = it;
                    it = it.Next;
                    if (cur.Value.Children.Count != 0 || cur.Value.Parents.Count != 0) continue;
                    sortedVertices.Add(cur.Value.Prefab);
                    vertices.Remove(cur);
                }
            }

            var openSet = new Queue<PrefabInfo>();

            // Find root nodes with no parents
            foreach (var vertex in vertices.Where(vertex => vertex.Parents.Count == 0))
                openSet.Enqueue(vertex);

            var visitedVertices = new HashSet<PrefabInfo>();
            while (openSet.Count > 0)
            {
                var vertex = openSet.Dequeue();

                if (visitedVertices.Contains(vertex))
                {
                    continue;
                }

                if (vertex.Parents.Count > 0)
                {
                    var neededParentVisit = false;

                    foreach (var vertexParent in vertex.Parents.Where(vertexParent => !visitedVertices.Contains(vertexParent)))
                    {
                        neededParentVisit = true;
                        openSet.Enqueue(vertexParent);
                    }

                    if (neededParentVisit)
                    {
                        // Re-queue to visit after we have traversed the node's parents
                        openSet.Enqueue(vertex);
                        continue;
                    }
                }

                visitedVertices.Add(vertex);
                sortedVertices.Add(vertex.Prefab);

                foreach (var vertexChild in vertex.Children)
                    openSet.Enqueue(vertexChild);
            }

            // Sanity check
            foreach (var vertex in vertices.Where(vertex => !visitedVertices.Contains(vertex)))
                throw new Exception($"Invalid DAG state: node '{vertex.Prefab}' was not visited.");

            return sortedVertices;
        }
    }

    public static class CopySerializedPropertyValue
    {
        public static void Copy(SerializedProperty source, SerializedProperty dest)
        {
            if (dest.propertyType == SerializedPropertyType.Generic)
                CopyBetweenTwoRecursively(source, dest);
            else
                CopyBetweenTwoValue(source, dest);
        }

        private static void CopyBetweenTwoRecursively(SerializedProperty source, SerializedProperty dest)
        {
            var srcIter = source.Copy();
            var dstIter = dest.Copy();
            var srcEnd = source.GetEndProperty();
            var dstEnd = dest.GetEndProperty();
            var enterChildren = true;
            while (srcIter.Next(enterChildren) && !SerializedProperty.EqualContents(srcIter, srcEnd))
            {
                var destCheck = dstIter.Next(enterChildren) && !SerializedProperty.EqualContents(dstIter, dstEnd);
                Assert.IsTrue(destCheck);

                switch (dstIter.propertyType)
                {
                    case SerializedPropertyType.FixedBufferSize:
                        Assert.AreEqual(srcIter.fixedBufferSize, dstIter.fixedBufferSize);
                        break;
                    case SerializedPropertyType.Generic:
                        break;
                    default:
                        CopyBetweenTwoValue(srcIter, dstIter);
                        break;
                }

                enterChildren = dstIter.propertyType == SerializedPropertyType.Generic;
            }

            {
                var destCheck = dstIter.NextVisible(enterChildren) && !SerializedProperty.EqualContents(dstIter, dstEnd);
                Assert.IsFalse(destCheck);
            }
        }

        private static void CopyBetweenTwoValue(SerializedProperty src, SerializedProperty dst)
        {
            switch (dst.propertyType)
            {
                case SerializedPropertyType.Generic:
                    throw new InvalidOperationException("for generic, use CopyBetweenTwoRecursively");
                case SerializedPropertyType.Integer:
                    dst.intValue = src.intValue;
                    break;
                case SerializedPropertyType.Boolean:
                    dst.boolValue = src.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    dst.floatValue = src.floatValue;
                    break;
                case SerializedPropertyType.String:
                    dst.stringValue = src.stringValue;
                    break;
                case SerializedPropertyType.Color:
                    dst.colorValue = src.colorValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    dst.objectReferenceValue = src.objectReferenceValue;
                    break;
                case SerializedPropertyType.LayerMask:
                    dst.intValue = src.intValue;
                    break;
                case SerializedPropertyType.Enum:
                    dst.intValue = src.intValue;
                    break;
                case SerializedPropertyType.Vector2:
                    dst.vector2Value = src.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    dst.vector3Value = src.vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    dst.vector4Value = src.vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    dst.rectValue = src.rectValue;
                    break;
                case SerializedPropertyType.ArraySize:
                    dst.intValue = src.intValue;
                    break;
                case SerializedPropertyType.Character:
                    dst.intValue = src.intValue;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    dst.animationCurveValue = src.animationCurveValue;
                    break;
                case SerializedPropertyType.Bounds:
                    dst.boundsValue = src.boundsValue;
                    break;
                case SerializedPropertyType.Gradient:
                    //dst.gradientValue = src.gradientValue;
                    //break;
                    throw new InvalidOperationException("unsupported type: Gradient");
                case SerializedPropertyType.Quaternion:
                    dst.quaternionValue = src.quaternionValue;
                    break;
                case SerializedPropertyType.ExposedReference:
                    dst.exposedReferenceValue = src.exposedReferenceValue;
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    throw new InvalidOperationException("unsupported type: FixedBufferSize");
                case SerializedPropertyType.Vector2Int:
                    dst.vector2IntValue = src.vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    dst.vector3IntValue = src.vector3IntValue;
                    break;
                case SerializedPropertyType.RectInt:
                    dst.rectIntValue = src.rectIntValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    dst.boundsIntValue = src.boundsIntValue;
                    break;
                case SerializedPropertyType.ManagedReference:
                    throw new InvalidOperationException("unsupported type: ManagedReference");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
