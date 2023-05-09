
using System;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// the Processor runs before removing EditorOnly
    /// </summary>
    internal class EarlyOptimizerProcessor : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => -2048;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            try
            {
                ProcessObject(new OptimizerSession(avatarGameObject, true));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        private static bool _processing;

        private static void ProcessObject(OptimizerSession session)
        {
            if (_processing) return;
            using (Utils.StartEditingScope(true))
            {
                try
                {
                    _processing = true;
                    DoProcessObject(session);
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
            using (BuildReport.ReportingOnAvatar(session.GetRootComponent<VRCAvatarDescriptor>()))
            {
                new Processors.UnusedBonesByReferencesToolEarlyProcessor().Process(session);
                session.MarkDirtyAll();
            }
        }
    }

    internal class OptimizerProcessor : IVRCSDKPreprocessAvatarCallback, IVRCSDKPostprocessAvatarCallback
    {
        public int callbackOrder => 0;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            try
            {
                ProcessObject(new OptimizerSession(avatarGameObject, true));
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
            new Processors.ClearEndpointPositionProcessor().Process(session);
            new Processors.MergePhysBoneProcessor().Process(session);
            new Processors.EditSkinnedMeshComponentProcessor().Process(session);
            new Processors.MergeBoneProcessor().Process(session);
            new Processors.DeleteGameObjectProcessor().Process(session);
            new Processors.MakeChildrenProcessor().Process(session);

            new Processors.ApplyObjectMapping().Apply(session);
            new Processors.ApplyDestroy().Apply(session);

            session.MarkDirtyAll();
        }
    }
}
