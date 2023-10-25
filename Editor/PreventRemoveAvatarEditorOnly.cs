#if AAO_VRCSDK3_AVATARS

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDKBase.Network;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer
{
    // See https://feedback.vrchat.com/sdk-bug-reports/p/ieditoronly-components-should-be-destroyed-late-in-the-build-process
    // See https://twitter.com/bd_j/status/1651227127960805385
    // See https://github.com/bdunderscore/modular-avatar/blob/47a1e8393c073acef80c0aa1f1ca12b149fea3a1/Packages/nadena.dev.modular-avatar/Editor/PreventStripTagObjects.cs
    [InitializeOnLoad]
    class PreventRemoveAvatarEditorOnly
    {
        static PreventRemoveAvatarEditorOnly()
        {
            EditorApplication.delayCall += () =>
            {
                var field = typeof(VRCBuildPipelineCallbacks).GetField("_preprocessAvatarCallbacks",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Debug.Assert(field != null, nameof(field) + " != null: VRCSDK version is not compatbile");
                var callbacks = (List<IVRCSDKPreprocessAvatarCallback>)field.GetValue(null);

                callbacks.RemoveAll(c => c is RemoveAvatarEditorOnly || c.GetType().FullName ==
                    "nadena.dev.modular_avatar.core.editor.ReplacementRemoveAvatarEditorOnly");
            };
        }
    }

    internal class ReplacementRemoveAvatarEditorOnlyGameObject : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => -1024;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            foreach (var transform in avatarGameObject.GetComponentsInChildren<Transform>(true))
                if (transform && transform.CompareTag("EditorOnly"))
                    Object.DestroyImmediate(transform.gameObject);
            return true;
        }
    }

    internal class ReplacementRemoveAvatarEditorOnlyComponents : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => 1024;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            foreach (var component in avatarGameObject.GetComponentsInChildren<IEditorOnly>(true))
                if ((Object)component)
                    Object.DestroyImmediate((Component) component);
            return true;
        }
    }

    internal class AaoReassignNetworkId : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => 65536;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            NetworkIDAssignment.ConfigureNetworkIDs(avatarGameObject.GetComponent<VRC_AvatarDescriptor>());
            return true;
        }
    }
}

#endif