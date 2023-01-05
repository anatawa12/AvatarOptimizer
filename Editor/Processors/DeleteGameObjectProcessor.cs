using UnityEngine;

namespace Anatawa12.Merger.Processors
{
    internal class DeleteGameObjectProcessor
    {
        public void Process(MergerSession session)
        {
            foreach (var mergePhysBone in session.GetComponents<DeleteGameObject>())
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
            }
        }
    }
}
