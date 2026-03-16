using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using Anatawa12.AvatarOptimizer.Processors;
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.Dynamics;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.BugReportHelper;

// バグレポートに必要なデータを集めるツール
// 常に集めるもの(再配布禁止系ライセンス違反のリスクが低いもの)
// - 各種パッケージのバージョン情報
// - Unityのバージョン情報
// - OSの情報
// - ターゲットプラットフォームとそれの設定
// - AnimatorParserの結果
// - アバターのヒエラルキー情報(EditモードとAAO実行直前の二回)
//   - ヒエラルキー構造
//   - コンポーネントの付与情報
//     - AAOの設定値
//   - メッシュの名前・blendshape名一覧
//   - VRCFuryを使用してるか。
// 再配布禁止系ライセンス違反のリスクが有るもの (opt-in, 警告を出す)
// - アバターのビルド済ファイル
internal class BugReportHelper : EditorWindow
{
    // create gui to trigger bug report generation.
    [MenuItem("Tools/Avatar Optimizer/Bug Report Helper", priority = 901)]
    public static void ShowWindow()
    {
        var window = GetWindow<BugReportHelper>("AAO Bug Report Helper");
        window.minSize = new Vector2(400, 200);
        if (window.targetAvatar == null)
        {
            var selected = Selection.activeGameObject;
            if (selected != null)
                window.targetAvatar = selected;
        }
    }

    public GameObject? targetAvatar;
    public TracingArea tracing = TracingArea.All;
    public bool tracingOpen;

    private static TracingArea[] TracingAreas = (TracingArea[])Enum.GetValues(typeof(TracingArea));

    private static class Styles
    {
        public static GUIStyle wrapLabel = new GUIStyle(EditorStyles.label) { wordWrap = true };
    }

    private void OnGUI()
    {
        AAOL10N.DrawLanguagePicker();

        GUILayout.Label("Bug Report Helper", EditorStyles.boldLabel);
        targetAvatar = (GameObject)EditorGUILayout.ObjectField("Target Avatar", targetAvatar, typeof(GameObject), allowSceneObjects: true);

        GUILayout.Space(10);

        GUILayout.Label(AAOL10N.Tr("BugReportHelper:description"), Styles.wrapLabel);

        // label
        tracingOpen = EditorGUILayout.Foldout(tracingOpen, "Detailed Log Settings");
        if (tracingOpen)
        {
            EditorGUI.indentLevel++;
            GUILayout.Label(AAOL10N.Tr("BugReportHelper:tracing-log"), Styles.wrapLabel);
            foreach (var value in TracingAreas)
            {
                if (value == TracingArea.None || value == TracingArea.All) continue;
                var enabled = (tracing & value) != 0;
                var newEnabled = EditorGUILayout.ToggleLeft($"{value}", enabled);
                if (enabled != newEnabled)
                {
                    if (newEnabled) tracing |= value;
                    else tracing &= ~value;
                }
            }
            EditorGUI.indentLevel--;

        }

        GUILayout.Space(10);

        if (targetAvatar == null)
        {
            EditorGUILayout.HelpBox(AAOL10N.Tr("BugReportHelper:select-avatar"), MessageType.Info);
            return;
        }

        if (!nadena.dev.ndmf.runtime.RuntimeUtil.IsAvatarRoot(targetAvatar.transform))
        {
            EditorGUILayout.HelpBox(AAOL10N.Tr("BugReportHelper:not-avatar-root"), MessageType.Info);
            return;
        }

        if (!targetAvatar.activeInHierarchy)
        {
            EditorGUILayout.HelpBox(AAOL10N.Tr("BugReportHelper:avatar-inactive"), MessageType.Info);
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorGUILayout.HelpBox(AAOL10N.Tr("BugReportHelper:play-mode"), MessageType.Warning);
        }

        EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);

