using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomPropertyDrawer(typeof(EulerQuaternionAttribute))]
    public class EulerQuaternionAttributeDrawer : PropertyDrawer
    {
        /// <summary>
        ///   <para>Override this method to make your own IMGUI based GUI for the property.</para>
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the property GUI.</param>
        /// <param name="property">The SerializedProperty to make the custom GUI for.</param>
        /// <param name="label">The label of this property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (fieldInfo.FieldType != typeof(Quaternion))
            {
                EditorGUI.LabelField(position, label.text, "Use EulerQuaternion with Quaternion.");
                return;
            }

            label = EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();
            var changedEuler = EditorGUI.Vector3Field(position, label, property.quaternionValue.eulerAngles);
            if (EditorGUI.EndChangeCheck())
                property.quaternionValue = Quaternion.Euler(changedEuler);

            EditorGUI.EndProperty();
        }

        public override bool CanCacheInspectorGUI(SerializedProperty property) => false;
    }
}
