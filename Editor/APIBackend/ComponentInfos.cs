using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.API;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;
using VRC.Core;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase;

namespace Anatawa12.AvatarOptimizer.APIBackend
{
    [ComponentInformation(typeof(Light))]
    [ComponentInformation(typeof(Camera))]
    [ComponentInformation(typeof(Animation))]
    [ComponentInformation(typeof(MergeBone))]
    [ComponentInformation(typeof(AudioSource))]
#pragma warning disable CS0618
    [ComponentInformation(typeof(PipelineSaver))]
#pragma warning restore CS0618
    [ComponentInformation(typeof(PipelineManager))]
    [ComponentInformation(typeof(VRCSpatialAudioSource))]
    [ComponentInformation(typeof(VRC_SpatialAudioSource))]
    [ComponentInformation(typeof(VRC.SDKBase.VRCStation))]
    [ComponentInformation(typeof(VRC.SDK3.Avatars.Components.VRCStation))]
    [ComponentInformation(typeof(nadena.dev.ndmf.runtime.AvatarActivator))]
    internal class EntrypointComponentInformation : ComponentInformation<Component>
    {
        protected override void CollectDependency(Component component, IComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();
        }
    }

    [ComponentInformation(typeof(Transform))]
    internal class TransformInformation : ComponentInformation<Transform>
    {
        protected override void CollectDependency(Transform component, IComponentDependencyCollector collector)
        {
            var casted = (Processors.TraceAndOptimizes.ComponentDependencyCollector.Collector)collector;
            casted.AddParentDependency(component);
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
        protected override void CollectDependency(Animator component, IComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();

            for (var bone = HumanBodyBones.Hips; bone < HumanBodyBones.LastBone; bone++)
            {
                var boneTransform = component.GetBoneTransform(bone);
                foreach (var transform in boneTransform.ParentEnumerable())
                {
                    if (transform == component.transform) break;
                    collector.AddDependency(transform);
                }
            }
        }
    }

    internal class RendererInformation<T> : ComponentInformation<T> where T : Renderer
    {
        protected override void CollectDependency(T component, IComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();
            // anchor proves
            if (component.reflectionProbeUsage != ReflectionProbeUsage.Off ||
                component.lightProbeUsage != LightProbeUsage.Off)
                collector.AddDependency(component.probeAnchor);
            if (component.lightProbeUsage == LightProbeUsage.UseProxyVolume)
                collector.AddDependency(component.lightProbeProxyVolumeOverride.transform);
        }
    }

    [ComponentInformation(typeof(SkinnedMeshRenderer))]
    internal class SkinnedMeshRendererInformation : RendererInformation<SkinnedMeshRenderer>
    {
        protected override void CollectDependency(SkinnedMeshRenderer component,
            IComponentDependencyCollector collector)
        {
            base.CollectDependency(component, collector);

            var casted = (Processors.TraceAndOptimizes.ComponentDependencyCollector.Collector)collector;

            var meshInfo2 = casted.MeshInfo2Holder.GetMeshInfoFor(component);
            foreach (var bone in meshInfo2.Bones)
                collector.AddDependency(bone.Transform).AsBone();
            collector.AddDependency(meshInfo2.RootBone);
        }
    }

    [ComponentInformation(typeof(MeshRenderer))]
    internal class MeshRendererInformation : RendererInformation<MeshRenderer>
    {
        protected override void CollectDependency(MeshRenderer component, IComponentDependencyCollector collector)
        {
            base.CollectDependency(component, collector);
            collector.AddDependency(component.GetComponent<MeshFilter>());
        }
    }

    [ComponentInformation(typeof(MeshFilter))]
    internal class MeshFilterInformation : ComponentInformation<MeshFilter>
    {
        protected override void CollectDependency(MeshFilter component, IComponentDependencyCollector collector)
        {
        }
    }

    [ComponentInformation(typeof(ParticleSystem))]
    internal class ParticleSystemInformation : ComponentInformation<ParticleSystem>
    {
        protected override void CollectDependency(ParticleSystem component, IComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();

            if (component.main.simulationSpace == ParticleSystemSimulationSpace.Custom)
                collector.AddDependency(component.main.customSimulationSpace);
            if (component.shape.enabled)
            {
                switch (component.shape.shapeType)
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

            if (component.collision.enabled)
            {
                switch (component.collision.type)
                {
                    case ParticleSystemCollisionType.Planes:
                        for (var i = 0; i < component.collision.maxPlaneCount; i++)
                            collector.AddDependency(component.collision.GetPlane(i));
                        break;
                    case ParticleSystemCollisionType.World:
                    default:
                        break;
                }
            }

            if (component.trigger.enabled)
            {
                for (var i = 0; i < component.trigger.maxColliderCount; i++)
                    collector.AddDependency(component.trigger.GetCollider(i));
            }

            if (component.subEmitters.enabled)
            {
                for (var i = 0; i < component.subEmitters.subEmittersCount; i++)
                    collector.AddDependency(component.subEmitters.GetSubEmitterSystem(i));
            }

            if (component.lights.enabled)
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
            IComponentDependencyCollector collector)
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
        protected override void CollectDependency(Cloth component, IComponentDependencyCollector collector)
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
        protected override void CollectDependency(Collider component, IComponentDependencyCollector collector)
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
        protected override void CollectDependency(Joint component, IComponentDependencyCollector collector)
        {
            collector.AddDependency(component.GetComponent<Rigidbody>(), component);
            collector.AddDependency(component.connectedBody);
        }
    }

