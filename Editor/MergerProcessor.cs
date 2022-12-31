
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase.Editor.BuildPipeline;
using Object = UnityEngine.Object;

namespace Anatawa12.Merger
{
    internal class MergerProcessor : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => 0;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            try
            {
                ProcessObject(avatarGameObject);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        public static void ProcessObject(GameObject gameObject)
        {
            var session = new MergerSession(gameObject);
            new Processors.MergePhysBoneProcessor().Merge(session);
            new Processors.MergeSkinnedMeshProcessor().Merge(session);
            // TODO: process mapping objects
        }
    }
}
