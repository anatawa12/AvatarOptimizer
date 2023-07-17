using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    partial class AutomaticConfigurationProcessor
    {
        private void AutoFreezeBlendShape()
        {
            foreach (var skinnedMeshRenderer in _session.GetComponents<SkinnedMeshRenderer>())
            {
                var mesh = skinnedMeshRenderer.sharedMesh;

                // skip SMR without mesh
                if (!mesh) continue;
                // skip configured mesh
                if (skinnedMeshRenderer.GetComponent<FreezeBlendShape>()) continue;

                var modifies = GetModifiedProperties(skinnedMeshRenderer);
                var notChanged = Enumerable.Range(0, mesh.blendShapeCount)
                    .Select(i => mesh.GetBlendShapeName(i))
                    .Where(name => !modifies.Contains($"blendShape.{name}"))
                    .ToArray();

                if (notChanged.Length == 0) continue;

                var freeze = skinnedMeshRenderer.gameObject.AddComponent<FreezeBlendShape>();
                var serialized = new SerializedObject(freeze);
                var editorUtil = PrefabSafeSet.EditorUtil<string>.Create(
                    serialized.FindProperty(nameof(FreezeBlendShape.shapeKeysSet)),
                    0, p => p.stringValue, (p, v) => p.stringValue = v);
                foreach (var shape in notChanged)
                    editorUtil.GetElementOf(shape).EnsureAdded();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}