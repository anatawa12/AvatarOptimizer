
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

        public static void ProcessObject(MergerSession session)
        {
            new Processors.MergePhysBoneProcessor().Merge(session);
            new Processors.MergeSkinnedMeshProcessor().Merge(session);

            new Processors.ApplyObjectMapping().Apply(session);
            new Processors.ApplyDestroy().Apply(session);
        }
    }
}
