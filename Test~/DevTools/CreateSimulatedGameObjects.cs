using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AvatarOptimizer.DevTools
{
    public class CreateSimulatedGameObjects : EditorWindow
    {
        [MenuItem("Tools/Avatar Optimizer/DevTools/CreateSimulatedGameObjects")]
        private static void OpenWindow() => GetWindow<CreateSimulatedGameObjects>();

        public string filePath;

        private void OnGUI()
        {
            filePath = DevtoolGUILayout.FilePath("AvatarInfo", filePath);
            if (GUILayout.Button("Create"))
            {
                try
                {
                    DoCreate(File.ReadAllText(filePath));
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Error", e.Message, "OK");
                    Debug.LogException(e);
                }
            }
            if (GUILayout.Button("Create From Clipboard"))
            {
                try
                {
                    DoCreate(EditorGUIUtility.systemCopyBuffer);
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Error", e.Message, "OK");
                    Debug.LogException(e);
                }
            }
        }

        private static void DoCreate(string treeTxt)
        {
            var tree = TreeTxtNode.Parse(treeTxt);

            var rootGameObject = new GameObject("AvatarInfoGenerated");
            var stack = new List<(string path, GameObject go)>();

            var delayedWork = new List<Action>();
            void Delayed(Action work) => delayedWork.Add(work);

            GameObject ResolveGameObject(string name)
            {
                if (name.StartsWith("avatar:")) return rootGameObject.transform.Find(name).gameObject;
                return null;
            }

            foreach (var gameObjectNode in tree.Children)
            {
                string path;
                GameObject gameObject;
                if (stack.Count == 0 && gameObjectNode.Text == "<Root>")
                {
                    path = "";
                    gameObject = rootGameObject;
                }
                else
                {
                    var parentIndex = stack.FindLastIndex(e => gameObjectNode.Text.StartsWith(e.path));
                    if (parentIndex == -1)
                        throw new Exception($"No parent found for '{gameObjectNode.Text}'");
                    var parent = stack[parentIndex];
                    stack.RemoveRange(parentIndex + 1, stack.Count - (parentIndex + 1));
                    path = gameObjectNode.Text + '/';
                    gameObject = new GameObject(gameObjectNode.Text[parent.path.Length..]);
                    gameObject.transform.SetParent(parent.go.transform);
                }
                stack.Add((path, gameObject));

                foreach (var componentNode in gameObjectNode.Children)
                {
                    switch (componentNode.Text)
                    {
                        case "UnityEngine.Transform":
                        {
                            foreach (var propertyNode in componentNode.Children)
                            {
                                switch (propertyNode.Key)
                                {
                                    case "activeSelf": gameObject.SetActive(bool.Parse(propertyNode.Value)); break;
                                    case "position": gameObject.transform.position = ParseVector3(propertyNode.Value); break;
                                    case "rotation": gameObject.transform.rotation = ParseQuaternion(propertyNode.Value); break;
                                    case "scale": gameObject.transform.localScale = ParseVector3(propertyNode.Value); break;
                                }
                            }
                            break;
                        }
#if AAO_VRCSDK3_AVATARS
                        case "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone":
                        {
                            var pb = gameObject.AddComponent<VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone>();
                            foreach (var propertyNode in componentNode.Children)
                            {
                                switch (propertyNode.Key)
                                {
                                    case "enabled": pb.enabled = bool.Parse(propertyNode.Value); break;
                                    case "version": pb.version = Enum.Parse<VRC.Dynamics.VRCPhysBoneBase.Version>(propertyNode.Value); break;
                                    case "integrationType": pb.integrationType = Enum.Parse<VRC.Dynamics.VRCPhysBoneBase.IntegrationType>(propertyNode.Value); break;
                                    case "rootTransform": Delayed(() => pb.rootTransform = ResolveGameObject(propertyNode.Value).transform); break;
                                    case var prop when prop.StartsWith("ignoreTransform[") && prop.EndsWith(']'):
                                    {
                                        var index = int.Parse(prop["ignoreTransform[".Length..^1]);
                                        while (pb.ignoreTransforms.Count < index)pb.ignoreTransforms.Add(null);
                                        Delayed(() => pb.ignoreTransforms[index] = ResolveGameObject(propertyNode.Value).transform);
                                        break;
                                    }
                                    case "endpointPosition": pb.endpointPosition = ParseVector3(propertyNode.Value); break;
                                    case "multiChildType": pb.multiChildType = Enum.Parse<VRC.Dynamics.VRCPhysBoneBase.MultiChildType>(propertyNode.Value); break;
                                    case "pull": pb.pull = float.Parse(propertyNode.Value.Split(',')[0]); break;
                                    case "spring": pb.spring = float.Parse(propertyNode.Value.Split(',')[0]); break;
                                    case "stiffness": pb.stiffness = float.Parse(propertyNode.Value.Split(',')[0]); break;
                                    case "gravity": pb.gravity = float.Parse(propertyNode.Value.Split(',')[0]); break;
                                    case "immobileType": pb.immobileType = Enum.Parse<VRC.Dynamics.VRCPhysBoneBase.ImmobileType>(propertyNode.Value); break;
                                    case "immobile": pb.immobile = float.Parse(propertyNode.Value.Split(',')[0]); break;
                                    case "allowCollision": pb.allowCollision = Enum.Parse<VRC.Dynamics.VRCPhysBoneBase.AdvancedBool>(propertyNode.Value); break;
                                    case "collisionFilter.allowSelf": pb.collisionFilter.allowSelf = bool.Parse(propertyNode.Value); break;
                                    case "collisionFilter.allowOthers": pb.collisionFilter.allowOthers = bool.Parse(propertyNode.Value); break;
                                    case "radius": pb.radius = float.Parse(propertyNode.Value.Split(',')[0]); break;
                                    case var prop when prop.StartsWith("collider[") && prop.EndsWith(']'):
                                    {
                                        var index = int.Parse(prop["collider[".Length..^1]);
                                        while (pb.colliders.Count < index)pb.colliders.Add(null);
                                        Delayed(() => pb.colliders[index] = ResolveGameObject(propertyNode.Value).GetComponent<VRC.Dynamics.VRCPhysBoneColliderBase>());
                                        break;
                                    }
                                    case "limitType": pb.limitType = Enum.Parse<VRC.Dynamics.VRCPhysBoneBase.LimitType>(propertyNode.Value); break;
                                    case "maxAngleX": pb.maxAngleX = float.Parse(propertyNode.Value.Split(',')[0]); break;
                                    case "maxAngleZ": pb.maxAngleZ = float.Parse(propertyNode.Value.Split(',')[0]); break;
                                    case "limitRotation.x": pb.limitRotation.x = float.Parse(propertyNode.Value.Split(',')[0]); break;
                                    case "limitRotation.y": pb.limitRotation.y = float.Parse(propertyNode.Value.Split(',')[0]); break;
                                    case "limitRotation.z": pb.limitRotation.z = float.Parse(propertyNode.Value.Split(',')[0]); break;
                                    case "allowGrabbing": pb.allowGrabbing = Enum.Parse<VRC.Dynamics.VRCPhysBoneBase.AdvancedBool>(propertyNode.Value); break;
                                    case "grabFilter.allowSelf": pb.grabFilter.allowSelf = bool.Parse(propertyNode.Value); break;
                                    case "grabFilter.allowOthers": pb.grabFilter.allowOthers = bool.Parse(propertyNode.Value); break;
                                    case "allowPosing": pb.allowPosing = Enum.Parse<VRC.Dynamics.VRCPhysBoneBase.AdvancedBool>(propertyNode.Value); break;
                                    case "poseFilter.allowSelf": pb.poseFilter.allowSelf = bool.Parse(propertyNode.Value); break;
                                    case "poseFilter.allowOthers": pb.poseFilter.allowOthers = bool.Parse(propertyNode.Value); break;
                                    case "snapToHand": pb.snapToHand = bool.Parse(propertyNode.Value); break;
                                    case "grabMovement": pb.grabMovement = float.Parse(propertyNode.Value); break;
                                    case "maxStretch": pb.maxStretch = float.Parse(propertyNode.Value.Split(',')[0]); break;
                                    case "maxSquish": pb.maxSquish = float.Parse(propertyNode.Value.Split(',')[0]); break;
                                    case "stretchMotion": pb.stretchMotion = float.Parse(propertyNode.Value.Split(',')[0]); break;
                                    case "isAnimated": pb.isAnimated = bool.Parse(propertyNode.Value); break;
                                    case "resetWhenDisabled": pb.resetWhenDisabled = bool.Parse(propertyNode.Value); break;
                                    case "parameter": pb.parameter = propertyNode.Value; break;
                                }
                            }
                            break;
                        }
#endif
                    }
                }
            }
        }

        private static Regex TupleRegex = new Regex(@"\((?:([^,]*)(?:,\s*)?)+\)");
        private static Vector3 ParseVector3(string value)
        {
            var match = TupleRegex.Match(value);
            var captures = match.Groups[1].Captures;
            if (captures.Count != 4) throw new Exception("Bad vector3 format");
            return new Vector3(float.Parse(captures[0].Value), float.Parse(captures[1].Value), float.Parse(captures[2].Value));
        }

        private static Quaternion ParseQuaternion(string value)
        {
            var vector4 = ParseVector4(value);
            return new Quaternion(vector4.x, vector4.y, vector4.z, vector4.w);
        }
        private static Vector4 ParseVector4(string value)
        {
            var match = TupleRegex.Match(value);
            var captures = match.Groups[1].Captures;
            if (captures.Count != 5) throw new Exception("Bad vector3 format");
            return new Vector4(float.Parse(captures[0].Value), float.Parse(captures[1].Value), float.Parse(captures[2].Value), float.Parse(captures[3].Value));
        }
    }
}
