using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors;
using Anatawa12.AvatarOptimizer.ndmf;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    internal class OptimizerSession
    {
        private readonly GameObject _rootObject;
        public bool IsTest { get; }
        public ObjectMappingBuilder MappingBuilder { get; }
        public MeshInfo2Holder MeshInfo2Holder { get; private set; } = new MeshInfo2Holder();

        public static implicit operator OptimizerSession(BuildContext context)
        {
            return context.Extension<OptimizerContext>().session;
        }

        public OptimizerSession(GameObject rootObject, bool isTest)
        {
            IsTest = isTest;
            _rootObject = rootObject;
            MappingBuilder = new ObjectMappingBuilder(rootObject);
        }

        public T GetRootComponent<T>() where T : Component
        {
            return _rootObject != null ? _rootObject.GetComponent<T>() : null;
        }

        public IEnumerable<T> GetComponents<T>() where T : Component
        {
            return (_rootObject != null ? _rootObject.GetComponentsInChildren<T>(true) : Object.FindObjectsOfType<T>())
                .Where(x => x);
        }

        public string RelativePath(Transform child)
        {
            return Utils.RelativePath(_rootObject.transform, child) ??
                   throw new ArgumentException("child is not child of rootObject", nameof(child));
        }

        public void SaveMeshInfo2()
        {
            MeshInfo2Holder.SaveToMesh(this);
            MeshInfo2Holder = null;
        }
    }
}
