
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
            foreach (var mergePhysBone in avatarGameObject.GetComponentsInChildren<MergePhysBone>())
            {
                if (!mergePhysBone.IsValid()) return false;
                mergePhysBone.DoMerge();
                Object.DestroyImmediate(mergePhysBone);
            }

            return true;
        }
    }
}
