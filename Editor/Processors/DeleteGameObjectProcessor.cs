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
                session.MappingBuilder.RecordRemoveGameObject(mergePhysBone.gameObject);
                mergePhysBone.transform.parent = null;
                session.Destroy(mergePhysBone.gameObject);
            });
        }
    }
}
