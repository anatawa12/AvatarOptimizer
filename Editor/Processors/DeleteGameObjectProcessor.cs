using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class DeleteGameObjectProcessor
    {
        public void Process(OptimizerSession session)
        {
            foreach (var mergePhysBone in session.GetComponents<DeleteGameObject>())
            {
                mergePhysBone.transform.parent = null;
                session.MappingBuilder.RecordRemoveGameObject(mergePhysBone.gameObject);
                session.Destroy(mergePhysBone.gameObject);
            }
        }
    }
}
