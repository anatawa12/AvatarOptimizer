#if AAO_VRCSDK3_AVATARS_PARAMETER_BIT_WIDTH
using System.Collections.Generic;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;

namespace Anatawa12.AvatarOptimizer.Test.E2E
{
    public class ReduceParameterBitWidthTest
    {
        [Test]
        public void ReduceBitWidthByUsage()
        {
            var avatar = TestUtils.NewAvatar();

            var parameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            parameters.parameters = new[]
            {
                IntParameter("MenuToggle"),
                IntParameter("DriverSet"),
                IntParameter("DriverRandom"),
                IntParameter("DriverCopyRange"),
                IntParameter("DriverCopyBool"),
                IntParameter("DriverCopyFloat"),
                IntParameter("DriverAdd"),
                IntParameter("ConditionOnly"),
                IntParameter("Explicit", bitWidth: 4),
                IntParameter("Signed", signed: true),
                IntParameter("NotSynced", synced: false),
                IntParameter("OutOfRange"),
                IntParameter("PhysBone_Angle"),
                IntParameter("GestureLeft"),
                new()
                {
                    name = "FloatParam",
                    valueType = VRCExpressionParameters.ValueType.Float,
                    networkSynced = true,
                },
            };

            var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            var subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.controls.Add(new VRCExpressionsMenu.Control
            {
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = "MenuToggle" },
                value = 3,
            });
            menu.controls.Add(new VRCExpressionsMenu.Control
            {
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = subMenu,
            });
            subMenu.controls.Add(new VRCExpressionsMenu.Control
            {
                type = VRCExpressionsMenu.Control.ControlType.Button,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = "MenuToggle" },
                value = 5,
            });
            subMenu.controls.Add(new VRCExpressionsMenu.Control
            {
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = "Explicit" },
                value = 1,
            });
            subMenu.controls.Add(new VRCExpressionsMenu.Control
            {
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = "GestureLeft" },
                value = 1,
            });
            subMenu.controls.Add(new VRCExpressionsMenu.Control
            {
                type = VRCExpressionsMenu.Control.ControlType.RadialPuppet,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = "PhysBone_Angle" },
                value = 1,
                subParameters = new[] { new VRCExpressionsMenu.Control.Parameter { name = "FloatParam" } },
            });

            var controller = new AnimatorController { name = "FX" };
            foreach (var parameter in parameters.parameters)
            {
                controller.AddParameter(parameter.name, parameter.valueType == VRCExpressionParameters.ValueType.Float
                    ? AnimatorControllerParameterType.Float
                    : AnimatorControllerParameterType.Int);
            }
            controller.AddParameter("BoolParam", AnimatorControllerParameterType.Bool);
            controller.AddLayer("Layer");
            var stateMachine = controller.layers[0].stateMachine;
            var stateA = stateMachine.AddState("A");
            var stateB = stateMachine.AddState("B");
            var transition = stateA.AddTransition(stateB);
            transition.AddCondition(AnimatorConditionMode.Equals, 100, "ConditionOnly");
            transition.AddCondition(AnimatorConditionMode.Greater, 2, "DriverSet");
            transition.AddCondition(AnimatorConditionMode.Less, 1, "Signed");

            var driver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
            driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>
            {
                new() { type = VRC_AvatarParameterDriver.ChangeType.Set, name = "DriverSet", value = 20 },
                new() { type = VRC_AvatarParameterDriver.ChangeType.Random, name = "DriverRandom", valueMin = 0, valueMax = 40 },
                new()
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Copy, name = "DriverCopyRange", source = "FloatParam",
                    convertRange = true, sourceMin = 0, sourceMax = 1, destMin = 0, destMax = 10,
                },
                new() { type = VRC_AvatarParameterDriver.ChangeType.Copy, name = "DriverCopyBool", source = "BoolParam" },
                new() { type = VRC_AvatarParameterDriver.ChangeType.Copy, name = "DriverCopyFloat", source = "FloatParam" },
                new() { type = VRC_AvatarParameterDriver.ChangeType.Add, name = "DriverAdd", value = 1 },
                new() { type = VRC_AvatarParameterDriver.ChangeType.Set, name = "Signed", value = -3 },
                new() { type = VRC_AvatarParameterDriver.ChangeType.Set, name = "Signed", value = 2 },
                new() { type = VRC_AvatarParameterDriver.ChangeType.Set, name = "NotSynced", value = 1 },
                new() { type = VRC_AvatarParameterDriver.ChangeType.Set, name = "OutOfRange", value = 300 },
            };
            stateA.behaviours = new StateMachineBehaviour[] { driver };

            TestUtils.SetFxLayer(avatar, controller);
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            descriptor.customExpressions = true;
            descriptor.expressionParameters = parameters;
            descriptor.expressionsMenu = menu;

            var physBoneRoot = avatar.AddChild("PhysBone");
            physBoneRoot.AddChild("Bone1").AddChild("Bone2");
            var physBone = physBoneRoot.AddComponent<VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone>();
            physBone.parameter = "PhysBone";

            var traceAndOptimize = avatar.AddComponent<TraceAndOptimize>();
            traceAndOptimize.reduceParameterBitWidth = true;

            AvatarProcessor.ProcessAvatar(avatar);

            var result = descriptor.expressionParameters;
            Assert.That(result, Is.Not.SameAs(parameters), "Expression Parameters should be cloned before modification");
            Assert.That(parameters.FindParameter("MenuToggle").bitWidth, Is.EqualTo(8), "The original asset should not be modified");

            Assert.That(BitWidth(result, "MenuToggle"), Is.EqualTo(3), "values 0, 3, 5");
            Assert.That(BitWidth(result, "DriverSet"), Is.EqualTo(5), "values 0, 3, 20");
            Assert.That(BitWidth(result, "DriverRandom"), Is.EqualTo(6), "values 0..40");
            Assert.That(BitWidth(result, "DriverCopyRange"), Is.EqualTo(4), "values 0..10");
            Assert.That(BitWidth(result, "DriverCopyBool"), Is.EqualTo(2), "values 0, 1");
            Assert.That(BitWidth(result, "DriverCopyFloat"), Is.EqualTo(8), "unknown value");
            Assert.That(BitWidth(result, "DriverAdd"), Is.EqualTo(8), "unknown value");
            Assert.That(BitWidth(result, "ConditionOnly"), Is.EqualTo(8), "no writer in the avatar");
            Assert.That(BitWidth(result, "Explicit"), Is.EqualTo(4), "explicitly configured");
            Assert.That(BitWidth(result, "Signed"), Is.EqualTo(3), "values -3..2");
            Assert.That(BitWidth(result, "NotSynced"), Is.EqualTo(8), "not synced");
            Assert.That(BitWidth(result, "OutOfRange"), Is.EqualTo(8), "does not fit in the representation");
            Assert.That(BitWidth(result, "PhysBone_Angle"), Is.EqualTo(8), "written by PhysBone");
            Assert.That(BitWidth(result, "GestureLeft"), Is.EqualTo(8), "written by VRChat");
            Assert.That(BitWidth(result, "FloatParam"), Is.EqualTo(8), "float parameter");
        }

        [Test]
        public void EdgeCases()
        {
            var avatar = TestUtils.NewAvatar();

            var parameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            parameters.parameters = new[]
            {
                IntParameter("ReversedCopy", signed: true),
                IntParameter("FractionalSet"),
                IntParameter("ConditionBelowZero"),
                IntParameter("UserFaceTracking"),
                IntParameter("Duplicated"),
                IntParameter("Duplicated", bitWidth: 4),
                IntParameter(null),
                new()
                {
                    name = "FloatParam",
                    valueType = VRCExpressionParameters.ValueType.Float,
                    networkSynced = true,
                },
            };

            var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            foreach (var name in new[] { "ConditionBelowZero", "UserFaceTracking", "Duplicated" })
            {
                menu.controls.Add(new VRCExpressionsMenu.Control
                {
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = name },
                    value = 1,
                });
            }

            var controller = new AnimatorController { name = "FX" };
            controller.AddParameter("ReversedCopy", AnimatorControllerParameterType.Int);
            controller.AddParameter("FractionalSet", AnimatorControllerParameterType.Int);
            controller.AddParameter("ConditionBelowZero", AnimatorControllerParameterType.Int);
            controller.AddParameter("FloatParam", AnimatorControllerParameterType.Float);
            controller.AddLayer("Layer");
            var stateMachine = controller.layers[0].stateMachine;
            var stateA = stateMachine.AddState("A");
            var stateB = stateMachine.AddState("B");
            stateA.AddTransition(stateB).AddCondition(AnimatorConditionMode.Less, 0, "ConditionBelowZero");

            var driver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
            driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>
            {
                new()
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Copy, name = "ReversedCopy", source = "FloatParam",
                    convertRange = true, sourceMin = 0, sourceMax = 1, destMin = -1, destMax = -4.1f,
                },
                new() { type = VRC_AvatarParameterDriver.ChangeType.Set, name = "FractionalSet", value = 3.5f },
            };
            stateA.behaviours = new StateMachineBehaviour[] { driver };

            TestUtils.SetFxLayer(avatar, controller);
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            descriptor.customExpressions = true;
            descriptor.expressionParameters = parameters;
            descriptor.expressionsMenu = menu;

            var traceAndOptimize = avatar.AddComponent<TraceAndOptimize>();
            traceAndOptimize.reduceParameterBitWidth = true;

            AvatarProcessor.ProcessAvatar(avatar);

            var result = descriptor.expressionParameters;
            Assert.That(BitWidth(result, "ReversedCopy"), Is.EqualTo(4), "values -5..0");
            Assert.That(BitWidth(result, "FractionalSet"), Is.EqualTo(3), "values 0, 3, 4");
            Assert.That(BitWidth(result, "ConditionBelowZero"), Is.EqualTo(2), "values 0, 1");
            Assert.That(BitWidth(result, "UserFaceTracking"), Is.EqualTo(8), "written by VRChat");
            Assert.That(result.parameters[4].GetBitWidthByType(), Is.EqualTo(8), "duplicated name");
            Assert.That(result.parameters[5].GetBitWidthByType(), Is.EqualTo(4), "duplicated name");
        }

        [Test]
        public void DisabledByDefault()
        {
            var avatar = TestUtils.NewAvatar();

            var parameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            parameters.parameters = new[] { IntParameter("MenuToggle") };

            var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.controls.Add(new VRCExpressionsMenu.Control
            {
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = "MenuToggle" },
                value = 1,
            });

            TestUtils.SetFxLayer(avatar, new AnimatorController { name = "FX" });
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            descriptor.customExpressions = true;
            descriptor.expressionParameters = parameters;
            descriptor.expressionsMenu = menu;
            avatar.AddComponent<TraceAndOptimize>();

            AvatarProcessor.ProcessAvatar(avatar);

            Assert.That(BitWidth(descriptor.expressionParameters, "MenuToggle"), Is.EqualTo(8));
        }

        private static VRCExpressionParameters.Parameter IntParameter(string name, int bitWidth = 8, bool signed = false,
            bool synced = true) => new()
        {
            name = name,
            valueType = VRCExpressionParameters.ValueType.Int,
            networkSynced = synced,
            bitWidth = (byte)bitWidth,
            flippedRepresentation = signed,
        };

        private static int BitWidth(VRCExpressionParameters parameters, string name) =>
            parameters.FindParameter(name).GetBitWidthByType();
    }
}
#endif
