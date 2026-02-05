using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
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
        targetAvatar = (GameObject)EditorGUILayout.ObjectField("Target Avatar", targetAvatar, typeof(GameObject), allowSceneObjects: false);

        GUILayout.Space(10);

        GUILayout.Label(AAOL10N.Tr("BugReportHelper:description"));

        GUILayout.Space(10);

        if (targetAvatar == null)
        {
            EditorGUILayout.HelpBox("Please select an avatar GameObject to generate bug report.", MessageType.Info);
            return;
        }

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
        }
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
            reportFile.AddFile("ComponentInfoRegistry.tree.txt", APIInternal.ComponentInfoRegistry.GetAsText());
            reportFile.AddFile("ShaderInformationRegistry.tree.txt", API.ShaderInformationRegistry.GetAsText());

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

            return reportFile;
        }
        finally
        {
            DestroyImmediate(clonedAvatar);
        }
    }

    private static string CollectAvatarInfo(GameObject clonedAvatar)
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
                        break;

#if AAO_VRCSDK3_AVATARS
                    case VRCPhysBoneBase physBone:
                        builder.AppendLine($"    rootTransform: {ComponentPath(physBone.rootTransform)}");
                        for (var i = 0; i < physBone.colliders.Count; i++)
                        {
                            var collider = physBone.colliders[i];
                            builder.AppendLine($"    collider[{i}]: {ComponentPath(collider)}");
                        }
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
                    if (c == null) return "<Missing>";
                    return Utils.RelativePath(clonedAvatar.transform, c.transform);
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
                var version = lockedInfo.version;
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
                public string version;
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
                var version = locked.version;
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
                public string version;
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
    public ReportFile ReportFile;

    public Context(ReportFile reportFile)
    {
        ReportFile = reportFile;
    }

    public void AnimatorParserResult(BuildContext context, RootPropModNodeContainer modifications)
    {
        ReportFile.AddFile("AnimatorParser.tree.txt", AnimatorParserDebugWindow.CreateText(modifications, context.AvatarRootObject, detailed: true));
    }

    public void AddGcDebugInfo(InternalGcDebugPosition position, string collectDataToString)
    {
        ReportFile.AddFile($"GCDebug.{position}.tree.txt", collectDataToString);
    }
}

// A report file consists of:
// - multiple set of key-value pairs
// - multiple text blobs, with their own key-value pairs as metadata
// The format of the report file is like multipart/form-data in HTTP, with several differences:
// - The file begins with a fixed header line: "AAO-BugReport-File/1.0"
// - The file will have 'header' fields before the first part. two newlines separate the header and the first part, similar to HTTP/1.1
// - The boundary will be stored in the header as "Boundary: <boundary-string>", since there is no external place to store it.
// - Each field's key-value paris cannot be concatenated into single line like Set-Cookie in HTTP, as opposed to most HTTP headers that can be concatenated with commas.
//   You must store header fields as `List<KeyValuePair<string, string>>`, rather than `Dictionary<string, string>`.
internal class ReportFile
{
    private string boundary;

    // fields
    public List<KeyValuePair<string, string>> Fields = new();
    // blobs
    public List<ReportBlob> Blobs = new();

    public ReportFile()
    {
        boundary = Guid.NewGuid().ToString("N");
        AddField("boundary", boundary, @internal: true);
    }

    public void AddField(string key, string value) => AddField(key, value, @internal: false);

    private void AddField(string key, string value, bool @internal)
    {
        if (!@internal)
            if (key.ToLowerInvariant() == "boundary")
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
            sw.WriteLine($"--{boundary}");
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
