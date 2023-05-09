using Anatawa12.AvatarOptimizer.ErrorReporting;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class DeleteGameObjectProcessor
    {
        public void Process(OptimizerSession session)
        {
            BuildReport.ReportingObjects(session.GetComponents<DeleteGameObject>(), mergePhysBone =>
            {
                void Destroy(Object obj)
                {
                    session.Destroy(obj);
                    session.AddObjectMapping(obj, null);
                }

                mergePhysBone.transform.parent = null;
                Destroy(mergePhysBone.gameObject);
                foreach (var component in mergePhysBone.GetComponents<Component>())
                    Destroy(component);
                mergePhysBone.transform.WalkChildren(x =>
                {
                    Destroy(x.gameObject);
                    foreach (var component in x.GetComponents<Component>())
                        Destroy(component);
                    return true;
                });
            });
        }
    }
}