        if (GUILayout.Button("Save Bug Report"))
        {
            try
            {
                var savePath = EditorUtility.SaveFilePanel("Save Bug Report", "", "AAO-BugReport.gz", "gz");
                if (!string.IsNullOrEmpty(savePath))
                {
                    var reportFile = RunBuild(targetAvatar, tracing);
                    reportFile.DiffCompress();
                    var contents = reportFile.ToString();
                    // compress with GZip
                    {
                        using var fileStream = File.Create(savePath);
                        using var gzipStream = new System.IO.Compression.GZipStream(fileStream,
                            System.IO.Compression.CompressionLevel.Optimal);
                        using var writer = new StreamWriter(gzipStream, Encoding.UTF8);
                        writer.Write(contents);
                        writer.Flush();
                    }
                    EditorUtility.DisplayDialog("Bug Report Generated", "Bug report has been generated and saved successfully.", "OK");
                    // open the folder
                    EditorUtility.RevealInFinder(savePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Error", $"An error occurred while generating the bug report. See console for details.", "OK");
            }
        }

        if (GUILayout.Button("Copy Bug Report to Clipboard"))
        {
            // confirm large data warning
            if (!EditorUtility.DisplayDialog("Copy Bug Report to Clipboard",
                    AAOL10N.Tr("BugReportHelper:copy-warning"),
                    "Yes", "No"))
                goto end_of_button;

            try
            {
                var reportFile = RunBuild(targetAvatar, tracing);
                reportFile.DiffCompress();
                GUIUtility.systemCopyBuffer = reportFile.ToString();
                EditorUtility.DisplayDialog("Bug Report Copied", "Bug report has been copied to clipboard successfully.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Error", $"An error occurred while generating the bug report. See console for details.", "OK");
            }

            end_of_button:;
        }

        EditorGUI.EndDisabledGroup();
    }

    public static ReportFile RunBuild(GameObject avatar, TracingArea tracing)
    {
        var clonedAvatar = Instantiate(avatar);
        var tracingConfig = Tracing.Enabled;
        try
        {
            Tracing.Enabled = tracing;
            var reportFile = new ReportFile();

            // collect project information
            reportFile.AddField("Unity-Version", Application.unityVersion);
            reportFile.AddField("Build-Target", EditorUserBuildSettings.activeBuildTarget.ToString());
            reportFile.AddField("Operating-System", SystemInfo.operatingSystem);
            reportFile.AddField("NDMF-Platform", AmbientPlatform.CurrentPlatform.QualifiedName);
            reportFile.AddField("Avatar-Name", avatar.name);

            foreach (var (package, version) in PackageManagerInfoCollector.UpmLockedPackages())
                reportFile.AddField("Upm-Dependency", $"{package}@{version}");
            foreach (var (package, version) in PackageManagerInfoCollector.VpmLockedPackages())
                reportFile.AddField("Vpm-Dependency", $"{package}@{version}");

            // collect environment information.
            reportFile.AddFile("ComponentInfoRegistry.txt", APIInternal.ComponentInfoRegistry.GetAsText());
            reportFile.AddFile("ShaderInformationRegistry.txt", API.ShaderInformationRegistry.GetAsText());
            reportFile.AddFile("NDMFPlugins.txt", CollectNdmfPlugins());
            reportFile.AddFile("NDMFSequence.txt", CollectNdmfSequence());
#if AAO_VRCSDK3_AVATARS
            reportFile.AddFile("VRCSDKBuildCallbacks.txt", CollectVRCSDKBuildCallbacks());
#endif

            // Collect pre-build avatar information
            var preBuildAvatarInfo = CollectAvatarInfo(clonedAvatar);
            reportFile.AddFile("AvatarInfo.PreBuild.tree.txt", preBuildAvatarInfo);

            var bugReporterLogHandler = new BugReporterLogHandler(Debug.unityLogger.logHandler);
            // NDMF Build. few information are collected during the build using Context.Current
            var preLogHandler = Debug.unityLogger.logHandler;
            var preLogEnabled = Debug.unityLogger.logEnabled;
            var preLogType = Debug.unityLogger.filterLogType;
            try
            {
                Debug.unityLogger.logHandler = bugReporterLogHandler;
                Debug.unityLogger.logEnabled = true;
                Debug.unityLogger.filterLogType = LogType.Log;
                Context.Current = new Context(reportFile);
                AvatarProcessor.ProcessAvatar(clonedAvatar);
            }
            finally
            {
                Debug.unityLogger.logHandler = preLogHandler;
                Debug.unityLogger.logEnabled = preLogEnabled;
                Debug.unityLogger.filterLogType = preLogType;
                Context.Current = null;
            }

            reportFile.AddFile("BuildLog.log.txt", bugReporterLogHandler.GetLog());

            // Collect post-build avatar information
            var postBuildAvatarInfo = CollectAvatarInfo(clonedAvatar);
            reportFile.AddFile("AvatarInfo.PostBuild.tree.txt", postBuildAvatarInfo);

            reportFile.AddFile("AnimatorParser.PostBuild.tree.txt", 
                AnimatorParserDebugWindow.CreateText(
                    new AnimatorParser(true)
                        .GatherAnimationModifications(new BuildContext(clonedAvatar, null)), 
                    clonedAvatar, detailed: true));
            
            reportFile.AddFile("RawAnimations.PostBuild.tree.txt", RawAnimationInfo(clonedAvatar));;

            return reportFile;
        }
        finally
        {
            Tracing.Enabled = tracingConfig;
            DestroyImmediate(clonedAvatar);
        }
    }

    private static string CollectNdmfPlugins()
    {
        // https://github.com/bdunderscore/ndmf/blob/d1bd628e38229c1d4000acd7526bf81cdd9d6294/Editor/API/Solver/PluginResolver.cs#L70
        const string sessionStateKey = "nadena.dev.ndmf.plugin-disabled.";
        // We collect NDMF Plugin information with reflections.
        // (NDMF doesn't expose API to get the list of loaded plugins)
        try
        {
            // required types
            var iPluginInternal = GetType("nadena.dev.ndmf.PluginResolver");
            var findAllPluginsMethod = iPluginInternal.GetMethod("FindAllPlugins", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                null, Type.EmptyTypes, null);
            if (findAllPluginsMethod == null) throw new Exception("FindAllPlugins method not found in PluginResolver");
            var plugins = (IEnumerable<object>)findAllPluginsMethod.Invoke(null, null);
            var pluginInstances = plugins.Cast<PluginBase>().ToList();

            var builder = new StringBuilder();
            foreach (var plugin in pluginInstances)
            {
                var disabled = SessionState.GetBool(sessionStateKey + plugin.QualifiedName, false);
                builder.AppendLine($"{plugin.QualifiedName} ({plugin.DisplayName}): {(disabled ? "Disabled" : "Enabled")}");
            }

            return builder.ToString();
        }
        catch (Exception e)
        {
            return "Error collecting NDMF plugin information: \n" + e;
        }

        Type GetType(string name) => Utils.GetTypeFromName(name) ?? throw new Exception($"Type '{name}' not found");
    }

    private static string CollectNdmfSequence()
    {
        // We collect NDMF build sequence information, something like shown on SolverWindow, with reflections.
        try
        {
            var pluginResolver = GetType("nadena.dev.ndmf.PluginResolver");
            var constructor = pluginResolver.GetConstructors().Select(ctor =>
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length == 0) return null;
                if (parameters.Any(x => !x.HasDefaultValue)) return null;

                var includeDisabledParamIndex = Array.FindIndex(parameters, p => p.Name == "includeDisabled" && p.ParameterType == typeof(bool));
                if (includeDisabledParamIndex < 0) return null;

                // we found a constructor with optional bool parameter named includeDisabled.
                // we can use it to create PluginResolver instance with includeDisabled = true

                return ((Func<bool, object>)(includeDisabled =>
                {
                    var args = new object[parameters.Length];
                    Array.Fill(args, Type.Missing);
                    args[includeDisabledParamIndex] = includeDisabled;
                    return ctor.Invoke(args);
                }));
            }).FirstOrDefault(x => x != null)?? throw new Exception("PluginResolver constructor with optional bool parameter named includeDisabled not found");
            // passes should be type interactable as IEnumerable<(BuildPhase, IEnumerable<ConcretePass>)>
            var passesProperty = GetPropertyOrField<IEnumerable>(pluginResolver, "Passes");
            var concratePassType = GetType("nadena.dev.ndmf.ConcretePass");
            var pluginOfConcratePass = GetPropertyOrField<PluginBase>(concratePassType, "Plugin");
            var deactivatePluginsOfConcratePass = GetPropertyOrField<IEnumerable<Type>>(concratePassType, "DeactivatePlugins");
            var activatePluginsOfConcratePass = GetPropertyOrField<IEnumerable<Type>>(concratePassType, "ActivatePlugins");
            var instantiatedPassOfConcratePass = GetPropertyOrField<object>(concratePassType, "InstantiatedPass");
            var iPassType = GetType("nadena.dev.ndmf.IPass");
            var qualifiedNameOfIPass = GetPropertyOrField<string>(iPassType, "QualifiedName");
            var displayNameOfIPass = GetPropertyOrField<string>(iPassType, "DisplayName");

            // generic type instances
            var enumerableOfConcretePass = typeof(IEnumerable<>).MakeGenericType(concratePassType);

            var resolverInstance = constructor(true);

            var passes = passesProperty(resolverInstance);
            var builder = new StringBuilder();
            foreach (ITuple passObj in passes)
            {
                var buildPhase = (BuildPhase)passObj[0];
                var phasePasses = (IEnumerable)passObj[1];
                if (!enumerableOfConcretePass.IsInstanceOfType(phasePasses))
                    throw new Exception($"Passes[*].Item2 is not of type IEnumerable<ConcretePass>");

                builder.AppendLine($"BuildPhase: {buildPhase}");
                PluginBase? priorPlugin = null;
                foreach (var pass in phasePasses)
                {
                    var plugin = pluginOfConcratePass(pass);
                    var deactivatePlugins = deactivatePluginsOfConcratePass(pass);
                    var activatePlugins = activatePluginsOfConcratePass(pass);
                    var instantiatedPass = instantiatedPassOfConcratePass(pass);
                    var qualifiedName = qualifiedNameOfIPass(instantiatedPass);
                    var displayName = displayNameOfIPass(instantiatedPass);

                    foreach (var deactivatePlugin in deactivatePlugins)
                        builder.AppendLine($"    Deactivates: {deactivatePlugin}");

                    if (priorPlugin != plugin)
                    {
                        builder.AppendLine($"  Plugin: {plugin.QualifiedName} ({plugin.DisplayName})");
                        priorPlugin = plugin;
                    }

                    foreach (var activatePlugin in activatePlugins)
                        builder.AppendLine($"    Activate: {activatePlugin}");

                    builder.AppendLine($"      Pass: {qualifiedName} ({displayName})");
                }
            }

            return builder.ToString();
        }
        catch (Exception e)
        {
            return "Error collecting NDMF sequence information: \n" + e;
        }

