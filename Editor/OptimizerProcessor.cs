
using System;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Anatawa12.AvatarOptimizer
{
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
            try
            {
                AssetDatabase.StartAssetEditing();
                _processing = true;
                DoProcessObject(session);
            }
            finally
            {
                _processing = false;
                AssetDatabase.StopAssetEditing();
                foreach (var component in session.GetComponents<AvatarTagComponent>())
                    UnityEngine.Object.DestroyImmediate(component);
                foreach (var activator in session.GetComponents<AvatarActivator>())
                    UnityEngine.Object.DestroyImmediate(activator);

                AssetDatabase.SaveAssets();
            }
        }
        
        private static void DoProcessObject(OptimizerSession session)
        {
            new Processors.UnusedBonesByReferencesToolProcessor().Process(session);
            new Processors.DeleteEditorOnlyGameObjectsProcessor().Process(session);
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
