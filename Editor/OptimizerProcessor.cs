
using System;
using System.Linq;
using Anatawa12.ApplyOnPlay;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// the Processor runs before removing EditorOnly
    /// </summary>
    internal class EarlyOptimizerProcessor : IVRCSDKPreprocessAvatarCallback, IApplyOnPlayCallback
    {
        public int callbackOrder => -2048;
        public string CallbackName => "Avatar Optimizer Early (Before IEditorOnly Deletion)";
        public string CallbackId => "com.anatawa12.avatar-optimizer.early";

        public bool ApplyOnPlay(GameObject avatarGameObject, ApplyReason reason)
        {
            if (CheckForMissingComponents(avatarGameObject)) return false;
            ProcessObject(new OptimizerSession(avatarGameObject, Utils.CreateOutputAssetFile(avatarGameObject, reason),
                reason == ApplyReason.EnteringPlayMode));
            return true;
        }

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            if (CheckForMissingComponents(avatarGameObject)) return false;
            try
            {
                ProcessObject(new OptimizerSession(avatarGameObject, true, false));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        private bool CheckForMissingComponents(GameObject gameObject)
        {
            var error = false;
            using (BuildReport.ReportingOnAvatar(gameObject.GetComponent<VRCAvatarDescriptor>()))
            {
                foreach (var children in gameObject.GetComponentsInChildren<Transform>(true))
                {
                    if (children.gameObject.GetComponents<Component>().Any(x => x is null))
                    {
                        BuildReport.LogFatal("Missing Script Component Detected!")?.WithContext(children.gameObject);
                        error = true;
                    }
                }
            }

            return error;
        }

        private static bool _processing;

        internal static void ProcessObject(OptimizerSession session)
        {
            if (_processing) return;
            using (Utils.StartEditingScope(true))
            using (BuildReport.ReportingOnAvatar(session.GetRootComponent<VRCAvatarDescriptor>()))
            {
                try
                {
                    _processing = true;
                    DoProcessObject(session);
                }
                catch (Exception e)
                {
                    BuildReport.ReportInternalError(e);
                    throw;
                }
                finally
                {
                    _processing = false;
                    session.MarkDirtyAll();
                }
            }
        }

        private static void DoProcessObject(OptimizerSession session)
        {
            new Processors.UnusedBonesByReferencesToolEarlyProcessor().Process(session);
            new Processors.MakeChildrenProcessor(early: true).Process(session);
            session.MarkDirtyAll();
        }
    }

    internal class OptimizerProcessor : IVRCSDKPreprocessAvatarCallback, IVRCSDKPostprocessAvatarCallback, IApplyOnPlayCallback
    {
        public int callbackOrder => 0;
        public string CallbackName => "Avatar Optimizer Main";
        public string CallbackId => "com.anatawa12.avatar-optimizer.main";

        public bool ApplyOnPlay(GameObject avatarGameObject, ApplyReason reason)
        {
            ProcessObject(new OptimizerSession(avatarGameObject, Utils.CreateOutputAssetFile(avatarGameObject, reason),
                reason == ApplyReason.EnteringPlayMode));
            return true;
        }

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            try
            {
                ProcessObject(new OptimizerSession(avatarGameObject, true, false));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        public void OnPostprocessAvatar()
        {
            Utils.DeleteTemporalDirectory();
        }

        private static bool _processing;

        public static void ProcessObject(OptimizerSession session)
        {
            if (_processing) return;
            using (Utils.StartEditingScope(true))
            using (BuildReport.ReportingOnAvatar(session.GetRootComponent<VRCAvatarDescriptor>()))
            {
                try
                {
                    _processing = true;
                    DoProcessObject(session);
                }
                catch (Exception e)
                {
                    BuildReport.ReportInternalError(e);
                    throw;
                }
                finally
                {
                    _processing = false;
                    foreach (var component in session.GetComponents<AvatarTagComponent>())
                        UnityEngine.Object.DestroyImmediate(component);
                    foreach (var activator in session.GetComponents<AvatarActivator>())
                        UnityEngine.Object.DestroyImmediate(activator);

                    session.MarkDirtyAll();
                }
            }
        }
        
        private static void DoProcessObject(OptimizerSession session)
        {
            var traceAndOptimize = new Processors.TraceAndOptimizeProcessor();
            traceAndOptimize.Process(session);
            new Processors.ClearEndpointPositionProcessor().Process(session);
            new Processors.MergePhysBoneProcessor().Process(session);
            new Processors.EditSkinnedMeshComponentProcessor().Process(session);
            new Processors.MergeBoneProcessor().Process(session);
            new Processors.MakeChildrenProcessor(early: false).Process(session);

            new Processors.ApplyObjectMapping().Apply(session);
            traceAndOptimize.ProcessLater(session);

            session.SaveMeshInfo2();
            session.MarkDirtyAll();
        }
    }
}
