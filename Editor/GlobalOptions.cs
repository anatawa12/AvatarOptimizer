using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer;

[InitializeOnLoad]
internal static class GlobalOptions
{
    private const string ToggleMeshValidationSessionKey = "AvatarOptimizer.MeshValidationEnabled";
    private const string ToggleMeshValidationMenuName = "Tools/Avatar Optimizer/Mesh Invariant Validation";

    public static bool MeshValidationEnabled
    {
        get => MeshInfo2.MeshValidationEnabled;
        set
        {
            MeshInfo2.MeshValidationEnabled = value;
            SessionState.SetBool(ToggleMeshValidationSessionKey, value);
            Menu.SetChecked(ToggleMeshValidationMenuName, value);
        }
    }

    static GlobalOptions()
    {
        EditorApplication.delayCall += () =>
        {
            EditorApplication.delayCall += () =>
            {
                MeshValidationEnabled =
                    SessionState.GetBool(ToggleMeshValidationSessionKey, CheckForUpdate.Checker.IsBeta);
            };
        };
    }

    [MenuItem(ToggleMeshValidationMenuName)]
    public static void ToggleMeshValidation()
    {
        MeshValidationEnabled = !MeshValidationEnabled;
    }
}