    [ComponentInformation(typeof(Rigidbody))]
    internal class RigidbodyInformation : ComponentInformation<Rigidbody>
    {
        protected override void CollectDependency(Rigidbody component, IComponentDependencyCollector collector)
        {
            collector.AddDependency(component.transform, component).EvenIfDependantDisabled().OnlyIfTargetCanBeEnable();
        }

        protected override void CollectMutations(Rigidbody component, IComponentMutationsCollector collector)
        {
            collector.TransformPositionAndRotation(component.transform);
        }
    }

    [ComponentInformation(typeof(FlareLayer))]
    internal class FlareLayerInformation : ComponentInformation<FlareLayer>
    {
        protected override void CollectDependency(FlareLayer component, IComponentDependencyCollector collector)
        {
            collector.AddDependency(component.GetComponent<Camera>(), component);
        }
    }

    internal class ConstraintInformation<T> : ComponentInformation<T> where T : Component, IConstraint
    {
        protected override void CollectDependency(T component, IComponentDependencyCollector collector)
        {
            collector.AddDependency(component.transform, component).EvenIfDependantDisabled();
            for (var i = 0; i < component.sourceCount; i++)
                collector.AddDependency(component.GetSource(i).sourceTransform);
        }
    }

    [ComponentInformation(typeof(AimConstraint))]
    internal class AimConstraintInformation : ConstraintInformation<AimConstraint>
    {
        protected override void CollectDependency(AimConstraint component, IComponentDependencyCollector collector)
        {
            base.CollectDependency(component, collector);
            collector.AddDependency(component.worldUpObject);
        }

        protected override void CollectMutations(AimConstraint component, IComponentMutationsCollector collector)
        {
            collector.TransformPositionAndRotation(component.transform);
        }
    }

    [ComponentInformation(typeof(LookAtConstraint))]
    internal class LookAtConstraintInformation : ConstraintInformation<LookAtConstraint>
    {
        protected override void CollectDependency(LookAtConstraint component, IComponentDependencyCollector collector)
        {
            base.CollectDependency(component, collector);
            collector.AddDependency(component.worldUpObject);
        }

        protected override void CollectMutations(LookAtConstraint component, IComponentMutationsCollector collector)
        {
            collector.TransformPositionAndRotation(component.transform);
        }
    }

    [ComponentInformation(typeof(ParentConstraint))]
    internal class ParentConstraintInformation : ConstraintInformation<ParentConstraint>
    {
        protected override void CollectMutations(ParentConstraint component, IComponentMutationsCollector collector)
        {
            collector.TransformPositionAndRotation(component.transform);
        }
    }

    [ComponentInformation(typeof(RotationConstraint))]
    internal class RotationConstraintInformation : ConstraintInformation<RotationConstraint>
    {
        protected override void CollectMutations(RotationConstraint component, IComponentMutationsCollector collector)
        {
            collector.TransformRotation(component.transform);
        }
    }

    [ComponentInformation(typeof(PositionConstraint))]
    internal class PositionConstraintInformation : ConstraintInformation<PositionConstraint>
    {
        protected override void CollectMutations(PositionConstraint component, IComponentMutationsCollector collector)
        {
            collector.TransformPosition(component.transform);
        }
    }

    [ComponentInformation(typeof(ScaleConstraint))]
    internal class ScaleConstraintInformation : ConstraintInformation<ScaleConstraint>
    {
        protected override void CollectMutations(ScaleConstraint component, IComponentMutationsCollector collector)
        {
            collector.TransformScale(component.transform);
        }
    }

    [ComponentInformation(typeof(VRC_AvatarDescriptor))]
    [ComponentInformation(typeof(VRCAvatarDescriptor))]
    internal class VRCAvatarDescriptorInformation : ComponentInformation<VRC_AvatarDescriptor>
    {
        protected override void CollectDependency(VRC_AvatarDescriptor component,
            IComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();
            collector.AddDependency(component.GetComponent<PipelineManager>()).EvenIfDependantDisabled();
        }
    }

