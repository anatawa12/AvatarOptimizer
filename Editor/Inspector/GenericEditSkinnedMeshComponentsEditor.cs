using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer;

internal static class GenericEditSkinnedMeshComponentsEditor
{
    public static void DrawUnexpectedRendererError(Object[] objects)
    {
        if (objects.Any(x => ((Component)x).GetComponent<Renderer>() is not (SkinnedMeshRenderer or MeshRenderer)))
        {
            EditorGUILayout.HelpBox(AAOL10N.Tr("EditSkinnedMeshComponents:invalid-renderer-type"), MessageType.Error);
        }
    }
}
