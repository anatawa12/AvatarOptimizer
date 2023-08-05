using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors
{
    partial class AutomaticConfigurationProcessor
    {
        void FindUnusedObjects()
        {
            // mark & sweep
            var gameObjects = new HashSet<GameObject>(_session.GetComponents<Transform>().Select(x => x.gameObject));
            var referenced = new HashSet<GameObject>();
            var newReferenced = new Queue<GameObject>();

            void AddGameObject(GameObject gameObject)
            {
                if (gameObject && gameObjects.Contains(gameObject) && referenced.Add(gameObject))
                    newReferenced.Enqueue(gameObject);
            }

            // entry points: active GameObjects
            foreach (var component in gameObjects.Where(x => x.activeInHierarchy))
                AddGameObject(component);

            // entry points: modified enable/disable
            foreach (var keyValuePair in _modifiedProperties)
            {
                if (!(keyValuePair.Key is GameObject gameObject)) continue;
                if (!keyValuePair.Value.TryGetValue("m_IsActive", out _)) continue;

                AddGameObject(gameObject);
            }

            while (newReferenced.Count != 0)
            {
                var gameObject = newReferenced.Dequeue();

                foreach (var component in gameObject.GetComponents<Component>())
                {
                    if (component is Transform transform)
                    {
                        if (transform.parent)
                            AddGameObject(transform.parent.gameObject);
                        continue;
                    }

                    if (component is VRCPhysBoneBase)
                    {
                        foreach (var child in component.GetComponentsInChildren<Transform>(true))
                            AddGameObject(child.gameObject);
                    }

                    using (var serialized = new SerializedObject(component))
                    {
                        var iter = serialized.GetIterator();
                        var enterChildren = true;
                        while (iter.Next(enterChildren))
                        {
                            if (iter.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                var value = iter.objectReferenceValue;
                                if (value is Component c && !EditorUtility.IsPersistent(value))
                                    AddGameObject(c.gameObject);
                            }

                            switch (iter.propertyType)
                            {
                                case SerializedPropertyType.Integer:
                                case SerializedPropertyType.Boolean:
                                case SerializedPropertyType.Float:
                                case SerializedPropertyType.String:
                                case SerializedPropertyType.Color:
                                case SerializedPropertyType.ObjectReference:
                                case SerializedPropertyType.Enum:
                                case SerializedPropertyType.Vector2:
                                case SerializedPropertyType.Vector3:
                                case SerializedPropertyType.Vector4:
                                case SerializedPropertyType.Rect:
                                case SerializedPropertyType.ArraySize:
                                case SerializedPropertyType.Character:
                                case SerializedPropertyType.Bounds:
                                case SerializedPropertyType.Quaternion:
                                case SerializedPropertyType.FixedBufferSize:
                                case SerializedPropertyType.Vector2Int:
                                case SerializedPropertyType.Vector3Int:
                                case SerializedPropertyType.RectInt:
                                case SerializedPropertyType.BoundsInt:
                                    enterChildren = false;
                                    break;
                                case SerializedPropertyType.Generic:
                                case SerializedPropertyType.LayerMask:
                                case SerializedPropertyType.AnimationCurve:
                                case SerializedPropertyType.Gradient:
                                case SerializedPropertyType.ExposedReference:
                                case SerializedPropertyType.ManagedReference:
                                default:
                                    enterChildren = true;
                                    break;
                            }
                        }
                    }
                }
            }

            // sweep
            foreach (var gameObject in gameObjects.Where(x => !referenced.Contains(x)))
            {
                if (gameObject)
                    Object.DestroyImmediate(gameObject);
            }
        }
    }
}