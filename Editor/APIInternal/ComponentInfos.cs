using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.API;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;

namespace Anatawa12.AvatarOptimizer.APIInternal
{
    [ComponentInformation(typeof(Light))]
    [ComponentInformation(typeof(Camera))]
    [ComponentInformation(typeof(Animation))]
    [ComponentInformation(typeof(AudioSource))]
    [ComponentInformationWithGUID("52fa21b17bc14dc294959f976e3e184f", 11500000)] // NDMFAvatarRoot experimental component in NDMF 1.8.0
    internal class EntrypointComponentInformation : ComponentInformation<Component>
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();
        }
    }

    [ComponentInformation(typeof(Transform))]
    [ComponentInformation(typeof(RectTransform))]
    internal class TransformInformation : ComponentInformation<Transform>
    {
        protected override void CollectDependency(Transform component, ComponentDependencyCollector collector)
        {
            // parent dependency is automatically collected on creating GCComponentInfo for Transform
            var casted = (ComponentDependencyRetriever.Collector)collector;
            // For compatibility with UnusedBonesByReferenceTool
            // https://github.com/anatawa12/AvatarOptimizer/issues/429
            if (casted.PreserveEndBone &&
                component.name.EndsWith("end", StringComparison.OrdinalIgnoreCase))
            {
                collector.AddDependency(component.parent, component).EvenIfDependantDisabled();
            }
        }
    }

    [ComponentInformation(typeof(Animator))]
    internal class AnimatorInformation : ComponentInformation<Animator>
    {
        // Animator does not do much for motion, just changes states of other components.
        // All State / Motion Changes are collected separately
        protected override void CollectDependency(Animator component, ComponentDependencyCollector collector)
        {
            // if AnimatorController is not null, it has side effect
            if (component.runtimeAnimatorController) collector.MarkEntrypoint();
            
            // For sub-animators without humanoid bones, we can't call GetBoneTransform or it will result in an exception.
            if (component.isHuman)
            {
                for (var bone = HumanBodyBones.Hips; bone < HumanBodyBones.LastBone; bone++)
                {
                    var boneTransform = component.GetBoneTransform(bone);
                    if (boneTransform == null) continue;
                    // https://github.com/anatawa12/AvatarOptimizer/issues/993
                    // If the bone is moved to out of the hierarchy, it will not be affected by the animator.
                    // however, the Animator component will cache the boneTransform so we need to check before
                    // declaring it as a dependency.
                    if (!boneTransform.IsChildOf(component.transform)) continue;

                    collector.AddPathDependency(boneTransform, component.transform);
                }
            }
        }
    }

    internal class RendererInformation<T> : ComponentInformation<T> where T : Renderer
    {
        protected override void CollectDependency(T component, ComponentDependencyCollector collector)
        {
            var casted = (ComponentDependencyRetriever.Collector)collector;
            AddDependencyInformation(casted._info!, component);
        }

        public static void AddDependencyInformation(GCComponentInfo info, Renderer renderer)
        {
            info.MarkEntrypoint();
            if (info.Activeness != false)
            {
                // anchor proves when this renderer can be rendered.
                if (renderer.reflectionProbeUsage != ReflectionProbeUsage.Off ||
                    renderer.lightProbeUsage != LightProbeUsage.Off)
                    info.AddDependency(renderer.probeAnchor);
                if (renderer.lightProbeUsage == LightProbeUsage.UseProxyVolume)
                    info.AddDependency(renderer.lightProbeProxyVolumeOverride.transform);
            }
        }
    }

    [ComponentInformation(typeof(SkinnedMeshRenderer))]
    internal class SkinnedMeshRendererInformation : RendererInformation<SkinnedMeshRenderer>
    {
        protected override void CollectDependency(SkinnedMeshRenderer component,
            ComponentDependencyCollector collector)
        {
            // IMPORTANT NOTE: We have to use MeshInfo to get information about the mesh!!!
            var casted = (ComponentDependencyRetriever.Collector)collector;
            var meshInfo2 = casted.GetMeshInfoFor(component);
            // SMR without mesh does nothing.
            if (meshInfo2.IsEmpty()) return;

            base.CollectDependency(component, collector);

            AddDependencyInformation(casted._info!, meshInfo2);
        }

        public static void AddDependencyInformation(GCComponentInfo info, Processors.SkinnedMeshes.MeshInfo2 meshInfo2)
        {
            info.MarkEntrypoint();
            foreach (var bone in meshInfo2.Bones)
                info.AddDependency(bone.Transform, GCComponentInfo.DependencyType.Bone);
            if (info.Activeness != false)
                info.AddDependency(meshInfo2.RootBone);
            RendererInformation<SkinnedMeshRenderer>.AddDependencyInformation(info, meshInfo2.SourceRenderer);
        }
    }

    [ComponentInformation(typeof(MeshRenderer))]
    internal class MeshRendererInformation : RendererInformation<MeshRenderer>
    {
        protected override void CollectDependency(MeshRenderer component, ComponentDependencyCollector collector)
        {
            // Mesh renderer without MeshFilter does nothing
            // Mesh renderer without Mesh does nothing
            if (!component.TryGetComponent<MeshFilter>(out var meshFilter) || meshFilter.sharedMesh == null) return;
            base.CollectDependency(component, collector);
            collector.AddDependency(meshFilter).EvenIfDependantDisabled();
        }
    }

    [ComponentInformation(typeof(MeshFilter))]
    internal class MeshFilterInformation : ComponentInformation<MeshFilter>
    {
        protected override void CollectDependency(MeshFilter component, ComponentDependencyCollector collector)
        {
        }
    }

    [ComponentInformation(typeof(ParticleSystem))]
    internal class ParticleSystemInformation : ComponentInformation<ParticleSystem>
    {
        protected override void CollectDependency(ParticleSystem component, ComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();

            // Some particle system module refers local scale instead of hierarchy / global scale
            // so we need to keep parent transform.
            // TODO: it might be better to check if the particle system is in local space or not.
            // TODO: it might be better to provide API to keep local scale.
            collector.AddDependency(component.transform.parent);

            if (component.main.simulationSpace == ParticleSystemSimulationSpace.Custom) // not animated
                collector.AddDependency(component.main.customSimulationSpace);

            if (collector.GetAnimatedFlag(component, "ShapeModule.enabled", component.shape.enabled) != false)
            {
                switch (component.shape.shapeType) // not animated
                {
                    case ParticleSystemShapeType.MeshRenderer:
                        collector.AddDependency(component.shape.meshRenderer);
                        break;
                    case ParticleSystemShapeType.SkinnedMeshRenderer:
                        collector.AddDependency(component.shape.skinnedMeshRenderer);
                        break;
                    case ParticleSystemShapeType.SpriteRenderer:
                        collector.AddDependency(component.shape.spriteRenderer);
                        break;
#pragma warning disable CS0618
                    case ParticleSystemShapeType.Sphere:
                    case ParticleSystemShapeType.SphereShell:
                    case ParticleSystemShapeType.Hemisphere:
                    case ParticleSystemShapeType.HemisphereShell:
                    case ParticleSystemShapeType.Cone:
                    case ParticleSystemShapeType.Box:
                    case ParticleSystemShapeType.Mesh:
                    case ParticleSystemShapeType.ConeShell:
                    case ParticleSystemShapeType.ConeVolume:
                    case ParticleSystemShapeType.ConeVolumeShell:
                    case ParticleSystemShapeType.Circle:
                    case ParticleSystemShapeType.CircleEdge:
                    case ParticleSystemShapeType.SingleSidedEdge:
                    case ParticleSystemShapeType.BoxShell:
                    case ParticleSystemShapeType.BoxEdge:
                    case ParticleSystemShapeType.Donut:
                    case ParticleSystemShapeType.Rectangle:
                    case ParticleSystemShapeType.Sprite:
                    default:
#pragma warning restore CS0618
                        break;
                }
            }

            if (collector.GetAnimatedFlag(component, "CollisionModule.enabled", component.collision.enabled) != false)
            {
                switch (component.collision.type) // not animated
                {
                    case ParticleSystemCollisionType.Planes:
                        for (var i = 0; i < component.collision.planeCount; i++)
                            collector.AddDependency(component.collision.GetPlane(i));
                        break;
                    case ParticleSystemCollisionType.World:
                    default:
                        break;
                }
            }

            if (collector.GetAnimatedFlag(component, "TriggerModule.enabled", component.trigger.enabled) != false)
            {
                for (var i = 0; i < component.trigger.colliderCount; i++)
                {
                    var collider = component.trigger.GetCollider(i);
                    if (!collider) continue;
                    collector.AddDependency(collider is Collider ? collider : collider.GetComponent<Collider>());
                }
            }

            if (component.subEmitters.enabled) // will not be animated
            {
                for (var i = 0; i < component.subEmitters.subEmittersCount; i++)
                    collector.AddDependency(component.subEmitters.GetSubEmitterSystem(i))
                        .OnlyIfTargetCanBeEnable();
            }

            if (collector.GetAnimatedFlag(component, "LightsModule.enabled", component.lights.enabled) != false)
            {
                collector.AddDependency(component.lights.light);
            }

            collector.AddDependency(component.GetComponent<ParticleSystemRenderer>()).EvenIfDependantDisabled();
        }
    }

    [ComponentInformation(typeof(ParticleSystemRenderer))]
    internal class ParticleSystemRendererInformation : RendererInformation<ParticleSystemRenderer>
    {
        protected override void CollectDependency(ParticleSystemRenderer component,
            ComponentDependencyCollector collector)
        {
            base.CollectDependency(component, collector);
            collector.AddDependency(component.GetComponent<ParticleSystem>()).EvenIfDependantDisabled();
        }
    }

    [ComponentInformation(typeof(TrailRenderer))]
    internal class TrailRendererInformation : RendererInformation<TrailRenderer>
    {
    }

    [ComponentInformation(typeof(LineRenderer))]
    internal class LineRendererInformation : RendererInformation<LineRenderer>
    {
    }

    [ComponentInformation(typeof(Cloth))]
    internal class ClothInformation : ComponentInformation<Cloth>
    {
        protected override void CollectDependency(Cloth component, ComponentDependencyCollector collector)
        {
            // If Cloth is disabled, SMR work as SMR without Cloth
            // If Cloth is enabled and SMR is disabled, SMR draw nothing.
            var skinnedMesh = component.GetComponent<SkinnedMeshRenderer>();
            collector.AddDependency(skinnedMesh, component).EvenIfDependantDisabled();
            foreach (var collider in component.capsuleColliders)
                collector.AddDependency(collider);
            foreach (var collider in component.sphereColliders)
            {
                collector.AddDependency(collider.first);
                collector.AddDependency(collider.second);
            }
        }
    }

    [ComponentInformation(typeof(Collider))]
    [ComponentInformation(typeof(TerrainCollider))]
    [ComponentInformation(typeof(BoxCollider))]
    [ComponentInformation(typeof(SphereCollider))]
    [ComponentInformation(typeof(MeshCollider))]
    [ComponentInformation(typeof(CapsuleCollider))]
    [ComponentInformation(typeof(WheelCollider))]
    internal class ColliderInformation : ComponentInformation<Collider>
    {
        protected override void CollectDependency(Collider component, ComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();
            var rigidbody = component.GetComponentInParent<Rigidbody>();
            if (rigidbody) collector.AddDependency(rigidbody, component).OnlyIfTargetCanBeEnable();
        }
    }

    [ComponentInformation(typeof(Joint))]
    [ComponentInformation(typeof(CharacterJoint))]
    [ComponentInformation(typeof(ConfigurableJoint))]
    [ComponentInformation(typeof(FixedJoint))]
    [ComponentInformation(typeof(HingeJoint))]
    [ComponentInformation(typeof(SpringJoint))]
    internal class JointInformation : ComponentInformation<Joint>
    {
        protected override void CollectDependency(Joint component, ComponentDependencyCollector collector)
        {
            if (component.TryGetComponent<Rigidbody>(out var rigidBody))
            {
                collector.AddDependency(rigidBody, component);
                collector.AddDependency(rigidBody);
            }
            if (component.connectedBody)
            {
                collector.AddDependency(component.connectedBody, component);
                collector.AddDependency(component.connectedBody);
            }
        }
    }

    [ComponentInformation(typeof(Rigidbody))]
    internal class RigidbodyInformation : ComponentInformation<Rigidbody>
    {
        protected override void CollectDependency(Rigidbody component, ComponentDependencyCollector collector)
        {
            collector.AddDependency(component.transform, component).EvenIfDependantDisabled().OnlyIfTargetCanBeEnable();
        }

        protected override void CollectMutations(Rigidbody component, ComponentMutationsCollector collector)
        {
            collector.TransformPositionAndRotation(component.transform);
        }
    }

    [ComponentInformation(typeof(FlareLayer))]
    internal class FlareLayerInformation : ComponentInformation<FlareLayer>
    {
        protected override void CollectDependency(FlareLayer component, ComponentDependencyCollector collector)
        {
            collector.AddDependency(component.GetComponent<Camera>(), component);
        }
    }

    internal class ConstraintInformation<T> : ComponentInformation<T> where T : Component, IConstraint
    {
        protected override void CollectDependency(T component, ComponentDependencyCollector collector)
        {
            collector.AddDependency(component.transform, component)
                .OnlyIfTargetCanBeEnable()
                .EvenIfDependantDisabled();
            for (var i = 0; i < component.sourceCount; i++)
                collector.AddDependency(component.GetSource(i).sourceTransform);
            // https://github.com/anatawa12/AvatarOptimizer/issues/856
            // https://github.com/anatawa12/AvatarOptimizer/pull/996
            // It's too buggy. the Constraint is too complex.
            collector.MarkBehaviour();
        }
    }

    [ComponentInformation(typeof(AimConstraint))]
    internal class AimConstraintInformation : ConstraintInformation<AimConstraint>
    {
        protected override void CollectDependency(AimConstraint component, ComponentDependencyCollector collector)
        {
            base.CollectDependency(component, collector);
            collector.AddDependency(component.worldUpObject);
        }

        protected override void CollectMutations(AimConstraint component, ComponentMutationsCollector collector)
        {
            collector.TransformRotation(component.transform);
        }
    }

    [ComponentInformation(typeof(LookAtConstraint))]
    internal class LookAtConstraintInformation : ConstraintInformation<LookAtConstraint>
    {
        protected override void CollectDependency(LookAtConstraint component, ComponentDependencyCollector collector)
        {
            base.CollectDependency(component, collector);
            collector.AddDependency(component.worldUpObject);
        }

        protected override void CollectMutations(LookAtConstraint component, ComponentMutationsCollector collector)
        {
            collector.TransformRotation(component.transform);
        }
    }

    [ComponentInformation(typeof(ParentConstraint))]
    internal class ParentConstraintInformation : ConstraintInformation<ParentConstraint>
    {
        protected override void CollectMutations(ParentConstraint component, ComponentMutationsCollector collector)
        {
            collector.TransformPositionAndRotation(component.transform);
        }
    }

    [ComponentInformation(typeof(RotationConstraint))]
    internal class RotationConstraintInformation : ConstraintInformation<RotationConstraint>
    {
        protected override void CollectMutations(RotationConstraint component, ComponentMutationsCollector collector)
        {
            collector.TransformRotation(component.transform);
        }
    }

    [ComponentInformation(typeof(PositionConstraint))]
    internal class PositionConstraintInformation : ConstraintInformation<PositionConstraint>
    {
        protected override void CollectMutations(PositionConstraint component, ComponentMutationsCollector collector)
        {
            collector.TransformPosition(component.transform);
        }
    }

    [ComponentInformation(typeof(ScaleConstraint))]
    internal class ScaleConstraintInformation : ConstraintInformation<ScaleConstraint>
    {
        protected override void CollectMutations(ScaleConstraint component, ComponentMutationsCollector collector)
        {
            collector.TransformScale(component.transform);
        }
    }

    [ComponentInformation(typeof(RemoveMeshByBlendShape))]
    internal class RemoveMeshByBlendShapeInformation : ComponentInformation<RemoveMeshByBlendShape>
    {
        protected override void CollectDependency(RemoveMeshByBlendShape component, ComponentDependencyCollector collector)
        {
        }

        protected override void CollectMutations(RemoveMeshByBlendShape component, ComponentMutationsCollector collector)
        {
            var blendShapes = component.RemovingShapeKeys;
            {
                collector.ModifyProperties(component.GetComponent<SkinnedMeshRenderer>(),
                    blendShapes.Select(blendShape => $"blendShape.{blendShape}"));
            }

            DeriveMergeSkinnedMeshProperties(component.GetComponent<MergeSkinnedMesh>());

            void DeriveMergeSkinnedMeshProperties(MergeSkinnedMesh mergeSkinnedMesh)
            {
                if (mergeSkinnedMesh == null) return;

                foreach (var renderer in mergeSkinnedMesh.renderersSet.GetAsSet())
                {
                    collector.ModifyProperties(renderer, blendShapes.Select(blendShape => $"blendShape.{blendShape}"));

                    DeriveMergeSkinnedMeshProperties(renderer.GetComponent<MergeSkinnedMesh>());
                }
            }
        }
    }
    
    [ComponentInformation(typeof(RemoveZeroSizedPolygon))]
    internal class RemoveZeroSizedPolygonInformation : ComponentInformation<RemoveZeroSizedPolygon>
    {
        protected override void CollectDependency(RemoveZeroSizedPolygon component, ComponentDependencyCollector collector)
        {
            collector.AddDependency(component.GetComponent<SkinnedMeshRenderer>(), component);
        }

        protected override void CollectMutations(RemoveZeroSizedPolygon component, ComponentMutationsCollector collector)
        {
        }
    }

    [ComponentInformation(typeof(MergeBone))]
    internal class MergeBoneInformation : ComponentInformation<MergeBone>
    {
        protected override void CollectDependency(MergeBone component, ComponentDependencyCollector collector)
        {
            collector.AddDependency(component.transform, component)
                .EvenIfDependantDisabled();
        }

        protected override void CollectMutations(MergeBone component, ComponentMutationsCollector collector)
        {
        }
    }

    internal static class ComponentInformationExtensions
    {
        public static void TransformPositionAndRotation(this ComponentMutationsCollector collector,
            Transform transform) =>
            collector.ModifyProperties(transform,
                TransformPositionAnimationKeys.Concat(TransformRotationAnimationKeys));

        public static void TransformRotation(this ComponentMutationsCollector collector, Transform transform) =>
            collector.ModifyProperties(transform, TransformRotationAnimationKeys);

        public static void TransformPosition(this ComponentMutationsCollector collector, Transform transform) =>
            collector.ModifyProperties(transform, TransformPositionAnimationKeys);

        public static void TransformScale(this ComponentMutationsCollector collector, Transform transform) =>
            collector.ModifyProperties(transform, TransformScaleAnimationKeys);

        private static readonly string[] TransformRotationAnimationKeys =
            { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };

        private static readonly string[] TransformPositionAnimationKeys =
            { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z" };

        private static readonly string[] TransformScaleAnimationKeys =
            { "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" };
    }
}
