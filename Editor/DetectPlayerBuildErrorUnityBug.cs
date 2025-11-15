using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Anatawa12.AvatarOptimizer;

// Because of unity bug, building avatar may fail with 'Error building Player because scripts had compiler errors'
// without any actual compiler errors if the project path contains non-ASCII characters.
// I don't know why but Avatar Optimizer is likely to trigger this bug so we try to detect this situation and warn user.
internal class DetectPlayerBuildErrorUnityBug : IVRCSDKPreprocessAvatarCallback, IVRCSDKPostprocessAvatarCallback,
    IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    private static PlayerErrorDetectionContext? _detectionContext;

    public bool OnPreprocessAvatar(GameObject avatarGameObject)
    {
        Debug.Log("OnPreprocessAvatar called");
        _detectionContext = new PlayerErrorDetectionContext();
        return true;
    }

    public void OnPostprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {
        _detectionContext?.OnPostprocessBuild();
    }

    public void OnPostprocessAvatar()
    {
        _detectionContext?.OnPostprocessAvatar();
    }

    private class PlayerErrorDetectionContext
    {
        // Phases
        private bool _compilationStarted;
        bool _hasCompilationErrors;
        private bool _postProcessBuildCalled;
        private bool _postProcessAvatarCalled;

        public PlayerErrorDetectionContext()
        {
            EditorApplication.delayCall += NextTick;
            CompilationPipeline.compilationStarted += CompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += AssemblyCompilationFinished;
        }

        private void CompilationStarted(object obj)
        {
            if (!BuildPipeline.isBuildingPlayer) return; // unrelated build
            _compilationStarted = true;
        }

        private void AssemblyCompilationFinished(string arg1, CompilerMessage[] arg2)
        {
            if (!BuildPipeline.isBuildingPlayer) return; // unrelated build
            _hasCompilationErrors |= arg2.Any(x => x.type == CompilerMessageType.Error);
        }

        public void OnPostprocessBuild()
        {
            _postProcessBuildCalled = true;
        }

        public void OnPostprocessAvatar()
        {
            _postProcessAvatarCalled = true;
            CleanUp();
        }

        private void NextTick()
        {
            CleanUp();
            // Building asset bundle finished without abort
            if (!_compilationStarted) return; // No compilation started during build. Probably unrelated.
            if (_hasCompilationErrors) return; // There are some actual compilation errors. We don't need to log anything.
            if (_postProcessBuildCalled) return; // current Unity does not call postprocess when compile errors exist. so we use it to detect no compile error case.
            if (_postProcessAvatarCalled) return; // VRChat SDK does not call call postprocess when asset bundle build fails. so we use it to detect no build error case.

            // If we reach here, it means:
            // - avatar build was started
            // - compilation was started during avatar build
            // - no compilation errors were reported (by AssemblyCompilationFinished)
            // - postprocess build and avatar were not called
            // Therefore, we assume that the build failed due to Unity bug.
            // We check if project path contains non-ASCII characters to be more sure.
            var projectPath = Application.dataPath; // e.g. dataPath == projectPath + "/Assets"
            if (projectPath.All(c => c <= 127)) 
            {
                // log information
                Debug.Log("[AvatarOptimizer] Avatar build failed, but project path contains only ASCII characters. The failure might not be due to the unknown Unity bug.");
            } else {
                // We found non-ASCII characters in project path, so likely due to Unity bug.
                Debug.LogError(AAOL10N.Tr("UnityPlayerBuildErrorBug:non-ascii-detected"));
            }
        }

        private void CleanUp()
        {
            EditorApplication.delayCall -= NextTick;
            CompilationPipeline.compilationStarted -= CompilationStarted;
            CompilationPipeline.assemblyCompilationFinished -= AssemblyCompilationFinished;
        }
    }
}
