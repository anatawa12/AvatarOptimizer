using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors;
using Anatawa12.AvatarOptimizer.ndmf;
using nadena.dev.ndmf;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    internal class OptimizerSession
    {
        private readonly GameObject _rootObject;
        public bool IsTest { get; }
        public ObjectMappingBuilder MappingBuilder { get; }
        public MeshInfo2Holder MeshInfo2Holder { get; private set; }

        public static implicit operator OptimizerSession(BuildContext context)
        {
            return context.Extension<OptimizerContext>().session;
        }

        public OptimizerSession(GameObject rootObject, bool isTest)
        {
            IsTest = isTest;
            _rootObject = rootObject;
            MappingBuilder = new ObjectMappingBuilder(rootObject);
            MeshInfo2Holder = new MeshInfo2Holder(rootObject);
        }

        public IEnumerable<T> GetComponents<T>() where T : Component
        {
            return (_rootObject != null ? _rootObject.GetComponentsInChildren<T>(true) : Object.FindObjectsOfType<T>())
                .Where(x => x);
        }

        public void SaveMeshInfo2()
        {
            MeshInfo2Holder.SaveToMesh();
            MeshInfo2Holder = null;
        }
    }
}
