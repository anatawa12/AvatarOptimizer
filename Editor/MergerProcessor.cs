
using System;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Anatawa12.Merger
{
    internal class MergerProcessor : IVRCSDKPreprocessAvatarCallback, IVRCSDKPostprocessAvatarCallback
    {
        public int callbackOrder => 0;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            try
            {
                ProcessObject(new MergerSession(avatarGameObject, true));
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

        public static void ProcessObject(MergerSession session)
        {
            if (_processing) return;
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
            }
        }
        
        private static void DoProcessObject(MergerSession session)
        {
            new Processors.MergePhysBoneProcessor().Merge(session);
            new Processors.MergeSkinnedMeshProcessor().Merge(session);

            new Processors.ApplyObjectMapping().Apply(session);
            new Processors.ApplyDestroy().Apply(session);
        }
    }
}
