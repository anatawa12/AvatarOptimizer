using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    class AutoFreezeBlendShape
    {
        private readonly AnimatorParser _animator;
        private readonly OptimizerSession _session;

        public AutoFreezeBlendShape(AnimatorParser animator, OptimizerSession session)
        {
            _animator = animator;
            _session = session;
        }

        public void Process()
        {
            foreach (var skinnedMeshRenderer in _session.GetComponents<SkinnedMeshRenderer>())
            {
                var mesh = skinnedMeshRenderer.sharedMesh;

                // skip SMR without mesh
                if (!mesh) continue;
                // skip configured mesh
                if (skinnedMeshRenderer.GetComponent<FreezeBlendShape>()) continue;

                var modifies = _animator.GetModifiedProperties(skinnedMeshRenderer);
                var blendShapeValues = Enumerable.Range(0, mesh.blendShapeCount)
                    .Select(i => skinnedMeshRenderer.GetBlendShapeWeight(i)).ToArray();
                var notChanged = Enumerable.Range(0, mesh.blendShapeCount)
                    .Select(i => mesh.GetBlendShapeName(i))
                    .Where((name, i) =>
                    {
                        if (!modifies.TryGetValue($"blendShape.{name}", out var prop)) return true;

                        if (!prop.IsConst) return false;

                        if (prop.IsAlwaysApplied)
                        {
                            blendShapeValues[i] = prop.ConstValue;
                            return true;
                        }

                        return prop.ConstValue.CompareTo(blendShapeValues[i]) == 0;
                    })
                    .ToArray();

                if (notChanged.Length == 0) continue;

                for (var i = 0; i < blendShapeValues.Length; i++)
                    skinnedMeshRenderer.SetBlendShapeWeight(i, blendShapeValues[i]);
                EditorUtility.SetDirty(skinnedMeshRenderer);

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