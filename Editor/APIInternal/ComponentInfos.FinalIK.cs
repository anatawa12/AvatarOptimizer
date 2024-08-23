using Anatawa12.AvatarOptimizer.API;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIInternal.Externals
{
    // Currently implements ComponentInfo with low information so I assume all references are dependency and 
    // All transforms will be rotated.

    [ComponentInformationWithGUID("36737232c1e1646399fa0c8cbc087280", 11500000)] // "IKExecutionOrder"
    [ComponentInformationWithGUID("bfac4fa329d1f4f23814af71a247c14b", 11500000)] // "VRIK",
    [ComponentInformationWithGUID("a70e525c82ce9413fa4d940ad7fcf1db", 11500000)] // "FullBodyBipedIK"
    [ComponentInformationWithGUID("4db3c450680fd4c809d5ad90a2f24e5f", 11500000)] // "LimbIK"
    [ComponentInformationWithGUID("5013856973b27429d937d256dc082f2e", 11500000)] // "AimIK"
    [ComponentInformationWithGUID("6d406d8b892f54ccaa5b0ef4de59944a", 11500000)] // "BipedIK"
    [ComponentInformationWithGUID("65ace204533ef4c24ac80f11ef8ee8ea", 11500000)] // "GrounderIK"
    [ComponentInformationWithGUID("6c72e1df647af4c0098866e944a04b01", 11500000)] // "GrounderFBBIK"
    [ComponentInformationWithGUID("b7bc22c0304fa3d4086a2e8e451894ef", 11500000)] // "GrounderVRIK"
    [ComponentInformationWithGUID("9823e47edf1dd40c29dfe0ba019f33a6", 11500000)] // "GrounderQuadruped"
    [ComponentInformationWithGUID("e561d2b0d3d2241fd9bc4929a8f64b7f", 11500000)] // "TwistRelaxer"
    [ComponentInformationWithGUID("a48af8ef2fd147446af3c65186a1cd9e", 11500000)] // "ShoulderRotator"
    [ComponentInformationWithGUID("e6c4fa4d3ae33fb44b29d48945f7a129", 11500000)] // "FBBIKArmBending"
    [ComponentInformationWithGUID("ebbd066464934494f896947690872ad4", 11500000)] // "FBBIKHeadEffector"
    [ComponentInformationWithGUID("52af154b35b9e48af96507346dc649ba", 11500000)] // "FABRIK"
    [ComponentInformationWithGUID("52af154b35b9e48af96507346dc649ba", 11500000)] // "FABRIK"
    [ComponentInformationWithGUID("6fb82f19cc3ce412892b525300de1141", 11500000)] // "FABRIKRoot"
    [ComponentInformationWithGUID("98b9a1a9e9a934b23a7db351dd9ec69e", 11500000)] // "CCDIK"
    [ComponentInformationWithGUID("197a3a7b95f0e4ac48f171363db95b5b", 11500000)] // "RotationLimit"
    [ComponentInformationWithGUID("484718f2c4ab849829491782b508958a", 11500000)] // "RotationLimitHinge"
    [ComponentInformationWithGUID("dae00b1bdc58d499396776ce508a5078", 11500000)] // "RotationLimitPolygonal"
    [ComponentInformationWithGUID("2ccb80eac3b2b4909a63c818e38ae6b8", 11500000)] // "RotationLimitSpline"

    // not listed but allowed with inheritance
    [ComponentInformationWithGUID("45281828b4c9247558c7c695124d6877", 11500000)] // "RotationLimitAngle"
    internal class FinalIKInformation : ComponentInformation<Component>, IExternalMarker
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();
            collector.MarkBehaviour();
            using (var serialized = new SerializedObject(component))
                foreach (var property in serialized.ObjectReferenceProperties())
                    if (property.objectReferenceValue is Component c)
                        collector.AddDependency(c).EvenIfDependantDisabled();
        }

        protected override void CollectMutations(Component component, ComponentMutationsCollector collector)
        {
            using (var serialized = new SerializedObject(component))
                foreach (var property in serialized.ObjectReferenceProperties())
                    if (property.objectReferenceValue is Transform t)
                        collector.TransformRotation(t);
        }
    }
}
