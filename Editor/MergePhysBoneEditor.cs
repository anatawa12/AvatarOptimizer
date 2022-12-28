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
        private MergePhysBone Target => (MergePhysBone)target;


        private static class Style
        {
            public static readonly GUIStyle ErrorStyle = new GUIStyle
            {
                normal = { textColor = Color.red },
                wordWrap = false,
            };
            public static readonly GUIStyle WarningStyle = new GUIStyle
            {
                normal = { textColor = Color.yellow },
                wordWrap = false,
            };
        }

        public override void OnInspectorGUI()
        {
            

            EditorGUILayout.LabelField("Components:");

            for (var i = 0; i < Target.components.Length; i++)
            {
                Target.components[i] =
                    (VRCPhysBone)EditorGUILayout.ObjectField(Target.components[i], typeof(VRCPhysBone), true);
                if (Target.components[i].multiChildType != VRCPhysBoneBase.MultiChildType.Ignore)
                    GUILayout.Label("Multi child type must be Ignore", Style.ErrorStyle);
                if (Target.components[i].parameter != "")
                    GUILayout.Label("You cannot use individual parameter", Style.WarningStyle);
            }

            if (Target.components.Any(x => x == null))
                Target.components = Target.components.Where(x => x != null).ToArray();

            var toAdd = (VRCPhysBone)EditorGUILayout.ObjectField(null, typeof(VRCPhysBone), true);
            if (toAdd != null)
                ArrayUtility.Add(ref Target.components, toAdd);

            if (Target.components.ZipWithNext().Any(x => !IsSamePhysBone(x.Item1, x.Item2))) {
                GUILayout.Label("Some Component has different", Style.ErrorStyle);
            }
        }

        private bool Eq(float a, float b) => Mathf.Abs(a - b) < 0.00001f;
        private bool Eq(Vector3 a, Vector3 b) => (a - b).magnitude < 0.00001f;
        private bool Eq(AnimationCurve a, AnimationCurve b) => a.Equals(b);
        private bool Eq(float aFloat, AnimationCurve aCurve, float bFloat, AnimationCurve bCurve) => 
            Eq(aFloat, bFloat) && Eq(aCurve, bCurve);

        private bool IsValidPhysBone(VRCPhysBone a)
        {
            if (a.multiChildType != VRCPhysBoneBase.MultiChildType.Ignore) return false;
            if (a.parameter != "") return false;
            return true;
        }

        private bool IsSamePhysBone(VRCPhysBone a, VRCPhysBone b)
        {
            // === Transforms ===
            // Root Transform: ignore: we'll merge them
            // Ignore Transforms: ignore: we'll merge them
            // Endpoint position: ignore: we'll replace with zero and insert end bone instead
            // Multi Child Type: ignore: Must be 'Ignore'
            // == Forces ==
            if (a.integrationType != b.integrationType) return false;
            if (!Eq(a.pull, a.pullCurve, b.pull, b.pullCurve)) return false;
            if (!Eq(a.spring, a.springCurve, b.spring, b.springCurve)) return false;
            if (!Eq(a.stiffness, a.stiffnessCurve, b.stiffness, b.stiffnessCurve)) return false;
            if (!Eq(a.gravity, a.gravityCurve, b.gravity, b.gravityCurve)) return false;
            if (!Eq(a.gravityFalloff, a.gravityFalloffCurve, b.gravityFalloff, b.gravityFalloffCurve)) return false;
            if (a.immobileType != b.immobileType) return false;
            if (!Eq(a.immobile, a.immobileCurve, b.immobile, b.immobileCurve)) return false;
            // == Limits ==
            if (a.limitType != b.limitType) return false;
            if (!Eq(a.maxAngleX, a.maxAngleXCurve, b.maxAngleX, b.maxAngleXCurve)) return false;
            if (!Eq(a.maxAngleZ, a.maxAngleZCurve, b.maxAngleZ, b.maxAngleZCurve)) return false;
            if (!Eq(a.limitRotation, b.limitRotation)) return false;
            // == Collision ==
            if (!Eq(a.radius, a.radiusCurve, b.radius, b.radiusCurve)) return false;
            if (a.allowCollision != b.allowCollision) return false;
            if (!Eq(a.colliders, b.colliders)) return false;
            // == Grab & Pose ==
            if (a.allowGrabbing != b.allowGrabbing) return false;
            if (a.allowPosing != b.allowPosing) return false;
            if (!Eq(a.grabMovement, b.grabMovement)) return false;
            if (!Eq(a.maxStretch, a.maxStretchCurve, b.maxStretch, b.maxStretchCurve)) return false;
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
