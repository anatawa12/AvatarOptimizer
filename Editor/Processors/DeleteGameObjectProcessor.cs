using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class DeleteGameObjectProcessor
    {
        public void Process(OptimizerSession session)
        {
            foreach (var mergePhysBone in session.GetComponents<DeleteGameObject>())
            {
                session.MappingBuilder.RecordRemoveGameObject(mergePhysBone.gameObject);
                mergePhysBone.transform.parent = null;
                session.Destroy(mergePhysBone.gameObject);
            }
        }
    }
}
