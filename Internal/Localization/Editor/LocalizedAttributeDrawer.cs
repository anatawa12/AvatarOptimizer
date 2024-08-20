using nadena.dev.ndmf.localization;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomPropertyDrawer(typeof(AAOLocalizedAttribute))]
    class LocalizedAttributeDrawer : InheritingDrawer<AAOLocalizedAttribute>
    {
        private AAOLocalizedAttribute _attribute = null!; // initialized in Initialize method
        private string? _localeCode;
        private GUIContent _label = new GUIContent();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            InitializeUpstream(property);
            return base.GetPropertyHeight(property, _label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitializeUpstream(property);
            Update();
            base.OnGUI(position, property, _label);
        }

        protected override void Initialize()
        {
            _attribute = (AAOLocalizedAttribute)attribute;
            Update(true);
        }

        private void Update(bool force = false)
        {
            if (force || _localeCode != LanguagePrefs.Language)
            {
                _label = new GUIContent(AAOL10N.Tr(_attribute.LocalizationKey));
                if (_attribute.TooltipKey != null)
                    _label.tooltip = AAOL10N.Tr(_attribute.TooltipKey);
                _localeCode = LanguagePrefs.Language;
            }
        }
    }
}
