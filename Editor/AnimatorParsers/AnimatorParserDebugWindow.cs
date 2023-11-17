using System;
using System.Text;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.AnimatorParsers
{
    internal class AnimatorParserDebugWindow : EditorWindow
    {
        [MenuItem("Tools/Avatar Optimizer/Animator Parser Debug Window")]
        private static void Open() => GetWindow<AnimatorParserDebugWindow>("AnimatorParser Debug Window");

        public ParserSource parserSource;
        public RuntimeAnimatorController animatorController;
        public GameObject avatar;
        public GameObject rootGameObject;
        public Motion motion;

        public Vector2 scrollView;

        public GameObject parsedRootObject;
        public ImmutableModificationsContainer Container;

        private void OnGUI()
        {
            OnParserSourceGUI();

            using (new EditorGUI.DisabledScope(!parsedRootObject))
            {
                if (GUILayout.Button("Copy Parsed Text"))
                    GUIUtility.systemCopyBuffer = CreateText();
            }

            scrollView = GUILayout.BeginScrollView(scrollView);

            foreach (var (gameObject, properties) in Container.ModifiedProperties)
            {
                EditorGUILayout.ObjectField(gameObject, typeof(Object), true);
                EditorGUI.indentLevel++;
                foreach (var (propName, propState) in properties.FloatProperties)
                {
                    string propStateInfo;

                    switch (propState.State)
                    {
                        case AnimationPropertyState.ConstantAlways:
                            propStateInfo = $"Always:{propState.ConstValue}";
                            break;
                        case AnimationPropertyState.ConstantPartially:
                            propStateInfo = $"Partially:{propState.ConstValue}";
                            break;
                        case AnimationPropertyState.Variable:
                            propStateInfo = "Variable";
                            break;
                        case AnimationPropertyState.Invalid:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    NarrowValueLabelField(propName, propStateInfo);
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.EndScrollView();
        }

        private string CreateText()
        {
            var root = parsedRootObject.transform;
            var resultText = new StringBuilder();
            
            foreach (var (obj, properties) in Container.ModifiedProperties)
            {
                var gameObject = obj.gameObject.transform;
                resultText.Append(Utils.RelativePath(root, gameObject)).Append(": ")
                    .Append(((Object)obj).GetType().FullName).Append('\n');

                foreach (var (propName, propState) in properties.FloatProperties)
                    PrintAbout(propName, propState);
                foreach (var (propName, propState) in properties.FloatProperties)
                    PrintAbout(propName, propState);
                
                void PrintAbout<T>(string propName, IAnimationProperty<T> propState)
                {
                    string propStateInfo;

                    switch (propState.State)
                    {
                        case AnimationPropertyState.ConstantAlways:
                            propStateInfo = $"Always:{propState.ConstValue}";
                            break;
                        case AnimationPropertyState.ConstantPartially:
                            propStateInfo = $"Partially:{propState.ConstValue}";
                            break;
                        case AnimationPropertyState.Variable:
                            propStateInfo = "Variable";
                            break;
                        case AnimationPropertyState.Invalid:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    resultText.Append(propName).Append(": ").Append(propStateInfo).Append('\n');
                }

                resultText.Append('\n');
            }

            return resultText.ToString();
        }

        private static void NarrowValueLabelField(string label0, string value)
        {
            var position = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(true, 18f));

            var valueWidth = EditorGUIUtility.labelWidth;
            var labelPosition = new Rect(position.x, position.y, position.width - valueWidth, position.height);
            var valuePosition = new Rect(position.x + labelPosition.width + 2, position.y, valueWidth - 2, position.height);

            GUI.Label(labelPosition, label0);
            GUI.Label(valuePosition, value);
        }

        void OnParserSourceGUI()
        {
            parserSource = (ParserSource)EditorGUILayout.EnumPopup(parserSource);
            switch (parserSource)
            {
                case ParserSource.WholeAvatar:
                    ObjectField(ref avatar, "Avatar", true);

                    using (new EditorGUI.DisabledScope(!avatar))
                    {
                        if (GUILayout.Button("Parse") && avatar)
                        {
                            parsedRootObject = avatar;
                            Container = new AnimatorParser(true).GatherAnimationModifications(
                                new BuildContext(avatar, null));
                        }
                    }

                    break;
                case ParserSource.AnimatorController:
                    ObjectField(ref animatorController, "Controller", false);
                    ObjectField(ref rootGameObject, "Root GameObject", false);

                    using (new EditorGUI.DisabledScope(!animatorController || !rootGameObject))
                    {
                        if (GUILayout.Button("Parse") && animatorController && rootGameObject)
                        {
                            parsedRootObject = rootGameObject;
                            Container = new AnimatorParser(true)
                                .ParseAnimatorController(rootGameObject, animatorController)
                                .ToImmutable();
                        }
                    }

                    break;
                case ParserSource.Motion:
                    ObjectField(ref motion, "Motion", false);
                    ObjectField(ref rootGameObject, "Root GameObject", false);

                    using (new EditorGUI.DisabledScope(!motion))
                    {
                        if (GUILayout.Button("Parse") && motion && rootGameObject)
                        {
                            parsedRootObject = rootGameObject;
                            Container = new AnimationParser()
                                .ParseMotion(rootGameObject, motion, Utils.EmptyDictionary<AnimationClip, AnimationClip>())
                                .ToImmutable();
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ObjectField<T>(ref T value, [CanBeNull] string label, bool allowScene) where T : Object
        {
            value = EditorGUILayout.ObjectField(label, value, typeof(T), allowScene) as T;
        }

        public enum ParserSource
        {
            WholeAvatar,
            AnimatorController,
            Motion
        }
    }
}