    [ComponentInformation(typeof(VRCPhysBoneBase))]
    [ComponentInformation(typeof(VRCPhysBone))]
    internal class VRCPhysBoneInformation : ComponentInformation<VRCPhysBoneBase>
    {
        protected override void CollectDependency(VRCPhysBoneBase component, IComponentDependencyCollector collector)
        {
            // first, Transform <=> PhysBone
            // Transform is used even if the bone is inactive so Transform => PB is always dependency
            // PhysBone works only if enabled so PB => Transform is active dependency
            var ignoreTransforms = new HashSet<Transform>(component.ignoreTransforms);
            CollectTransforms(component.GetTarget());

            void CollectTransforms(Transform bone)
            {
                collector.AddDependency(bone, component).EvenIfDependantDisabled().OnlyIfTargetCanBeEnable();
                collector.AddDependency(bone);
                foreach (var child in bone.DirectChildrenEnumerable())
                {
                    if (!ignoreTransforms.Contains(child))
                        CollectTransforms(child);
                }
            }

            // then, PB => Collider
            // in PB, PB Colliders work only if Colliders are enabled
            foreach (var physBoneCollider in component.colliders)
                collector.AddDependency(physBoneCollider).OnlyIfTargetCanBeEnable();

            // If parameter is not empty, the PB can be required for Animator Parameter so it's Entrypoint Component
            // https://github.com/anatawa12/AvatarOptimizer/issues/450
            if (!string.IsNullOrEmpty(component.parameter))
                collector.MarkEntrypoint();
        }

        protected override void CollectMutations(VRCPhysBoneBase component, IComponentMutationsCollector collector)
        {
            foreach (var transform in component.GetAffectedTransforms())
                collector.TransformPositionAndRotation(transform);
        }
    }

    [ComponentInformation(typeof(VRCPhysBoneColliderBase))]
    [ComponentInformation(typeof(VRCPhysBoneCollider))]
    internal class VRCPhysBoneColliderInformation : ComponentInformation<VRCPhysBoneColliderBase>
    {
        protected override void CollectDependency(VRCPhysBoneColliderBase component,
            IComponentDependencyCollector collector)
        {
            collector.AddDependency(component.rootTransform);
        }
    }

    [ComponentInformation(typeof(ContactBase))]
    [ComponentInformation(typeof(ContactReceiver))]
    [ComponentInformation(typeof(VRCContactReceiver))]
    [ComponentInformation(typeof(ContactSender))]
    [ComponentInformation(typeof(VRCContactSender))]
    internal class ContactBaseInformation : ComponentInformation<ContactBase>
    {
        protected override void CollectDependency(ContactBase component, IComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();
            collector.AddDependency(component.rootTransform);
        }
    }

    [ComponentInformation(typeof(RemoveMeshByBlendShape))]
    internal class RemoveMeshByBlendShapeInformation : ComponentInformation<RemoveMeshByBlendShape>
    {
        protected override void CollectDependency(RemoveMeshByBlendShape component, IComponentDependencyCollector collector)
        {
        }

        protected override void CollectMutations(RemoveMeshByBlendShape component, IComponentMutationsCollector collector)
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

    internal class DynamicBoneInformation : ComponentInformation<Component>
    {
        protected override void CollectDependency(Component component, IComponentDependencyCollector collector)
        {
            DynamicBone.TryCast(component, out var dynamicBone);
            foreach (var transform in dynamicBone.GetAffectedTransforms())
            {
                collector.AddDependency(transform, component)
                    .EvenIfDependantDisabled()
                    .OnlyIfTargetCanBeEnable();
                collector.AddDependency(transform);
            }

            foreach (var collider in dynamicBone.Colliders)
            {
                // DynamicBone ignores enabled/disabled of Collider Component AFAIK
                collector.AddDependency(collider);
            }
        }

        protected override void CollectMutations(Component component, IComponentMutationsCollector collector)
        {
            // DynamicBone : similar to PhysBone
            DynamicBone.TryCast(component, out var dynamicBone);
            foreach (var transform in dynamicBone.GetAffectedTransforms())
                collector.TransformRotation(transform);
        }
    }

    internal class DynamicBoneColliderInformation : ComponentInformation<Component>
    {
        protected override void CollectDependency(Component component, IComponentDependencyCollector collector)
        {
        }
    }

    internal static class ComponentInformationExtensions
    {
        public static void TransformPositionAndRotation(this IComponentMutationsCollector collector,
            Transform transform) =>
            collector.ModifyProperties(transform,
                TransformPositionAnimationKeys.Concat(TransformRotationAnimationKeys));

        public static void TransformRotation(this IComponentMutationsCollector collector, Transform transform) =>
            collector.ModifyProperties(transform, TransformRotationAnimationKeys);

        public static void TransformPosition(this IComponentMutationsCollector collector, Transform transform) =>
            collector.ModifyProperties(transform, TransformPositionAnimationKeys);

        public static void TransformScale(this IComponentMutationsCollector collector, Transform transform) =>
            collector.ModifyProperties(transform, TransformScaleAnimationKeys);

        private static readonly string[] TransformRotationAnimationKeys =
            { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };

        private static readonly string[] TransformPositionAnimationKeys =
            { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z" };

        private static readonly string[] TransformScaleAnimationKeys =
            { "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" };
    }
}