        Type GetType(string name) => Utils.GetTypeFromName(name) ?? throw new Exception($"Type '{name}' not found");
        Func<object, T> GetPropertyOrField<T>(Type type, string name)
        {
            var field = type.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                return obj => (T)field.GetValue(obj);
            var prop = type.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prop != null)
                return obj => (T)prop.GetValue(obj);
            throw new Exception($"Field or property '{name}' not found in type '{type.FullName}'");
        }
    }

#if AAO_VRCSDK3_AVATARS
    private static string CollectVRCSDKBuildCallbacks()
    {
        try
        {
            var type = typeof(VRC.SDKBase.Editor.BuildPipeline.VRCBuildPipelineCallbacks);
            var field = type.GetField("_preprocessAvatarCallbacks", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (field == null) throw new Exception("Field '_preprocessAvatarCallbacks' not found in VRCBuildPipelineCallbacks");
            var callbacks = ((IEnumerable<VRC.SDKBase.Editor.BuildPipeline.IVRCSDKPreprocessAvatarCallback>)field.GetValue(null)).ToList();
            callbacks.Sort((a, b) => a.callbackOrder.CompareTo(b.callbackOrder));

            var builder = new StringBuilder();
            foreach (var callback in callbacks)
            {
                builder.AppendLine($"{callback.GetType().FullName}: callbackOrder: {callback.callbackOrder}; {callback}");
            }

            return builder.ToString();
        }
        catch (Exception e)
        {
            return "Error collecting VRCSDK build callback information: \n" + e;
        }
    }
#endif

    public static string CollectAvatarInfo(GameObject clonedAvatar)
    {
        // Avatr Info file consists is something like:
        // Path/Of/GameObject:
        //   ComponentType1
        //   ComponentType2
        //   SkinnedMeshRenderer
        //     Additional Component Info if needed

        var builder = new StringBuilder();

        foreach (var transform in clonedAvatar.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            var path = Utils.RelativePath(clonedAvatar.transform, transform);
            if (path == "") path = "<Root>";
            builder.AppendLine(path);
            foreach (var component in transform.GetComponents<Component>())
            {
                if (component == null)
                {
                    builder.AppendLine("  <Missing Component>");
                    continue;
                }

                var type = component.GetType();
                builder.AppendLine($"  {type.FullName}");

                switch (component)
                {
                    case Transform t:
                        builder.AppendLine($"    activeSelf: {t.gameObject.activeSelf}");
                        break;
                    case Behaviour b:
                        builder.AppendLine($"    enabled: {b.enabled}");
                        break;
                    case Cloth cloth:
                        builder.AppendLine($"    enabled: {cloth.enabled}");
                        break;
                    case Collider collider:
                        builder.AppendLine($"    enabled: {collider.enabled}");
                        break;
                    case LODGroup lodGroup:
                        builder.AppendLine($"    enabled: {lodGroup.enabled}");
                        break;
                    case Renderer r:
                        builder.AppendLine($"    enabled: {r.enabled}");
                        break;
                }

                // Additional info for few components
                switch (component)
                {
                    // unity components
                    case MeshFilter meshFilter:
                        MeshInfo(meshFilter.sharedMesh);
                        break;
                    case SkinnedMeshRenderer skinnedMeshRenderer:
                        MeshInfo(skinnedMeshRenderer.sharedMesh);
                        for (var i = 0; i < skinnedMeshRenderer.bones.Length; i++)
                        {
                            var bone = skinnedMeshRenderer.bones[i];
                            builder.AppendLine($"    bone[{i}]: {ComponentPath(bone)}");
                        }
                        // blendshape weights
                        for (var i = 0; i < skinnedMeshRenderer.sharedMesh?.blendShapeCount; i++)
                        {
                            var weight = skinnedMeshRenderer.GetBlendShapeWeight(i);
                            var blendShapeName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);
                            builder.AppendLine($"    blendShapeWeight[{i}]: {blendShapeName} = {weight}");
                        }
                        // root bone
                        builder.AppendLine($"    rootBone: {ComponentPath(skinnedMeshRenderer.rootBone)}");
                        // anchor related
                        builder.AppendLine($"    probeAnchor: {ComponentPath(skinnedMeshRenderer.probeAnchor)}");
                        Renderer(skinnedMeshRenderer);
                        break;
                    case MeshRenderer meshRenderer:
                        Renderer(meshRenderer);
                        break;

#if AAO_VRCSDK3_AVATARS
                    case VRCPhysBoneBase physBone:
                        builder.AppendLine($"    version: {physBone.version}");
                        builder.AppendLine($"    integrationType: {physBone.integrationType}");
                        builder.AppendLine($"    rootTransform: {ComponentPath(physBone.rootTransform)}");
                        for (var i = 0; i < physBone.ignoreTransforms.Count; i++)
                        {
                            var t = physBone.ignoreTransforms[i];
                            builder.AppendLine($"    ignoreTransform[{i}]: {ComponentPath(t)}");
                        }
                        builder.AppendLine($"    endpointPosition: {physBone.endpointPosition}");
                        builder.AppendLine($"    multiChildType: {physBone.multiChildType}");
                        builder.AppendLine($"    pull: {physBone.pull:G9}, curve: {physBone.pullCurve?.keys?.Length ?? 0}");
                        builder.AppendLine($"    spring: {physBone.spring:G9}, curve: {physBone.springCurve?.keys?.Length ?? 0}");
                        builder.AppendLine($"    stiffness: {physBone.stiffness:G9}, curve: {physBone.stiffnessCurve?.keys?.Length ?? 0}");
                        builder.AppendLine($"    gravity: {physBone.gravity:G9}, curve: {physBone.gravityCurve?.keys?.Length ?? 0}");
                        builder.AppendLine($"    immobileType: {physBone.immobileType}");
                        builder.AppendLine($"    immobile: {physBone.immobile:G9}, curve: {physBone.immobileCurve?.keys?.Length ?? 0}");
                        builder.AppendLine($"    allowCollision: {physBone.allowCollision}");
                        builder.AppendLine($"    collisionFilter.allowSelf: {physBone.collisionFilter.allowSelf}");
                        builder.AppendLine($"    collisionFilter.allowOthers: {physBone.collisionFilter.allowOthers}");
                        builder.AppendLine($"    radius: {physBone.radius}, curve: {physBone.radiusCurve?.keys?.Length ?? 0}");
                        for (var i = 0; i < physBone.colliders.Count; i++)
                        {
                            var collider = physBone.colliders[i];
                            builder.AppendLine($"    collider[{i}]: {ComponentPath(collider)}");
                        }
                        builder.AppendLine($"    limitType: {physBone.limitType}");
                        builder.AppendLine($"    maxAngleX: {physBone.maxAngleX:G9}, curve: {physBone.maxAngleXCurve?.keys?.Length ?? 0}");
                        builder.AppendLine($"    maxAngleZ: {physBone.maxAngleZ:G9}, curve: {physBone.maxAngleZCurve?.keys?.Length ?? 0}");
                        builder.AppendLine($"    limitRotation.x: {physBone.limitRotation.x:G9}, curve: {physBone.limitRotationXCurve?.keys?.Length ?? 0}");
                        builder.AppendLine($"    limitRotation.y: {physBone.limitRotation.y:G9}, curve: {physBone.limitRotationYCurve?.keys?.Length ?? 0}");
                        builder.AppendLine($"    limitRotation.z: {physBone.limitRotation.z:G9}, curve: {physBone.limitRotationZCurve?.keys?.Length ?? 0}");
                        builder.AppendLine($"    allowGrabbing: {physBone.allowGrabbing}");
                        builder.AppendLine($"    grabFilter.allowSelf: {physBone.grabFilter.allowSelf}");
                        builder.AppendLine($"    grabFilter.allowOthers: {physBone.grabFilter.allowOthers}");
                        builder.AppendLine($"    allowPosing: {physBone.allowPosing}");
                        builder.AppendLine($"    poseFilter.allowSelf: {physBone.poseFilter.allowSelf}");
                        builder.AppendLine($"    poseFilter.allowOthers: {physBone.poseFilter.allowOthers}");
                        builder.AppendLine($"    snapToHand: {physBone.snapToHand}");
                        builder.AppendLine($"    grabMovement: {physBone.grabMovement:G9}");
                        builder.AppendLine($"    maxStretch: {physBone.maxStretch:G9}, curve: {physBone.maxStretchCurve?.keys?.Length ?? 0}");
                        builder.AppendLine($"    maxSquish: {physBone.maxSquish:G9}, curve: {physBone.maxSquishCurve?.keys?.Length ?? 0}");
                        builder.AppendLine($"    stretchMotion: {physBone.stretchMotion:G9}, curve: {physBone.stretchMotionCurve?.keys?.Length ?? 0}");
                        builder.AppendLine($"    isAnimated: {physBone.isAnimated}");
                        builder.AppendLine($"    resetWhenDisabled: {physBone.resetWhenDisabled}");
                        builder.AppendLine($"    parameter: '{physBone.parameter}'");
                        break;
#endif

                    // Core AAO components
                    case TraceAndOptimize traceAndOptimize:
                        builder.AppendLine($"    SettingsJSON: {JsonUtility.ToJson(traceAndOptimize)}");
                        break;
                }

                string ComponentPath(Component? c)
                {
                    if (c == null) return "<None or Missing>";
                    if (c.transform.IsChildOf(clonedAvatar.transform))
                        return "avatar:" + Utils.RelativePath(clonedAvatar.transform, c.transform);
                    if (c.gameObject.scene.IsValid())
                        return "scene:" + Utils.RelativePath(null, c.transform);
                    return "non-scene:" + Utils.RelativePath(null, c.transform);
                }

                void MeshInfo(Mesh mesh)
                {
                    if (mesh == null)
                    {
                        builder.AppendLine("  Mesh: <Missing or None>");
                        return;
                    }

                    builder.AppendLine($"    Mesh: {mesh.name}");
                    builder.AppendLine($"      vertexBufferCount: {mesh.vertexBufferCount}");
                    builder.AppendLine($"      vertexCount: {mesh.vertexCount}");
                    builder.AppendLine($"      bindposes: {mesh.bindposes?.Length ?? 0}");
                    builder.AppendLine($"      triangles: {mesh.triangles?.Length ?? 0}");
                    // attributes
                    var attributes = mesh.GetVertexAttributes();
                    builder.AppendLine($"      attributes: {attributes.Length}");
                    for (var i = 0; i < attributes.Length; i++)
                    {
                        var attribute = attributes[i];
                        builder.AppendLine($"      attributes[{i}]: {attribute}");
                    }
                    // blendshapes
                    var blendShapeCount = mesh.blendShapeCount;
                    builder.AppendLine($"      blendShapeCount: {blendShapeCount}");
                    for (var i = 0; i < blendShapeCount; i++)
                    {
                        var blendShapeName = mesh.GetBlendShapeName(i);
                        var frameCount = mesh.GetBlendShapeFrameCount(i);
                        builder.AppendLine($"      blendShape[{i}]: {blendShapeName} (frames: {frameCount})");
                    }
                    // submeshes
                    var subMeshCount = mesh.subMeshCount;
                    builder.AppendLine($"      subMeshCount: {subMeshCount}");
                    for (var i = 0; i < subMeshCount; i++)
                    {
                        var subMesh = mesh.GetSubMesh(i);
                        builder.AppendLine($"      subMesh[{i}]: topology={subMesh.topology}, indexCount={subMesh.indexCount}, baseVertex={subMesh.baseVertex}, firstVertex={subMesh.firstVertex}");
                    }
                    // bone weights
                    builder.AppendLine($"      boneWeights: total={mesh.GetAllBoneWeights().Length}, bonesPerVertex={mesh.GetBonesPerVertex().Length}");
                }

                void Renderer(Renderer renderer)
                {
                    builder.AppendLine($"    enabled: {renderer.enabled}");
                    builder.AppendLine($"    shadowCastingMode: {renderer.shadowCastingMode}");
                    builder.AppendLine($"    receiveShadows: {renderer.receiveShadows}");
                    builder.AppendLine($"    lightProbeUsage: {renderer.lightProbeUsage}");
                    builder.AppendLine($"    reflectionProbeUsage: {renderer.reflectionProbeUsage}");
                    builder.AppendLine($"    sortingLayerID: {renderer.sortingLayerID}");
                    builder.AppendLine($"    sortingOrder: {renderer.sortingOrder}");
                    for (var i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        var sharedMaterial = renderer.sharedMaterials[i];
                        if (sharedMaterial != null)
                        {
                            builder.AppendLine($"    sharedMaterials[{i}]: {sharedMaterial.name} ({sharedMaterial.shader.name}) ({sharedMaterial.GetInstanceID()})");
                            MaterialInfo(sharedMaterial, "      ");
                        }
                        else
                        {
                            builder.AppendLine($"    sharedMaterials[{i}]: <Missing / None>");
                        }
                    }
                }

                void MaterialInfo(Material material, string indent)
                {
                    var shader = material.shader;
                    var propertyCount = shader.GetPropertyCount();
                    for (var i = 0; i < propertyCount; i++)
                    {
                        var propertyName = shader.GetPropertyName(i);
                        var propertyType = shader.GetPropertyType(i);
                        switch (propertyType)
                        {
                            case UnityEngine.Rendering.ShaderPropertyType.Color:
                                var color = material.GetColor(propertyName);
                                builder.AppendLine($"{indent}property[{i}]: {propertyName} (Color) = {Hashed($"{color}")}");
                                break;
                            case UnityEngine.Rendering.ShaderPropertyType.Vector:
                                var vector = material.GetVector(propertyName);
                                builder.AppendLine($"{indent}property[{i}]: {propertyName} (Vector) = {Hashed($"{vector}")}");
                                break;
                            case UnityEngine.Rendering.ShaderPropertyType.Float:
                            case UnityEngine.Rendering.ShaderPropertyType.Range:
                                var floatValue = material.GetFloat(propertyName);
                                builder.AppendLine( $"{indent}property[{i}]: {propertyName} ({propertyType}) = {Hashed($"{floatValue}")}");
                                break;
                            case UnityEngine.Rendering.ShaderPropertyType.Int:
                                var intValue = material.GetInt(propertyName);
                                builder.AppendLine( $"{indent}property[{i}]: {propertyName} (Int) = {Hashed($"{intValue}")}");
                                break;
                            case UnityEngine.Rendering.ShaderPropertyType.Texture:
                                var texture = material.GetTexture(propertyName);
                                builder.AppendLine(
                                    $"{indent}property[{i}]: {propertyName} (Texture) = {(texture != null ? texture.name : "<Missing>")}");
                                break;
                        }
                    }
                }

                // simple hash function to avoid leaking exact values
                string Hashed(string input)
                {
                    var hash = input.GetHashCode();
                    return $"<hash:{hash:X8}>";
                }
            }
        }

        return builder.ToString();
    }

    class BugReporterLogHandler : ILogHandler
    {
        ILogHandler? _upstream;
        StringBuilder _builder = new StringBuilder();
        private readonly StackTrace _stacktrace;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public BugReporterLogHandler(ILogHandler? upstream, int skipFrames = 0)
        {
            _upstream = upstream;
            _stacktrace = new StackTrace(skipFrames: skipFrames + 1);
        }

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            _upstream?.LogFormat(logType, context, format, args);
            // log with timestamp, logtype, message, and stacktrace
            _builder.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] [{logType}] {string.Format(format, args)}");
            _builder.AppendLine(GetStackTrace());
            _builder.AppendLine();
        }

        public void LogException(Exception exception, Object context)
        {
            _upstream?.LogException(exception, context);

            _builder.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] [Exception] {exception}");
            _builder.AppendLine(exception.StackTrace);
            _builder.AppendLine();
        }

        private string GetStackTrace()
        {
            var current = new StackTrace();
            // we generally expect this is called from UnityEngine.Debug class so top few frames are not useful.
            // we only keep last top 'UnityEngine.Debug' frame and frames after that.
            // also, we generally expect this is called from the location BugReporterHelper is created, and 
            // several bottom frames are not useful too. we compare with _stacktrace and remove frames from the bottom until we find a different frame.

            var currentFrames = (current.GetFrames() ?? Array.Empty<StackFrame>()).AsSpan();
            var baseFrames = (_stacktrace.GetFrames() ?? Array.Empty<StackFrame>()).AsSpan();

            // trim bottom
            while ((currentFrames.Length != 0 && baseFrames.Length != 0)
                   && currentFrames[^1].ToString() == baseFrames[^1].ToString())
            {
                currentFrames = currentFrames[..^1];
                baseFrames = baseFrames[..^1];
            }

            // trim head (UnityEngine.Debug frames)
            for (var firstDebugFrameIndex = 0; firstDebugFrameIndex < currentFrames.Length; firstDebugFrameIndex++)
            {
                var currentFrame = currentFrames[firstDebugFrameIndex];
                var frameMethodType = currentFrame.GetMethod()?.DeclaringType;
                if (frameMethodType == null) break;
                if (frameMethodType.FullName == typeof(Debug).FullName)
                {
                    // we found first Debug frame, we find next frame which is not Debug frame and trim head until that frame
                    for (var firstNonDebugFrameIndex = firstDebugFrameIndex + 1; firstNonDebugFrameIndex < currentFrames.Length; firstNonDebugFrameIndex++)
                    {
                        var nonDebugFrame = currentFrames[firstNonDebugFrameIndex];
                        var nonDebugFrameMethodType = nonDebugFrame.GetMethod()?.DeclaringType;
                        if (nonDebugFrameMethodType == null) break; // we don't expect to have non-native or non-class frame between this and debug frame.
                        if (nonDebugFrameMethodType.FullName != typeof(Debug).FullName)
                        {
                            // we want to keep single Debug frame, so we trim until firstNonDebugFrameIndex - 1
                            currentFrames = currentFrames[(firstNonDebugFrameIndex - 1)..];
                            break;
                        }
                    }
                    break;
                }
            }

            // convert to string
            // use internal constructor with StackFrame[] to create StackTrace, and call ToString() to get formatted stack trace string.
            // TODO(unity6000): When unity moved to coreclr, move to IEnumerable<StackFrame> public constructor instead of using internal constructor.
            var trimmedStackTrace = (StackTrace)System.Activator.CreateInstance(typeof(StackTrace), 
                bindingAttr: System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, 
                binder: null, 
                args: new object[] { currentFrames.ToArray() }, 
                culture: null)!;
            return trimmedStackTrace.ToString();
        }

        public string GetLog()
        {
            return _builder.ToString();
        }
    }

    // Originally copied from console log saver
    // https://github.com/anatawa12/ConsoleLogSaver/blob/v1.0.2/com.anatawa12.console-log-saver/PackageManagerInfoCollector.cs
    static class PackageManagerInfoCollector
    {
        // returns list of locked dependencies listed on vpm-manifest.json
        public static IEnumerable<(string package, string version)> VpmLockedPackages()
        {
            Dictionary<string, VpmManifest.Dependency> locked;
            try
            {
                var vpmManifest = JsonConvert.DeserializeObject<VpmManifest>(File.ReadAllText("Packages/vpm-manifest.json"));
                if (vpmManifest == null) yield break;
                locked = vpmManifest.locked;
            }
            catch
            {
                yield break;
            }

            foreach (var (package, lockedInfo) in locked)
            {
                var version = lockedInfo?.version;
                if (version == null) continue;
                yield return (package, version);
            }
        }

        class VpmManifest
        {
            [JsonProperty]
            public Dictionary<string, Dependency> locked = new Dictionary<string, Dependency>();
            public class Dependency
            {
                [JsonProperty]
                public string? version;
            }
        }

        // returns list of locked dependencies listed on packages-lock.json
        public static IEnumerable<(string package, string version)> UpmLockedPackages()
        {
            UpmManifest? manifest;
            try
            {
                manifest = JsonConvert.DeserializeObject<UpmManifest>(File.ReadAllText("Packages/packages-lock.json"));
                if (manifest == null) yield break;
            }
            catch
            {
                yield break;
            }

            foreach (var (package, locked) in manifest.dependencies)
            {
                var version = locked?.version;
                if (version == null) continue;
                yield return (package, version);
            }
        }

        class UpmManifest
        {
            [JsonProperty]
            public Dictionary<string, Dependency> dependencies = new Dictionary<string, Dependency>();
            public class Dependency
            {
                [JsonProperty]
                public string? version;
            }
        }
    }

    public static string RawAnimationInfo(GameObject contextAvatarRootObject)
    {
        var builder = new StringBuilder();

        foreach (var animator in contextAvatarRootObject.GetComponentsInChildren<Animator>(includeInactive: true))
        {
            RawAnimationInfo(builder, contextAvatarRootObject, animator.gameObject, animator, animator.runtimeAnimatorController);
        }

#if AAO_VRCSDK3_AVATARS
        foreach (var descriptor in contextAvatarRootObject.GetComponentsInChildren<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(includeInactive: true))
        {
            foreach (var customAnimLayer in descriptor.baseAnimationLayers.Concat(descriptor.specialAnimationLayers))
            {
                RawAnimationInfo(builder, contextAvatarRootObject, descriptor.gameObject, descriptor, customAnimLayer.animatorController);
            }
        }
#endif

        return builder.ToString();
    }

    private static void RawAnimationInfo(
        StringBuilder builder,
        GameObject avatarRoot,
        GameObject rootObject,
        Component component,
        RuntimeAnimatorController? animatorController
    )
    {
        if (animatorController == null) return;
        var (controller, animationClipMapping) = ACUtils.GetControllerAndOverrides(animatorController);
        if (controller == null) return;
        RawAnimationInfo(builder, avatarRoot, rootObject, component, controller, animationClipMapping);
    }

    // saves animation info without resolving references for debugging purpose.
    private static void RawAnimationInfo(
        StringBuilder builder,
        GameObject avatarRoot, 
        GameObject rootObject, 
        Component component, 
        AnimatorController animatorController,
        IReadOnlyDictionary<AnimationClip, AnimationClip> animationClipMapping
    ) {
        builder.AppendLine($"{Utils.RelativePath(avatarRoot.transform, rootObject.transform)}: {component.GetType().FullName} with AnimatorController {animatorController.name}");

        var layers = animatorController.layers;

        foreach (var grouping in layers.SelectMany(layer => layer.syncedLayerIndex < 0
                         ? ACUtils.AllStates(layer.stateMachine)
                             .Select(state => (state, state.motion))
                         : ACUtils.AllStates(layers[layer.syncedLayerIndex].stateMachine)
                             .Select(state => (state, motion: layer.GetOverrideMotion(state))))
                     .SelectMany(p => ACUtils.AllClips(p.motion)
                         .Select(clip => (p.state, clip: animationClipMapping.GetValueOrDefault(clip, clip))))
                     .GroupBy(x => x.clip))
        {
            var clip = grouping.Key;

            builder.Append($"  {clip.name}: ({clip.GetInstanceID()}) (Used in:");
            foreach (var state in grouping.Select(x => x.state))
                builder.Append($" {state.name}");
            builder.AppendLine(")");

            // contents of the clip
            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                builder.AppendLine($"    {binding.path}.({binding.type.FullName}).{binding.propertyName}: float: {curve.GetHashCode2():x8}");
            }
            var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var binding in objectBindings)
            {
                var curve = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                builder.AppendLine($"    {binding.path}.({binding.type.FullName}).{binding.propertyName}: object reference: {curve.Length}");
            }
        }
    }

    public static string MaterialInformation(Transform avatarRoot, IEnumerable<MaterialInformation> materialInformations)
    {
        var builder = new StringBuilder();
        foreach (var information in materialInformations)
        {
            builder.AppendLine($"{information.Material.name} ({information.Material.shader.name}):");
            builder.AppendLine($"  UserRenderers:");
            foreach (var renderer in information.UserRenderers)
                if (renderer)
                    builder.AppendLine($"    {Utils.RelativePath(avatarRoot, renderer.transform)}({renderer.GetType()})");
                else
                    builder.AppendLine($"    destroyed renderer");
            AddShaderInformationResult(builder, "DefaultResult", information.DefaultResult);
            AddShaderInformationResult(builder, "FallbackResult", information.FallbackResult);
        }
        return builder.ToString();

        void AddShaderInformationResult(StringBuilder builder, string label, MaterialInformation.ShaderInformationResult? result)
        {
            if (result != null)
            {
                builder.AppendLine($"  {label}:");
                builder.AppendLine($"    OtherUVUsage: {result.OtherUVUsage}");
                builder.AppendLine($"    UseVertexIndex: {result.UseVertexIndex}");
                if (result.TextureUsageInformationList == null)
                {
                    builder.AppendLine($"    TextureUsageInformationList: null");
                }
                else
                {
                    builder.AppendLine($"    TextureUsageInformationList");
                    foreach (var usageInformation in result.TextureUsageInformationList)
                    {
                        builder.AppendLine($"      {usageInformation.MaterialPropertyName}:");
                        builder.AppendLine($"        UVChannel: {usageInformation.UVChannel}");
                        builder.AppendLine($"        WrapModeU: {usageInformation.WrapModeU}");
                        builder.AppendLine($"        WrapModeV: {usageInformation.WrapModeV}");
                        if (usageInformation.UVMatrix is { } matrix)
                        {
                            builder.AppendLine($"        UVMatrix: ");
                            builder.AppendLine($"          {matrix.M00,-10} {matrix.M01,-10} {matrix.M02,-10}");
                            builder.AppendLine($"          {matrix.M10,-10} {matrix.M11,-10} {matrix.M12,-10}");
                        }
                        else
                        {
                            builder.AppendLine($"        UVMatrix: null");
                        }
                    }
                }
            }
            else
            {
                builder.AppendLine($"  {label}: null");
            }
        }
    }
}

