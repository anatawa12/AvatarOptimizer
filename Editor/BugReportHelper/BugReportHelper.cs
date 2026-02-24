using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.Dynamics;
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

    private void OnGUI()
    {
        AAOL10N.DrawLanguagePicker();

        GUILayout.Label("Bug Report Helper", EditorStyles.boldLabel);
        targetAvatar = (GameObject)EditorGUILayout.ObjectField("Target Avatar", targetAvatar, typeof(GameObject), allowSceneObjects: true);

        GUILayout.Space(10);

        GUILayout.Label(AAOL10N.Tr("BugReportHelper:description"));

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
                    var reportFile = RunBuild(targetAvatar);
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
                var reportFile = RunBuild(targetAvatar);
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

    public static ReportFile RunBuild(GameObject avatar)
    {
        var clonedAvatar = Instantiate(avatar);
        try
        {
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

        public BugReporterLogHandler(ILogHandler? upstream) => _upstream = upstream;

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            _upstream?.LogFormat(logType, context, format, args);
            // log with timestamp, logtype, message, and stacktrace
            _builder.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] [{logType}] {string.Format(format, args)}");
            _builder.AppendLine(Environment.StackTrace);
            _builder.AppendLine();
        }

        public void LogException(Exception exception, Object context)
        {
            _upstream?.LogException(exception, context);

            _builder.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] [Exception] {exception}");
            _builder.AppendLine(exception.StackTrace);
            _builder.AppendLine();
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

    public void AddGcDebugInfo(InternalGcDebugPosition position, string collectDataToString, GameObject root)
    {
        ReportFile.AddFile($"GCDebug.{position}.tree.txt", collectDataToString);
        ReportFile.AddFile($"AvatarInfo.{position}.tree.txt", BugReportHelper.CollectAvatarInfo(root));
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

    public void AddFile(string fileName, string content)
    {
        var headers = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Content-Disposition", $"attachment; filename=\"{fileName}\""),
        };
        Blobs.Add(new ReportBlob
        {
            Headers = headers,
            Content = content,
        });
    }

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
