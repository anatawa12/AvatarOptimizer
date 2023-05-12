using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Anatawa12.AvatarOptimizer
{
    [InitializeOnLoad]
    public static class MaterialSaverUtility
    {
        static MaterialSaverUtility()
        {
            EditorApplication.update += Update;
        }

        public static Material[] GetMaterials(Renderer renderer) =>
            SessionState.instance.Mapping.TryGetValue(renderer, out var materials)
                ? materials
                : renderer.sharedMaterials;

        private static void Update()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
            {
                SessionState.instance.Mapping.Clear();
                foreach (var renderer in Enumerable.Range(0, SceneManager.sceneCount)
                             .Select(SceneManager.GetSceneAt)
                             .SelectMany(x => x.GetRootGameObjects())
                             .SelectMany(x => x.GetComponentsInChildren<Renderer>(true)))
                {
                    SessionState.instance.Mapping[renderer] = renderer.sharedMaterials;
                }
            }

            if (!EditorApplication.isPlaying)
            {
                SessionState.instance.Mapping.Clear();
            }
        }

        private class SessionState : ScriptableSingleton<SessionState>, ISerializationCallbackReceiver
        {
            [FormerlySerializedAs("materials")] [SerializeField] private MappingData[] serialized;

            public readonly Dictionary<Renderer, Material[]> Mapping = new Dictionary<Renderer, Material[]>();

            [Serializable]
            public struct MappingData
            {
                public Renderer renderer;
                public Material[] materials;

                public MappingData(KeyValuePair<Renderer, Material[]> kvp) =>
                    (renderer, materials) = (kvp.Key, kvp.Value);
            }

            public void OnBeforeSerialize()
            {
                serialized = Mapping.Select(kvp => new MappingData(kvp)).ToArray();
            }

            public void OnAfterDeserialize()
            {
                Mapping.Clear();
#if NET_STANDARD_2_1
                Mapping.EnsureCapacity(materials.Count);
#endif
                foreach (var mappingData in serialized)
                    Mapping[mappingData.renderer] = mappingData.materials;
            }
        }
    }
}