/// <summary>
/// This class holds additional context information during the build process.
/// This is to collect more data for bug reports than normal functionality.
/// This is expected to be accessed as BugReportHelper.Context.Current during the build process.
/// </summary>
internal class Context
{
    public static Context? Current = null;
    public readonly ReportFile ReportFile;

    public Context(ReportFile reportFile)
    {
        ReportFile = reportFile;
    }

    public void AnimatorParserResult(BuildContext context, RootPropModNodeContainer modifications)
    {
        ReportFile.AddFile("AnimatorParser.tree.txt", AnimatorParserDebugWindow.CreateText(modifications, context.AvatarRootObject, detailed: true));
        ReportFile.AddFile("RawAnimations.tree.txt", BugReportHelper.RawAnimationInfo(context.AvatarRootObject));
    }

    public void AddGcDebugInfo(InternalGcDebugPosition position, string collectDataToString, GameObject root, IEnumerable<MaterialInformation> materials)
    {
        ReportFile.AddFile($"GCDebug.{position}.tree.txt", collectDataToString);
        ReportFile.AddFile($"AvatarInfo.{position}.tree.txt", BugReportHelper.CollectAvatarInfo(root));
        ReportFile.AddFile($"MaterialInformation.{position}.tree.txt", 
            BugReportHelper.MaterialInformation(root.transform, materials));
    }
}

