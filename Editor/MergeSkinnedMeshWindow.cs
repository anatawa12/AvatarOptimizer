using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal class MergeSkinnedMeshWindow : EditorWindow
    {
        private Mesh? _mesh;

        private void OnGUI()
        {
            _mesh = (Mesh)EditorGUILayout.ObjectField(_mesh, typeof(Mesh), true);
            if (_mesh != null)
            {
                SelectableLabelField(nameof(_mesh.indexFormat), _mesh.indexFormat);
                SelectableLabelField(nameof(_mesh.vertexBufferCount), _mesh.vertexBufferCount);
                SelectableLabelField(nameof(_mesh.blendShapeCount), _mesh.blendShapeCount);
                PrintInfo(nameof(_mesh.bindposes), _mesh.bindposes);
                //PrintInfo(nameof(_mesh.isReadable), _mesh.isReadable);
                //PrintInfo(nameof(_mesh.canAccess), _mesh.canAccess); internal
                //PrintInfo(nameof(_mesh.vertexCount), _mesh.vertexCount);
                //PrintInfo(nameof(_mesh.subMeshCount), _mesh.subMeshCount);
                //PrintInfo(nameof(_mesh.bounds), _mesh.bounds);
                PrintInfo(nameof(_mesh.vertices), _mesh.vertices);
                PrintInfo(nameof(_mesh.normals), _mesh.normals);
                PrintInfo(nameof(_mesh.tangents), _mesh.tangents);
                PrintInfo(nameof(_mesh.uv), _mesh.uv);
                PrintInfo(nameof(_mesh.uv2), _mesh.uv2);
                PrintInfo(nameof(_mesh.uv3), _mesh.uv3);
                PrintInfo(nameof(_mesh.uv4), _mesh.uv4);
                PrintInfo(nameof(_mesh.uv5), _mesh.uv5);
                PrintInfo(nameof(_mesh.uv6), _mesh.uv6);
                PrintInfo(nameof(_mesh.uv7), _mesh.uv7);
                PrintInfo(nameof(_mesh.uv8), _mesh.uv8);
                PrintInfo(nameof(_mesh.colors), _mesh.colors);
                PrintInfo(nameof(_mesh.colors32), _mesh.colors32);
                //PrintInfo(nameof(_mesh.vertexAttributeCount), _mesh.vertexAttributeCount);
                PrintInfo(nameof(_mesh.triangles), _mesh.triangles);
                PrintInfo(nameof(_mesh.boneWeights), _mesh.boneWeights);
                SelectableLabelField("BlendShape count", _mesh.blendShapeCount);
                SelectableLabelField("All Weights", _mesh.GetAllBoneWeights().Length);
                SelectableLabelField("BonesPerVertex", _mesh.GetBonesPerVertex().Length);
                SelectableLabelField("subMeshCount", _mesh.subMeshCount);
                for (var i = 0; i < _mesh.subMeshCount; i++)
                    SelectableLabelField($"subMesh #{i}", _mesh.GetSubMesh(i));
                var attributes = _mesh.GetVertexAttributes();
                for (var i = 0; i < attributes.Length; i++)
                {
                    SelectableLabelField($"attr #{i}", attributes[i]);
                }
            }
        }

        private void SelectableLabelField<T>(string label, T value) where T : notnull
        {
            var fullRect = EditorGUILayout.GetControlRect(true);
            var elementRect = EditorGUI.PrefixLabel(fullRect, new GUIContent(label));
            EditorGUI.SelectableLabel(elementRect, value.ToString());
        }

        private void PrintInfo<T>(string prop, T[] array)
        {
            SelectableLabelField(prop, array == null ? "null" : array.Length.ToString());
        }

        [MenuItem("Tools/Avatar Optimizer/Test GUI")]
        public static void Open() => CreateWindow<MergeSkinnedMeshWindow>();
    }
}
