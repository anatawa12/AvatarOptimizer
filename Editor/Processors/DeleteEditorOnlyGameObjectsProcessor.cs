using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class DeleteEditorOnlyGameObjectsProcessor
    {
        public void Process(OptimizerSession session)
        {
            var deleteEditorOnlyGameObjects = session.GetRootComponent<DeleteEditorOnlyGameObjects>();
            if (!deleteEditorOnlyGameObjects) return;

            foreach (var component in session.GetComponents<Transform>())
            {
                if (component.gameObject.CompareTag("EditorOnly"))
                {
                    session.Destroy(component.gameObject);
                }
            }
        }
    }
}