// A report file consists of:
// - multiple set of key-value pairs
// - multiple text blobs, with their own key-value pairs as metadata
// The format of the report file is like multipart/form-data in HTTP, with several differences:
// - The file begins with a fixed header line: "AAO-BugReport-File/1.0"
// - The file will have 'header' fields before the first part. two newlines separate the header and the first part, similar to HTTP/1.1
// - The boundary will be stored in the header as "Boundary: <boundary-string>", since there is no external place to store it.
// - Each field's key-value pairs cannot be concatenated into single line like Set-Cookie in HTTP, as opposed to most HTTP headers that can be concatenated with commas.
//   You must store header fields as `List<KeyValuePair<string, string>>`, rather than `Dictionary<string, string>`.
internal class ReportFile
{
    private readonly string _boundary;

    // fields
    public readonly List<KeyValuePair<string, string>> Fields = new();
    // blobs
    public readonly List<ReportBlob> Blobs = new();

    public ReportFile()
    {
        _boundary = Guid.NewGuid().ToString("N");
        AddField("boundary", _boundary, @internal: true);
    }

    public void AddField(string key, string value) => AddField(key, value, @internal: false);

    private void AddField(string key, string value, bool @internal)
    {
        if (!@internal)
            if (key.Equals("boundary", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Field key 'boundary' is reserved", nameof(key));
        Fields.Add(new KeyValuePair<string, string>(key, value));
    }

    // Returns the first dot-separated segment of a filename, e.g. "AvatarInfo" from "AvatarInfo.PreBuild.tree.txt".
    private static string? GetFilePrefix(string fileName)
    {
        var dot = fileName.IndexOf('.');
        return dot > 0 ? fileName.Substring(0, dot) : null;
    }

    // Extracts the filename from a blob's Content-Disposition header, or null if absent.
    private static string? GetBlobFileName(ReportBlob blob)
    {
        foreach (var header in blob.Headers)
        {
            if (!header.Key.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase)) continue;
            var value = header.Value;
            var idx = value.IndexOf("filename=\"", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            idx += "filename=\"".Length;
            var end = value.IndexOf('"', idx);
            if (end < 0) continue;
            return value.Substring(idx, end - idx);
        }
        return null;
    }

    /// <summary>
    /// Adds a file to the report.
    /// </summary>
    public void AddFile(string fileName, string content)
    {
        var headers = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Content-Disposition", $"attachment; filename=\"{fileName}\""),
        };
        Blobs.Add(new ReportBlob { Headers = headers, Content = content });
    }

