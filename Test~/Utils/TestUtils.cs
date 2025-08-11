using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Test
{
    public static class TestUtils
    {
        class DummyAvatarTagComponent : AvatarTagComponent {}

        public static GameObject NewAvatar(string name = null)
        {
            var root = new GameObject();
            root.name = name ?? "Test Avatar";
            var animator = root.AddComponent<Animator>();
            animator.avatar = AvatarBuilder.BuildGenericAvatar(root, "");
#if AAO_VRCSDK3_AVATARS
            var descriptor = root.AddComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
#endif
            // for any AvatarTagComponent checks on the avatar
            root.AddComponent<DummyAvatarTagComponent>();
            return root;
        }

        public static void SetFxLayer(GameObject root, RuntimeAnimatorController controller)
        {
#if AAO_VRCSDK3_AVATARS
            var descriptor = root.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            descriptor.customizeAnimationLayers = true;
            descriptor.specialAnimationLayers ??= new VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.CustomAnimLayer[]
            {
                new()
                {
                    type = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.Sitting,
                },
                new()
                {
                    type = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.TPose,
                },
                new()
                {
                    type = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.IKPose,
                },
            };
            descriptor.baseAnimationLayers ??= new VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.CustomAnimLayer[]
            {
                new()
                {
                    type = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.Base,
                },
                new()
                {
                    type = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.Action,
                },
                new()
                {
                    type = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX,
                },
            };
            var index = Array.FindIndex(descriptor.baseAnimationLayers,
                x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX);
            if (index <= 0)
                throw new InvalidOperationException("FX Layer not found");

            descriptor.baseAnimationLayers[index].animatorController = controller;
            descriptor.baseAnimationLayers[index].isDefault = false;
#else
            var animator = root.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
#endif
        }

        public static string GetAssetPath(string testRelativePath)
        {
            var path = AssetDatabase.GUIDToAssetPath("801b64144a3842adb8909fd2d209241a");
            var baseDir = path.Substring(0, path.LastIndexOf('/'));
            return $"{baseDir}/{testRelativePath}";
        }

        public static T GetAssetAt<T>(string testRelativePath) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(GetAssetPath(testRelativePath));

        public static string GetStateName(AnimatorStateInfo stateInfo, AnimatorController controller)
        {
            foreach (var state in controller.layers.Select(x => x.stateMachine).SelectMany(ACUtils.AllStates))
            {
                var hash = Animator.StringToHash(state.name);
                if (hash == stateInfo.shortNameHash)
                {
                    return state.name;
                }
            }
            return $"<unknown name({stateInfo.shortNameHash:x8})>";
        }

        [MenuItem("Tools/TestNewCubeMesh")]
        static void TestNewCubeMesh()
        {
            var go = new GameObject();
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshFilter>().mesh = NewCubeMesh();
        }

        /// <summary>
        /// This function returns an cube mesh with 8 vertices and 12 triangles at the origin sized 2x2x2.
        /// </summary>
        /// <returns>New cube mesh</returns>
        public static Mesh NewCubeMesh()
        {
            var mesh = new Mesh();
            mesh.vertices = new Vector3[8]
            {
                new (-1, -1, -1),
                new (-1, -1, +1),
                new (-1, +1, -1),
                new (-1, +1, +1),
                new (+1, -1, -1),
                new (+1, -1, +1),
                new (+1, +1, -1),
                new (+1, +1, +1),
            };
            mesh.triangles = new int[12 * 3]
            {
                0, 1, 2,
                1, 3, 2,
                4, 6, 5,
                5, 6, 7,
                0, 2, 4,
                2, 6, 4,
                1, 5, 3,
                3, 5, 7,
                0, 4, 1,
                1, 4, 5,
                2, 3, 6,
                3, 7, 6,
            };
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, 12 * 3));
            return mesh;
        }
        
        public static Vector3[] NewCubeBlendShapeFrame(params (int index, Vector3 delta)[] deltas)
        {
            var frame = new Vector3[8];
            foreach (var (index, delta) in deltas) frame[index] = delta;
            return frame;
        }

        public static SkinnedMeshRenderer NewSkinnedMeshRenderer(Mesh mesh)
        {
            var go = new GameObject();
            var renderer = go.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            return renderer;
        }

        #nullable enable

        private static FieldInfo? _comparerFieldInfo; 

        public static EqualConstraint UsingTupleAdapter(this EqualConstraint constraint)
        {
            if (constraint == null) throw new ArgumentNullException(nameof(constraint));
            if (_comparerFieldInfo == null)
            {
                _comparerFieldInfo =
                    typeof(EqualConstraint).GetField("_comparer", BindingFlags.NonPublic | BindingFlags.Instance);
                if (_comparerFieldInfo == null)
                    throw new InvalidOperationException("comparer field not found");
            }
            var comparer = (NUnitEqualityComparer)_comparerFieldInfo.GetValue(constraint);
            comparer.ExternalComparers.Insert(0, new TupleEqualityAdapter(comparer));
            return constraint;
        }

        private class TupleEqualityAdapter : EqualityAdapter
        {
            private NUnitEqualityComparer baseComparator;

            public TupleEqualityAdapter(NUnitEqualityComparer baseComparator) => this.baseComparator = baseComparator;

            public override bool CanCompare(object? x, object? y)
            {
                if (x == null) return false;
                if (y == null) return false;
                var xType = x.GetType();
                var yType = y.GetType();
                if (xType != yType) return false;
                if (!xType.IsGenericType) return false;
                return IsValueTuple(xType);

                bool IsValueTuple(Type xType)
                {
                    if (xType == typeof(ValueTuple)) return true;
                    if (!xType.IsGenericType) return false;
                    var genericType = xType.GetGenericTypeDefinition();
                    if (genericType == typeof(ValueTuple<>)) return true;
                    if (genericType == typeof(ValueTuple<,>)) return true;
                    if (genericType == typeof(ValueTuple<,,>)) return true;
                    if (genericType == typeof(ValueTuple<,,,>)) return true;
                    if (genericType == typeof(ValueTuple<,,,,>)) return true;
                    if (genericType == typeof(ValueTuple<,,,,,>)) return true;
                    if (genericType == typeof(ValueTuple<,,,,,,>)) return true;
                    if (genericType == typeof(ValueTuple<,,,,,,,>)) 
                    {
                        var restType = xType.GetGenericArguments()[7];
                        return IsValueTuple(restType);
                    }

                    return false;
                }
            }

            public override bool AreEqual(object? x, object? y)
            {
                if (x == null) return y == null;
                if (y == null) return false;
                var xType = x.GetType();
                var yType = y.GetType();
                if (xType != yType) return false;
                var xTuple = x as System.Runtime.CompilerServices.ITuple;
                var yTuple = y as System.Runtime.CompilerServices.ITuple;
                if (xTuple == null) return false;
                if (yTuple == null) return false;

                var xLength = xTuple.Length;
                var yLength = yTuple.Length;
                if (xLength != yLength) return false;

                for (int i = 0; i < xLength; i++)
                {
                    var xItem = xTuple[i];
                    var yItem = yTuple[i];
                    var exact = Tolerance.Exact;
                    if (!baseComparator.AreEqual(xItem, yItem, ref exact))
                    {
                        baseComparator.FailurePoints.Insert(0, new NUnitEqualityComparer.FailurePoint
                        {
                            Position = i,
                            ExpectedHasData = true,
                            ExpectedValue = xItem,
                            ActualHasData = true,
                            ActualValue = yItem
                        });
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
