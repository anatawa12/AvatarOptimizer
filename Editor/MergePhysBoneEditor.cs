using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Anatawa12.Merger
{
    [CustomEditor(typeof(MergePhysBone))]
    internal class MergePhysBoneEditor : Editor
    {
        private bool _inited = false;
        private bool _differ = false;
        public override void OnInspectorGUI()
        {
            // ReSharper disable once LocalVariableHidesMember
            var target = (MergePhysBone)this.target;

            EditorGUI.BeginChangeCheck();
            
            var size = EditorGUILayout.IntField("Size", target.components.Length);
            if (size != target.components.Length)
            {
                Array.Resize(ref target.components, size);
            }

            for (var i = 0; i < target.components.Length; i++)
                target.components[i] =
                    (VRCPhysBone)EditorGUILayout.ObjectField(target.components[i], typeof(VRCPhysBone), true);
            

            if (EditorGUI.EndChangeCheck() || _inited)
            {
                var nonNulls = target.components.Where(x => x != null).ToArray();
                if (nonNulls.Length >= 2)
                {
                    var first = nonNulls[0];

                    _differ = nonNulls.Skip(1).Any(x => !IsSamePhysBone(x, first));
                }
                else
                {
                    _differ = false;
                }

            }

            if (_differ) {
                var style = new GUIStyle();
                style.normal.textColor = Color.red;
                style.wordWrap = false;
                GUILayout.Label("Some Component has different", style);
            }

            _inited = true;
        }

        private bool Eq(float a, float b) => Mathf.Abs(a - b) < 0.00001f;
        private bool Eq(Vector3 a, Vector3 b) => (a - b).magnitude < 0.00001f;
        private bool Eq(AnimationCurve a, AnimationCurve b) => a.Equals(b);

        private bool IsSamePhysBone(VRCPhysBone a, VRCPhysBone b)
        {
            // === Transforms ===
            // Root Transform: ignore: we'll merge them
            // Ignore Transforms: ignore: we'll merge them
            // Endpoint position: ignore: we'll replace with zero and insert end bone instead
            // Multi Child Type: ignore: Must be 'Ignore'
            // == Forces ==
            if (a.integrationType != b.integrationType) return false;
            if (!Eq(a.pull, b.pull)) return false;
            if (!Eq(a.pullCurve, b.pullCurve)) return false;
            if (!Eq(a.spring, b.spring)) return false;
            if (!Eq(a.springCurve, b.springCurve)) return false;
            if (!Eq(a.stiffness, b.stiffness)) return false;
            if (!Eq(a.stiffnessCurve, b.stiffnessCurve)) return false;
            if (!Eq(a.gravity, b.gravity)) return false;
            if (!Eq(a.gravityCurve, b.gravityCurve)) return false;
            if (!Eq(a.gravityFalloff, b.gravityFalloff)) return false;
            if (!Eq(a.gravityFalloffCurve, b.gravityFalloffCurve)) return false;
            if (a.immobileType != b.immobileType) return false;
            if (!Eq(a.immobile, b.immobile)) return false;
            if (!Eq(a.immobileCurve, b.immobileCurve)) return false;
            Debug.Log("Forces");
            // == Limits ==
            if (a.limitType != b.limitType) return false;
            if (!Eq(a.maxAngleX, b.maxAngleX)) return false;
            if (!Eq(a.maxAngleXCurve, b.maxAngleXCurve)) return false;
            if (!Eq(a.maxAngleZ, b.maxAngleZ)) return false;
            if (!Eq(a.maxAngleZCurve, b.maxAngleZCurve)) return false;
            if (!Eq(a.limitRotation, b.limitRotation)) return false;
            Debug.Log("Limits");
            // == Collision ==
            if (!Eq(a.radius, b.radius)) return false;
            if (!Eq(a.radiusCurve, b.radiusCurve)) return false;
            if (a.allowCollision != b.allowCollision) return false;
            Debug.Log("Collision except colliders");
            if (!Eq(a.colliders, b.colliders)) return false;
            Debug.Log("Collision");
            // == Grab & Pose ==
            if (a.allowGrabbing != b.allowGrabbing) return false;
            if (a.allowPosing != b.allowPosing) return false;
            if (!Eq(a.grabMovement, b.grabMovement)) return false;
            if (!Eq(a.maxStretch, b.maxStretch)) return false;
            if (!Eq(a.maxStretchCurve, b.maxStretchCurve)) return false;
            Debug.Log("Grab & Pose");
            // == Options ==
            // Parameter: ignore: must be empty
            // Is Animated: ignore: we can merge them.
            // Gizmos: ignore: it should not affect actual behaviour
            return true;
        }

        private bool Eq(IEnumerable<VRCPhysBoneColliderBase> a, IEnumerable<VRCPhysBoneColliderBase> b) => 
            new HashSet<VRCPhysBoneColliderBase>(a).SetEquals(b);
    }
}