    /// <summary>
    /// Post-processes all blobs added via <see cref="AddFile"/> and replaces adjacent
    /// files that share a filename prefix with ed-script diffs when doing so produces
    /// a smaller blob.  Call this once, after all files have been added and before
    /// calling <see cref="ToString"/>.
    /// </summary>
    public void DiffCompress()
    {
        var lastFileByPrefix = new Dictionary<string, (string fileName, string resolvedContent)>();
        for (int i = 0; i < Blobs.Count; i++)
        {
            var blob = Blobs[i];
            var fileName = GetBlobFileName(blob);
            if (fileName == null) continue;

            var originalContent = blob.Content;
            var prefix = GetFilePrefix(fileName);
            if (prefix != null && lastFileByPrefix.TryGetValue(prefix, out var baseInfo))
            {
                var diff = CreateLineDiff(baseInfo.resolvedContent, originalContent);
                if (diff.Length < originalContent.Length)
                {
                    blob.Headers.Add(new KeyValuePair<string, string>("Content-Encoding", $"ed-script base=\"{baseInfo.fileName}\""));
                    blob.Content = diff;
                    Blobs[i] = blob;
                }
            }

            if (prefix != null)
                lastFileByPrefix[prefix] = (fileName, originalContent);
        }
    }

    /// <summary>
    /// Produces an ed-script diff from <paramref name="baseContent"/> to
    /// <paramref name="newContent"/> that can be applied with <see cref="ApplyLineDiff"/>.
    /// <para>
    /// The output follows the standard <c>diff -e</c> ed-script format:
    /// commands are emitted in <em>reverse</em> file order so they can be applied
    /// sequentially without adjusting line numbers.
    /// <list type="bullet">
    ///   <item><c>addr1[,addr2]d</c> – delete the specified 1-based line range.</item>
    ///   <item><c>addr1[,addr2]c</c> – replace the specified lines with the following
    ///     text block, terminated by a line containing only <c>.</c>.</item>
    ///   <item><c>addra</c> – append (insert after line <c>addr</c>, where 0 means
    ///     before the first line) the following text block, terminated by <c>.</c>.</item>
    /// </list>
    /// </para>
    /// </summary>
    internal static string CreateLineDiff(string baseContent, string newContent)
    {
        var baseLines = SplitLines(baseContent);
        var newLines = SplitLines(newContent);
        int n = baseLines.Length, m = newLines.Length;

        // Compute matching runs with absolute positions:
        // 1. Strip a common prefix and suffix for efficiency.
        int pfx = 0;
        while (pfx < n && pfx < m && baseLines[pfx] == newLines[pfx])
            pfx++;

        int sfx = 0;
        while (sfx < n - pfx && sfx < m - pfx &&
               baseLines[n - 1 - sfx] == newLines[m - 1 - sfx])
            sfx++;

        // 2. Run greedy matching on the middle section, then prepend/append the
        //    fixed-point runs for prefix and suffix.
        var runs = new List<(int baseI, int newI, int count)>();
        if (pfx > 0)
            runs.Add((0, 0, pfx));

        int midN = n - pfx - sfx;
        int midM = m - pfx - sfx;
        if (midN > 0 || midM > 0)
        {
            foreach (var (bi, ni, cnt) in ComputeGreedyRuns(baseLines, pfx, midN, newLines, pfx, midM))
                runs.Add((pfx + bi, pfx + ni, cnt));
        }

        if (sfx > 0)
            runs.Add((n - sfx, m - sfx, sfx));

        // 3. Derive hunks (differing regions between runs).
        var hunks = new List<(int bStart, int bEnd, int nStart, int nEnd)>();
        int prevB = 0, prevN = 0;
        foreach (var (bI, nI, cnt) in runs)
        {
            if (bI > prevB || nI > prevN)
                hunks.Add((prevB, bI, prevN, nI));
            prevB = bI + cnt;
            prevN = nI + cnt;
        }
        if (prevB < n || prevN < m)
            hunks.Add((prevB, n, prevN, m));

        // 4. Emit ed script in reverse file order.
        var sb = new StringBuilder();
        for (int i = hunks.Count - 1; i >= 0; i--)
        {
            var (bStart, bEnd, nStart, nEnd) = hunks[i];
            bool hasDel = bEnd > bStart;
            bool hasIns = nEnd > nStart;

            if (hasDel && hasIns)
            {
                AppendEdAddress(sb, bStart + 1, bEnd);
                sb.Append("c\n");
                for (int j = nStart; j < nEnd; j++)
                    sb.Append(newLines[j]).Append('\n');
                sb.Append(".\n");
            }
            else if (hasDel)
            {
                AppendEdAddress(sb, bStart + 1, bEnd);
                sb.Append("d\n");
            }
            else // insert only
            {
                // 'a' appends after line bStart (0 = before first line).
                sb.Append(bStart).Append("a\n");
                for (int j = nStart; j < nEnd; j++)
                    sb.Append(newLines[j]).Append('\n');
                sb.Append(".\n");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Reconstructs the original content from a base file and an ed-script diff
    /// produced by <see cref="CreateLineDiff"/>.
    /// Commands are applied in the order given (reverse file order), so each
    /// operation targets a region that is unaffected by previous operations.
    /// </summary>
    internal static string ApplyLineDiff(string baseContent, string diffContent)
    {
        var lines = new List<string>(SplitLines(baseContent));
        var diffLines = SplitLines(diffContent);
        int i = 0;

        while (i < diffLines.Length)
        {
            var cmdLine = diffLines[i];
            if (string.IsNullOrEmpty(cmdLine)) { i++; continue; }

            char cmd = cmdLine[cmdLine.Length - 1];
            if (cmd != 'a' && cmd != 'd' && cmd != 'c') { i++; continue; }

            var addrPart = cmdLine.Substring(0, cmdLine.Length - 1);
            int addr1, addr2;
            int comma = addrPart.IndexOf(',');
            if (comma >= 0)
            {
                addr1 = int.Parse(addrPart.Substring(0, comma));
                addr2 = int.Parse(addrPart.Substring(comma + 1));
            }
            else
            {
                addr1 = int.Parse(addrPart);
                addr2 = addr1;
            }
            i++;

            switch (cmd)
            {
                case 'd':
                    lines.RemoveRange(addr1 - 1, addr2 - addr1 + 1);
                    break;
                case 'a':
                case 'c':
                    var insertLines = new List<string>();
                    while (i < diffLines.Length && diffLines[i] != ".")
                        insertLines.Add(diffLines[i++]);
                    if (i < diffLines.Length) i++; // skip '.'

                    if (cmd == 'c')
                    {
                        lines.RemoveRange(addr1 - 1, addr2 - addr1 + 1);
                        lines.InsertRange(addr1 - 1, insertLines);
                    }
                    else // 'a': insert after addr1 (addr1=0 means before first line)
                    {
                        lines.InsertRange(addr1, insertLines);
                    }
                    break;
            }
        }

        return string.Join("\n", lines);
    }

    // Greedy forward-scan LCS approximation: builds a hash index of base lines and
    // matches each new line to the earliest available base position strictly after
    // the last match.  Consecutive matched pairs are merged into copy runs.
    // Returns runs with positions relative to the supplied offsets.
    private static List<(int baseI, int newI, int count)> ComputeGreedyRuns(
        string[] baseLines, int baseOffset, int baseCount,
        string[] newLines, int newOffset, int newCount)
    {
        var baseIndex = new Dictionary<string, List<int>>();
        for (int i = 0; i < baseCount; i++)
        {
            var line = baseLines[baseOffset + i];
            if (!baseIndex.TryGetValue(line, out var positions))
                baseIndex[line] = positions = new List<int>();
            positions.Add(i);
        }

        var matches = new List<(int baseI, int newI)>();
        int lastBase = -1;
        for (int j = 0; j < newCount; j++)
        {
            var line = newLines[newOffset + j];
            if (!baseIndex.TryGetValue(line, out var positions)) continue;

            // Binary search: first index where positions[idx] > lastBase.
            int lo = 0, hi = positions.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (positions[mid] > lastBase) hi = mid;
                else lo = mid + 1;
            }
            if (lo >= positions.Count) continue;

            int matchedBase = positions[lo];
            matches.Add((matchedBase, j));
            lastBase = matchedBase;
        }

        var runs = new List<(int baseI, int newI, int count)>();
        int k = 0;
        while (k < matches.Count)
        {
            int bStart = matches[k].baseI, nStart = matches[k].newI, cnt = 1;
            while (k + cnt < matches.Count &&
                   matches[k + cnt].baseI == bStart + cnt &&
                   matches[k + cnt].newI == nStart + cnt)
                cnt++;
            runs.Add((bStart, nStart, cnt));
            k += cnt;
        }

        return runs;
    }

    private static void AppendEdAddress(StringBuilder sb, int start, int end)
    {
        sb.Append(start);
        if (start != end) sb.Append(',').Append(end);
    }

    private static string[] SplitLines(string content)
        => content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

    public override string ToString()
    {
        using var sw = new StringWriter();
        sw.WriteLine("AAO-BugReport-File/1.0");
        // write headers
        foreach (var header in Fields)
        {
            sw.WriteLine($"{header.Key}: {header.Value}");
        }
        sw.WriteLine(); // end of headers

        // write parts
        foreach (var blob in Blobs)
        {
            sw.WriteLine($"--{_boundary}");
            foreach (var header in blob.Headers)
            {
                sw.WriteLine($"{header.Key}: {header.Value}");
            }
            sw.WriteLine(); // end of part headers
            sw.WriteLine(blob.Content);
        }

        return sw.ToString();
    }

    internal struct ReportBlob
    {
        public List<KeyValuePair<string, string>> Headers;
        public string Content;
    }
}
