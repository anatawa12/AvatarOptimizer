using UnityEditor;
using UnityEngine;

namespace Anatawa12.Merger.Processors
{
    internal class ApplyObjectMapping
    {
        public void Apply(MergerSession session)
        {
            // replace all objects
            foreach (var component in session.GetComponents<Component>())
            {
                var serialized = new SerializedObject(component);
                var p = serialized.GetIterator();
                while (p.NextVisible(true))
                    if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue != null)
                        if (session.GetMapping().TryGetValue(p.objectReferenceValue, out var mapped))
                            p.objectReferenceValue = mapped;
                serialized.ApplyModifiedProperties();
            }

            // map animator controllers
            // TODO
        }
    }
}
