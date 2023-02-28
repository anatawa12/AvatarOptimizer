using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    internal static class PrefabSafeSetUtil
    {
        public static int PrefabNestCount(Object instance)
        {
            var nestCount = 0;
            while ((bool)(instance = PrefabUtility.GetCorrespondingObjectFromSource(instance)))
                nestCount++;

            return nestCount;
        }

        public static bool IsNotNull<T>(T arg)
        {
            if (arg == null) return false;
            if (typeof(Object).IsAssignableFrom(typeof(T)))
                return (Object)(object)arg;
            return true;
        }
    }

    [UsedImplicitly] // used by reflection
    internal static class OnBeforeSerializeImpl<T, TLayer> where TLayer : PrefabLayer<T>, new()
    {
        [UsedImplicitly] // used by reflection
        public static void Impl(PrefabSafeSet<T, TLayer> self)
        {
            // fakeSlot must not be modified,
            self.fakeSlot = default;
            if (!self.OuterObject) return;

            // match prefabLayers count.
            var nestCount = PrefabSafeSetUtil.PrefabNestCount(self.OuterObject);

            if (self.prefabLayers.Length == nestCount)
                DistinctCheck(self, nestCount);
            else if (self.prefabLayers.Length < nestCount)
                self.prefabLayers = PrefabSafeSetRuntimeUtil.ResizeArray(self.prefabLayers, nestCount);
            else if (self.prefabLayers.Length > nestCount)
                ApplyModificationsToLatestLayer(self, nestCount);
        }

        private static void DistinctCheck(PrefabSafeSet<T, TLayer> self, int nestCount)
        {
            void DistinctCheckArray(ref T[] source, ref T[] checkedArray, Func<T, bool> filter)
            {
                if (checkedArray == source && source.All(filter)) return;
                var array = source.Distinct().Where(filter).ToArray();
                if (array.Length != source.Length)
                    source = array;
                checkedArray = source;
            }

            if (nestCount == 0)
            {
                DistinctCheckArray(ref self.mainSet, ref self.CheckedCurrentLayerAdditions, 
                    PrefabSafeSetUtil.IsNotNull);
            }
            else
            {
                var currentLayer = self.prefabLayers[nestCount - 1] ?? (self.prefabLayers[nestCount - 1] = new TLayer());
                DistinctCheckArray(ref currentLayer.additions, ref self.CheckedCurrentLayerAdditions,
                    PrefabSafeSetUtil.IsNotNull);
                DistinctCheckArray(ref currentLayer.removes, ref self.CheckedCurrentLayerRemoves,
                    x => PrefabSafeSetUtil.IsNotNull(x) && !currentLayer.additions.Contains(x));
            }
        }

        private static void ApplyModificationsToLatestLayer(PrefabSafeSet<T,TLayer> self, int nestCount)
        {
            // after apply modifications?: apply to latest layer
            if (nestCount == 0)
            {
                // nestCount is 0: apply everything to mainSet
                var result = new ListSet<T>(self.mainSet);
                foreach (var layer in self.prefabLayers)
                {
                    result.RemoveRange(layer.removes);
                    result.AddRange(layer.additions);
                }

                self.mainSet = result.ToArray();
                self.prefabLayers = Array.Empty<TLayer>();
            }
            else
            {
                // nestCount is not zero: apply to current layer
                var targetLayer = self.prefabLayers[nestCount - 1];
                var additions = new ListSet<T>(targetLayer.additions);
                var removes = new ListSet<T>(targetLayer.removes);

                foreach (var layer in self.prefabLayers.Skip(nestCount))
                {
                    additions.RemoveRange(layer.removes);
                    removes.AddRange(layer.removes);

                    additions.AddRange(layer.additions);
                    removes.RemoveRange(layer.additions);
                }

                targetLayer.additions = additions.ToArray();
                targetLayer.removes = removes.ToArray();

                // resize array.               
                self.prefabLayers = PrefabSafeSetRuntimeUtil.ResizeArray(self.prefabLayers, nestCount);
            }
        }
    }

    internal static class EditorStatics
    {
        public static readonly GUIContent MultiEditingNotSupported = new GUIContent("Multi editing not supported");
        public static readonly GUIContent UnsupportedType = new GUIContent("Element type is not supported");
        public static readonly GUIContent AddNotSupported = new GUIContent("Add Not Supported");

        public static readonly GUIContent ToAdd = new GUIContent("Element to add")
        {
            tooltip = "Drag & Drop value to here to add element to this set."
        };

        public static readonly GUIContent ForceAddButton = new GUIContent("+")
        {
            tooltip = "Add this element in current prefab modifications."
        };
    }

    [CustomPropertyDrawer(typeof(PrefabSafeSet<,>), true)]
    internal class ObjectsEditor : PropertyDrawer
    {
        private int _nestCountCache = -1;

        private int GetNestCount(Object obj) =>
            _nestCountCache != -1 ? _nestCountCache : _nestCountCache = PrefabSafeSetUtil.PrefabNestCount(obj);

        private readonly Dictionary<string, Editor> _caches =
            new Dictionary<string, Editor>();

        [CanBeNull]
        private Editor GetCache(SerializedProperty property)
        {
            if (!_caches.TryGetValue(property.propertyPath, out var cached))
            {
                var prop = property.FindPropertyRelative(Names.FakeSlot);
                if (IsSupportedPropType(prop.propertyType))
                {
                    _caches[property.propertyPath] = cached =
                        new Editor(property, GetNestCount(property.serializedObject.targetObject));
                }
                else
                {
                    _caches[property.propertyPath] = cached = null;
                }
            }

            return cached;
        }

        private static bool IsSupportedPropType(SerializedPropertyType type)
        {
            switch (type)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.String:
                case SerializedPropertyType.Color:
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Rect:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.Bounds:
                case SerializedPropertyType.ExposedReference:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.BoundsInt:
                    return true;
                default:
                    return false;
            }
        }

        private class Editor : EditorBase<object>
        {
            public Editor(SerializedProperty property, int nestCount) : base(property, nestCount)
            {
            }

            private protected override object GetValue(SerializedProperty prop)
            {
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        return prop.intValue;
                    case SerializedPropertyType.Boolean:
                        return prop.boolValue;
                    case SerializedPropertyType.Float:
                        return prop.floatValue;
                    case SerializedPropertyType.String:
                        return prop.stringValue;
                    case SerializedPropertyType.Color:
                        return prop.colorValue;
                    case SerializedPropertyType.ObjectReference:
                        return prop.objectReferenceValue;
                    case SerializedPropertyType.LayerMask:
                        return (LayerMask) prop.intValue;
                    case SerializedPropertyType.Enum:
                        return prop.enumValueIndex;
                    case SerializedPropertyType.Vector2:
                        return prop.vector2Value;
                    case SerializedPropertyType.Vector3:
                        return prop.vector3Value;
                    case SerializedPropertyType.Vector4:
                        return prop.vector4Value;
                    case SerializedPropertyType.Rect:
                        return prop.rectValue;
                    case SerializedPropertyType.ArraySize:
                        return prop.intValue;
                    case SerializedPropertyType.Character:
                        return (char) prop.intValue;
                    case SerializedPropertyType.AnimationCurve:
                        return prop.animationCurveValue;
                    case SerializedPropertyType.Bounds:
                        return prop.boundsValue;
                    case SerializedPropertyType.ExposedReference:
                        return prop.exposedReferenceValue;
                    case SerializedPropertyType.Vector2Int:
                        return prop.vector2IntValue;
                    case SerializedPropertyType.Vector3Int:
                        return prop.vector3IntValue;
                    case SerializedPropertyType.RectInt:
                        return prop.rectIntValue;
                    case SerializedPropertyType.BoundsInt:
                        return prop.boundsIntValue;
                    default:
                        throw new InvalidOperationException();
                }
            }

            private protected override void SetValue(SerializedProperty prop, object value) 
            {
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        prop.intValue = (int)value;
                        break;
                    case SerializedPropertyType.Boolean:
                        prop.boolValue = (bool)value;
                        break;
                    case SerializedPropertyType.Float:
                        prop.floatValue = (float)value;
                        break;
                    case SerializedPropertyType.String:
                        prop.stringValue = (string)value;
                        break;
                    case SerializedPropertyType.Color:
                        prop.colorValue = (Color)value;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        prop.objectReferenceValue = (Object)value;
                        break;
                    case SerializedPropertyType.LayerMask:
                        prop.intValue = (LayerMask)value;
                        break;
                    case SerializedPropertyType.Enum:
                        prop.enumValueIndex = (int)value;
                        break;
                    case SerializedPropertyType.Vector2:
                        prop.vector2Value = (Vector2)value;
                        break;
                    case SerializedPropertyType.Vector3:
                        prop.vector3Value = (Vector3)value;
                        break;
                    case SerializedPropertyType.Vector4:
                        prop.vector4Value = (Vector4)value;
                        break;
                    case SerializedPropertyType.Rect:
                        prop.rectValue = (Rect)value;
                        break;
                    case SerializedPropertyType.ArraySize:
                        prop.intValue = (int)value;
                        break;
                    case SerializedPropertyType.Character:
                        prop.intValue = (char)value;
                        break;
                    case SerializedPropertyType.AnimationCurve:
                        prop.animationCurveValue = (AnimationCurve)value;
                        break;
                    case SerializedPropertyType.Bounds:
                        prop.boundsValue = (Bounds)value;
                        break;
                    case SerializedPropertyType.ExposedReference:
                        prop.exposedReferenceValue = (Object)value;
                        break;
                    case SerializedPropertyType.Vector2Int:
                        prop.vector2IntValue = (Vector2Int)value;
                        break;
                    case SerializedPropertyType.Vector3Int:
                        prop.vector3IntValue = (Vector3Int)value;
                        break;
                    case SerializedPropertyType.RectInt:
                        prop.rectIntValue = (RectInt)value;
                        break;
                    case SerializedPropertyType.BoundsInt:
                        prop.boundsIntValue = (BoundsInt)value;
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            private protected override float GetAddRegionSize() => EditorGUIUtility.singleLineHeight;

            private protected override void OnGUIAddRegion(Rect position)
            {
                if (FakeSlot.propertyType == SerializedPropertyType.ObjectReference)
                {
                    var addValue = Field(position, EditorStatics.ToAdd, default);
                    if (addValue != null) EditorUtil.GetElementOf(addValue).Add();
                }
                else
                {
                    EditorGUI.LabelField(position, EditorStatics.ToAdd, EditorStatics.AddNotSupported);
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.isEditingMultipleObjects || !property.isExpanded)
                return EditorGUIUtility.singleLineHeight;
            var cache = GetCache(property);
            if (cache == null)
                return EditorGUIUtility.singleLineHeight;

            return cache.GetPropertyHeight() + EditorGUIUtility.singleLineHeight +
                   EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.isEditingMultipleObjects)
            {
                EditorGUI.LabelField(position, label, EditorStatics.MultiEditingNotSupported);
                return;
            }
            var cache = GetCache(property);
            if (cache == null)
            {
                EditorGUI.LabelField(position, label, EditorStatics.UnsupportedType);
                return;
            }

            position.height = EditorGUIUtility.singleLineHeight;

            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);

            if (property.isExpanded)
            {
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                cache.OnGUI(position);
            }
        }
    }

    /// <summary>
    ///  because there are many generic arguments, nameof is too long. so I use this class 
    /// </summary>
    internal static class Names
    {
        public const string FakeSlot = nameof(PrefabSafeSet<object, PrefabLayer<object>>.fakeSlot);
        public const string MainSet = nameof(PrefabSafeSet<object, PrefabLayer<object>>.mainSet);
        public const string PrefabLayers = nameof(PrefabSafeSet<object, PrefabLayer<object>>.prefabLayers);
        public const string Additions = nameof(PrefabLayer<object>.additions);
        public const string Removes = nameof(PrefabLayer<object>.removes);
    }

    internal abstract class EditorBase<T>
    {
        protected readonly SerializedProperty FakeSlot;
        protected readonly EditorUtil<T> EditorUtil;

        public EditorBase(SerializedProperty property, int nestCount)
        {
            if (property.serializedObject.isEditingMultipleObjects)
                throw new ArgumentException("multi editing not supported", nameof(property));
            FakeSlot = property.FindPropertyRelative(Names.FakeSlot)
                        ?? throw new ArgumentException("fakeSlot not found");
            EditorUtil = EditorUtil<T>.Create(property, nestCount, GetValue, SetValue);
        }

        private protected abstract T GetValue(SerializedProperty prop);
        private protected abstract void SetValue(SerializedProperty prop, T value);
        private protected abstract float GetAddRegionSize();
        private protected abstract void OnGUIAddRegion(Rect position);

        public float GetPropertyHeight() =>
            EditorUtil.ElementsCount * (FieldHeight() + EditorGUIUtility.standardVerticalSpacing) 
            + GetAddRegionSize();

        // position is 
        public void OnGUI(Rect position)
        {
            var elementI = 0;
            var newLabel = new GUIContent("");

            // to avoid changes in for loop
            Action action = null;

            foreach (var element in EditorUtil.Elements)
            {
                ModificationKind fieldModKind;

                switch (element.Status)
                {
                    case ElementStatus.Natural:
                        newLabel.text = $"Element {elementI++}";
                        fieldModKind = ModificationKind.Natural;
                        break;
                    case ElementStatus.Removed:
                        newLabel.text = "(Removed)";
                        fieldModKind = ModificationKind.Remove;
                        break;
                    case ElementStatus.NewElement:
                        newLabel.text = $"Element {elementI++}";
                        fieldModKind = ModificationKind.Add;
                        break;
                    case ElementStatus.AddedTwice:
                        newLabel.text = $"Element {elementI++} (Added twice)";
                        fieldModKind = ModificationKind.Add;
                        break;
                    case ElementStatus.FakeRemoved:
                        newLabel.text = "(Removed but not found)";
                        fieldModKind = ModificationKind.Remove;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var currentModKind = OnePrefabElement(position, newLabel, element, fieldModKind);

                switch (currentModKind)
                {
                    case ModificationKind.Natural:
                        break;
                    case ModificationKind.Remove:
                        action = element.Remove;
                        break;
                    case ModificationKind.Add:
                        action = element.Add;
                        break;
                }

                position.y += FieldHeight() + EditorGUIUtility.standardVerticalSpacing;
            }

            action?.Invoke();

            OnGUIAddRegion(position);
        }

        private enum ModificationKind
        {
            Natural,
            Add,
            Remove
        }

        private ModificationKind OnePrefabElement(Rect position, GUIContent label, IElement<T> element, 
            ModificationKind kind)
        {
            // layout
            var fieldPosition = position;
            // two buttons
            fieldPosition.width -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var addButtonPosition = new Rect(
                fieldPosition.xMax + EditorGUIUtility.standardVerticalSpacing, position.y,
                EditorGUIUtility.singleLineHeight, position.height);

            var result = ModificationKind.Natural;

            using (new PropertyScope<T>(element, fieldPosition, label))
            {
                EditorGUI.BeginDisabledGroup(kind == ModificationKind.Remove);
                // field
                var fieldValue = Field(fieldPosition, label, element.Value);
                if (fieldValue == null)
                    result = ModificationKind.Remove;

                EditorGUI.BeginDisabledGroup(kind == ModificationKind.Add);
                if (GUI.Button(addButtonPosition, EditorStatics.ForceAddButton))
                    result = ModificationKind.Add;
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndDisabledGroup();
            }

            return result;
        }

        protected virtual float FieldHeight() => EditorGUI.GetPropertyHeight(FakeSlot);

        protected virtual T Field(Rect position, GUIContent label, T value)
        {
            SetValue(FakeSlot, value);
            EditorGUI.PropertyField(position, FakeSlot, label);
            value = GetValue(FakeSlot);
            return value;
        }
    }

    /// <summary>
    /// Utility to edit PrefabSafeSet in CustomEditor with SerializedProperty
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class EditorUtil<T>
    {
        // common property;
        [NotNull] private readonly Func<SerializedProperty, T> _getValue;
        [NotNull] private readonly Action<SerializedProperty, T> _setValue;

        public abstract IEnumerable<IElement<T>> Elements { get; }
        public abstract int ElementsCount { get; }
        public virtual int Count => Elements.Count(x => x.Contains);
        public virtual IEnumerable<T> Values => Elements.Where(x => x.Contains).Select(x => x.Value);

        public static EditorUtil<T> Create(SerializedProperty property, int nestCount,
            Func<SerializedProperty, T> getValue,
            Action<SerializedProperty, T> setValue)
        {
            if (nestCount == 0)
                return new Root(property, getValue, setValue);
            return new PrefabModification(property, nestCount, getValue, setValue);
        }

        private EditorUtil(Func<SerializedProperty, T> getValue, Action<SerializedProperty, T> setValue)
        {
            _getValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
            _setValue = setValue ?? throw new ArgumentNullException(nameof(setValue));
        }

        public abstract void Clear();

        protected abstract IElement<T> NewSlotElement(T value);

        public IElement<T> GetElementOf(T value) =>
            Elements.FirstOrDefault(x => x.Value.Equals(value)) ?? NewSlotElement(value);

        public abstract void HandleApplyRevertMenuItems(IElement<T> element, GenericMenu genericMenu);

        private sealed class Root : EditorUtil<T>
        {
            private List<ElementImpl> _list;
            [NotNull] private readonly SerializedProperty _mainSet;
            public override int Count => _mainSet.arraySize;

            public Root(SerializedProperty property, Func<SerializedProperty, T> getValue,
                Action<SerializedProperty, T> setValue) : base(getValue, setValue)
            {
                _mainSet = property.FindPropertyRelative(Names.MainSet)
                           ?? throw new ArgumentException("mainSet not found", nameof(property));
            }

            public override IEnumerable<IElement<T>> Elements
            {
                get
                {
                    if (_list?.Count != _mainSet.arraySize)
                        _list = new ArrayPropertyEnumerable(_mainSet)
                            .Select((x, i) => new ElementImpl(this, x, i))
                            .ToList();
                    return _list;
                }
            }

            public override int ElementsCount => _mainSet.arraySize;

            public override void Clear() => _mainSet.arraySize = 0;

            protected override IElement<T> NewSlotElement(T value) => new ElementImpl(this, value);

            public override void HandleApplyRevertMenuItems(IElement<T> element, GenericMenu genericMenu)
            {
                // logic failure
            }

            private class ElementImpl : IElement<T>
            {
                public EditorUtil<T> Container => _container;
                public T Value { get; }
                public ElementStatus Status => Contains ? ElementStatus.Natural : ElementStatus.NewSlot;
                public bool Contains => _index >= 0;
                public SerializedProperty ModifierProp { get; private set; }

                private readonly Root _container;
                private int _index;

                public ElementImpl(Root container, SerializedProperty prop, int index)
                {
                    Value = container._getValue(prop);
                    _container = container;
                    _index = index;
                    ModifierProp = prop;
                }

                public ElementImpl(Root container, T value)
                {
                    Value = value;
                    _container = container;
                    _index = -1;
                    ModifierProp = null;
                }

                public void EnsureAdded() => Add();

                public void Add()
                {
                    if (Contains) return;
                    _index = _container._mainSet.arraySize;
                    _container._setValue(ModifierProp = AddArrayElement(_container._mainSet), Value);
                    _container._list.Add(this);
                }

                public void EnsureRemoved() => Remove();

                public void Remove()
                {
                    if (!Contains) return;
                    _container.RemoveArrayElementAt(_container._mainSet, _index);
                    _index = -1;
                    ModifierProp = null;
                    _container._list.Remove(this);
                }

                public void SetExistence(bool existence)
                {
                    if (existence) Add();
                    else Remove();
                }
            }
        }

        private struct ArraySizeCheck
        {
            private int _size;
            private readonly SerializedProperty _prop;

            public ArraySizeCheck(SerializedProperty prop)
            {
                _prop = prop;
                _size = _prop.intValue;
            }

            public bool Changed => _size != _prop.intValue;

            public void Updated() => _size = _prop.intValue;
        }

        private sealed class PrefabModification : EditorUtil<T>
        {
            private readonly List<ElementImpl> _elements;
            private bool _needsUpstreamUpdate;
            private int _upstreamElementCount;
            private readonly SerializedProperty _rootProperty;
            private readonly SerializedProperty _currentRemoves;
            private readonly SerializedProperty _currentAdditions;
            private int _currentRemovesSize;
            private int _currentAdditionsSize;

            // upstream change check
            private ArraySizeCheck _mainSet;
            private readonly ArraySizeCheck[] _layerRemoves;
            private readonly ArraySizeCheck[] _layerAdditions;

            public PrefabModification(SerializedProperty property, int nestCount,
                Func<SerializedProperty, T> getValue, Action<SerializedProperty, T> setValue) : base(getValue,
                setValue)
            {
                _elements = new List<ElementImpl>();
                _rootProperty = property;

                ClearNonLayerModifications(property, nestCount);

                var mainSet = property.FindPropertyRelative(Names.MainSet);
                _mainSet = new ArraySizeCheck(mainSet.FindPropertyRelative("Array.size"));
                _layerRemoves = new ArraySizeCheck[nestCount - 1];
                _layerAdditions = new ArraySizeCheck[nestCount - 1];

                // apply modifications until previous one
                var prefabLayers = property.FindPropertyRelative(Names.PrefabLayers);
                // process current layer
                if (prefabLayers.arraySize < nestCount) prefabLayers.arraySize = nestCount;

                DoInitializeUpstream();

                var currentLayer = prefabLayers.GetArrayElementAtIndex(nestCount - 1);
                _currentRemoves = currentLayer.FindPropertyRelative(Names.Removes)
                                  ?? throw new ArgumentException("prefabLayers.removes not found",
                                      nameof(property));
                _currentAdditions = currentLayer.FindPropertyRelative(Names.Additions)
                                    ?? throw new ArgumentException("prefabLayers.additions not found",
                                        nameof(property));

                DoInitialize();
            }

            private void DoInitializeUpstream()
            {
                _elements.Clear();
                var upstreamValues = new HashSet<T>();

                var mainSet = _rootProperty.FindPropertyRelative(Names.MainSet);

                foreach (var valueProp in new ArrayPropertyEnumerable(mainSet))
                {
                    var value = _getValue(valueProp);
                    if (value == null) continue;
                    if (upstreamValues.Contains(value)) continue;
                    upstreamValues.Add(value);
                    _elements.Add(ElementImpl.Natural(this, value, 0));
                }

                var prefabLayers = _rootProperty.FindPropertyRelative(Names.PrefabLayers);

                for (var i = 0; i < prefabLayers.arraySize - 1; i++)
                {
                    var layer = prefabLayers.GetArrayElementAtIndex(i);
                    var removes = layer.FindPropertyRelative(Names.Removes);
                    var additions = layer.FindPropertyRelative(Names.Additions);

                    foreach (var prop in new ArrayPropertyEnumerable(removes))
                    {
                        var value = _getValue(prop);
                        if (upstreamValues.Remove(value))
                            _elements.RemoveAll(x => x.Value.Equals(value));
                    }

                    foreach (var prop in new ArrayPropertyEnumerable(additions))
                    {
                        var value = _getValue(prop);
                        if (upstreamValues.Add(value))
                            _elements.Add(ElementImpl.Natural(this, value, i + 1));
                    }

                    _layerRemoves[i] = new ArraySizeCheck(removes.FindPropertyRelative("Array.size"));
                    _layerAdditions[i] = new ArraySizeCheck(additions.FindPropertyRelative("Array.size"));
                }

                _upstreamElementCount = _elements.Count;
                _mainSet.Updated();
            }

            private void ClearNonLayerModifications(SerializedProperty property, int nestCount)
            {
                try
                {
                    var thisObjectPropPath = property.propertyPath;
                    var arraySizeProp = property.FindPropertyRelative(Names.PrefabLayers)
                        .FindPropertyRelative("Array.size").propertyPath;
                    var arrayValueProp = property.FindPropertyRelative(Names.PrefabLayers)
                        .GetArrayElementAtIndex(nestCount - 1).propertyPath;
                    var serialized = property.serializedObject;
                    var obj = serialized.targetObject;

                    foreach (var modification in PrefabUtility.GetPropertyModifications(obj))
                    {
                        // if property is not of the object: do nothing
                        if (!modification.propertyPath.StartsWith(thisObjectPropPath)) continue;
                        // if property is Array.size or current layer of nest: allow modification
                        if (modification.propertyPath.StartsWith(arraySizeProp)) continue;
                        if (modification.propertyPath.StartsWith(arrayValueProp)) continue;
                        // that modification is not allowed: revert
                        PrefabUtility.RevertPropertyOverride(serialized.FindProperty(modification.propertyPath),
                            InteractionMode.AutomatedAction);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    // ignored
                }
            }

            public override IEnumerable<IElement<T>> Elements
            {
                get
                {
                    Initialize();
                    return _elements;
                }
            }

            public override int ElementsCount => _elements.Count;

            /// <summary>
            /// initialize or update cache info if needed
            /// </summary>
            public void Initialize()
            {
                if (_mainSet.Changed || _layerRemoves.Any(x => x.Changed) || _layerAdditions.Any(x => x.Changed))
                {
                    DoInitializeUpstream();
                }

                if (_currentRemovesSize != _currentRemoves.arraySize ||
                    _currentAdditionsSize != _currentAdditions.arraySize)
                {
                    DoInitialize();
                }
            }

            /// <summary>
            /// initialize or update cache info
            /// </summary>
            public void DoInitialize()
            {
                var removesArray = ToArray(_currentRemoves);
                var removesSet = new HashSet<T>(removesArray);
                var additionsArray = ToArray(_currentAdditions);
                var addsSet = new HashSet<T>(additionsArray);

                _elements.RemoveRange(_upstreamElementCount, _elements.Count - _upstreamElementCount);

                for (var i = 0; i < _elements.Count; i++)
                {
                    var element = _elements[i];

                    var value = element.Value;
                    if (removesSet.Remove(value))
                    {
                        var index = Array.IndexOf(removesArray, value);
                        element.MarkRemovedAt(index);
                    }
                    else if (addsSet.Remove(value))
                    {
                        var index = Array.IndexOf(additionsArray, value);
                        element.MarkAddedTwiceAt(index);
                    }
                    else
                    {
                        element.MarkNatural();
                    }

                    _elements[i] = element;
                }

                // newly added elements
                for (var i = 0; i < additionsArray.Length; i++)
                {
                    var value = additionsArray[i];
                    if (!addsSet.Contains(value)) continue; // it's duplicated addition

                    _elements.Add(ElementImpl.NewElement(this, value, i));
                }

                // fake removed elements
                for (var i = 0; i < removesArray.Length; i++)
                {
                    var value = removesArray[i];
                    if (!removesSet.Contains(value)) continue; // it's removed upper layer

                    _elements.Add(ElementImpl.FakeRemoved(this, value, i));
                }

                _currentRemovesSize = _currentRemoves.arraySize;
                _currentAdditionsSize = _currentAdditions.arraySize;
            }

            public override void Clear()
            {
                Initialize();
                for (var i = _elements.Count - 1; i >= _upstreamElementCount; i--)
                    _elements[i].EnsureRemoved();
                _currentAdditionsSize = _currentAdditions.arraySize = 0;
            }

            protected override IElement<T> NewSlotElement(T value) => ElementImpl.NewSlot(this, value);

            private class ElementImpl : IElement<T>
            {
                public EditorUtil<T> Container => _container;
                private readonly PrefabModification _container;
                private int _indexInModifier;
                internal readonly int SourceNestCount;
                public T Value { get; }
                public ElementStatus Status { get; private set; }

                public bool Contains
                {
                    get
                    {
                        switch (Status)
                        {
                            case ElementStatus.Natural:
                            case ElementStatus.NewElement:
                            case ElementStatus.AddedTwice:
                                return true;
                            case ElementStatus.FakeRemoved:
                            case ElementStatus.Removed:
                            case ElementStatus.NewSlot:
                                return false;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                public SerializedProperty ModifierProp { get; private set; }

                private ElementImpl(PrefabModification container, int indexInModifier, T value, ElementStatus status,
                    int sourceNestCount, SerializedProperty modifierProp)
                {
                    _container = container;
                    _indexInModifier = indexInModifier;
                    SourceNestCount = sourceNestCount;
                    Value = value;
                    Status = status;
                    ModifierProp = modifierProp;
                }

                public static ElementImpl Natural(PrefabModification container, T value, int nestCount) =>
                    new ElementImpl(container, -1, value, ElementStatus.Natural, nestCount, null);

                public static ElementImpl NewElement(PrefabModification container, T value, int i) =>
                    new ElementImpl(container, i, value, ElementStatus.NewElement, -1,
                        container._currentAdditions.GetArrayElementAtIndex(i));

                public static ElementImpl FakeRemoved(PrefabModification container, T value, int i) =>
                    new ElementImpl(container, i, value, ElementStatus.FakeRemoved, -1,
                        container._currentRemoves.GetArrayElementAtIndex(i));

                public static ElementImpl NewSlot(PrefabModification container, T value) =>
                    new ElementImpl(container, -1, value, ElementStatus.NewSlot, -1, null);

                public void EnsureAdded() => DoAdd(false);
                public void Add() => DoAdd(true);
                public void EnsureRemoved() => DoRemove(false);
                public void Remove() => DoRemove(true);

                private void DoAdd(bool forceAdd)
                {
                    void AddToAdditions(ElementStatus status)
                    {
                        _indexInModifier = _container._currentAdditions.arraySize;
                        _container._setValue(ModifierProp = AddArrayElement(_container._currentAdditions), Value);
                        _container._currentAdditionsSize += 1;
                        Status = status;
                    }

                    switch (Status)
                    {
                        case ElementStatus.Natural:
                            if (forceAdd)
                                AddToAdditions(ElementStatus.AddedTwice);
                            break;
                        case ElementStatus.Removed:
                            _container._currentRemovesSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentRemoves, _indexInModifier);
                            Status = ElementStatus.Natural;
                            ModifierProp = null;
                            break;
                        case ElementStatus.NewElement:
                        case ElementStatus.AddedTwice:
                            // already added
                            break;
                        case ElementStatus.FakeRemoved:
                            _container._currentRemovesSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentRemoves, _indexInModifier);
                            AddToAdditions(ElementStatus.NewElement);
                            break;
                        case ElementStatus.NewSlot:
                            AddToAdditions(ElementStatus.NewElement);
                            _container._elements.Add(this);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                private void DoRemove(bool forceRemove)
                {
                    void AddToRemoves(ElementStatus status)
                    {
                        _indexInModifier = _container._currentRemoves.arraySize;
                        _container._setValue(ModifierProp = AddArrayElement(_container._currentRemoves), Value);
                        _container._currentRemovesSize += 1;
                        Status = status;
                    }

                    switch (Status)
                    {
                        case ElementStatus.Natural:
                            AddToRemoves(ElementStatus.Removed);
                            break;
                        case ElementStatus.Removed:
                        case ElementStatus.FakeRemoved:
                            // already removed: nothing to do
                            break;
                        case ElementStatus.NewElement:
                            _container._currentAdditionsSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentAdditions, _indexInModifier);
                            Status = ElementStatus.NewSlot;
                            _container._elements.Remove(this);
                            ModifierProp = null;
                            break;
                        case ElementStatus.AddedTwice:
                            _container._currentAdditionsSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentAdditions, _indexInModifier);
                            AddToRemoves(ElementStatus.Removed);
                            break;
                        case ElementStatus.NewSlot:
                            if (forceRemove)
                            {
                                AddToRemoves(ElementStatus.FakeRemoved);
                                _container._elements.Add(this);
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                internal void Revert()
                {
                    switch (Status)
                    {
                        case ElementStatus.Natural:
                            break; // nop
                        case ElementStatus.Removed:
                            _container._currentRemovesSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentRemoves, _indexInModifier);
                            Status = ElementStatus.Natural;
                            ModifierProp = null;
                            break;
                        case ElementStatus.NewElement:
                            _container._currentAdditionsSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentAdditions, _indexInModifier);
                            Status = ElementStatus.NewSlot;
                            _container._elements.Remove(this);
                            ModifierProp = null;
                            break;
                        case ElementStatus.AddedTwice:
                            _container._currentAdditionsSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentAdditions, _indexInModifier);
                            Status = ElementStatus.Natural;
                            ModifierProp = null;
                            break;
                        case ElementStatus.FakeRemoved:
                            _container._currentRemovesSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentRemoves, _indexInModifier);
                            Status = ElementStatus.NewSlot;
                            _container._elements.Remove(this);
                            ModifierProp = null;
                            break;
                        case ElementStatus.NewSlot:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    _container._rootProperty.serializedObject.ApplyModifiedProperties();
                }

                public void MarkRemovedAt(int index)
                {
                    _indexInModifier = index;
                    ModifierProp = _container._currentRemoves.GetArrayElementAtIndex(index);
                    Status = ElementStatus.Removed;
                }

                public void MarkAddedTwiceAt(int index)
                {
                    _indexInModifier = index;
                    ModifierProp = _container._currentAdditions.GetArrayElementAtIndex(index);
                    Status = ElementStatus.AddedTwice;
                }

                public void MarkNatural()
                {
                    Status = ElementStatus.Natural;
                }

                public void SetExistence(bool existence)
                {
                    if (existence != Contains)
                    {
                        switch (Status)
                        {
                            case ElementStatus.NewElement:
                                Remove();
                                Remove();
                                break;
                            case ElementStatus.Natural:
                            case ElementStatus.AddedTwice:
                                Remove();
                                break;

                            case ElementStatus.Removed:
                                Add();
                                Add();
                                break;
                            case ElementStatus.FakeRemoved:
                            case ElementStatus.NewSlot:
                                Add();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

            public override void HandleApplyRevertMenuItems(IElement<T> element, GenericMenu genericMenu)
            {
                var elementImpl = (ElementImpl)element;
                HandleApplyMenuItems(_currentAdditions.serializedObject.targetObject, elementImpl, genericMenu);
                HandleRevertMenuItem(genericMenu, elementImpl);
            }

            private void HandleApplyMenuItems(Object instanceOrAssetObject,
                ElementImpl elementImpl,
                GenericMenu genericMenu,
                bool defaultOverrideComparedToSomeSources = false)
            {
                var applyTargets = GetApplyTargets(instanceOrAssetObject,
                    defaultOverrideComparedToSomeSources);
                if (applyTargets == null || applyTargets.Count == 0)
                    return;
                for (var index = 0; index < applyTargets.Count; ++index)
                {
                    var componentOrGameObject = applyTargets[index];

                    var rootGameObject = GetRootGameObject(componentOrGameObject);
                    var format = L10n.Tr(index == applyTargets.Count - 1
                        ? "Apply to Prefab '{0}'"
                        : "Apply as Override in Prefab '{0}'");
                    var guiContent = new GUIContent(string.Format(format, rootGameObject.name));

                    var nestCount = applyTargets.Count - index - 1;

                    if (!PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(GetRootGameObject(componentOrGameObject)))
                    {
                        genericMenu.AddDisabledItem(guiContent);
                    }
                    else if (EditorUtility.IsPersistent(_rootProperty.serializedObject.targetObject))
                    {
                        genericMenu.AddDisabledItem(guiContent);
                    }
                    else
                    {
                        void RemoveFromList(SerializedProperty sourceArrayProp, T value)
                        {
                            var foundIndex = Array.IndexOf(ToArray(sourceArrayProp), value);
                            var serialized = new SerializedObject(componentOrGameObject);
                            if (foundIndex == -1) return;
                            var newArray = serialized.FindProperty(sourceArrayProp.propertyPath);
                            RemoveArrayElementAt(newArray, foundIndex);
                            serialized.ApplyModifiedProperties();
                            PrefabUtility.SavePrefabAsset(rootGameObject);
                        }

                        void AddToList(SerializedProperty sourceArrayProp, T value)
                        {
                            var serialized = new SerializedObject(componentOrGameObject);
                            var newArray = serialized.FindProperty(sourceArrayProp.propertyPath);
                            _setValue(AddArrayElement(newArray), value);
                            serialized.ApplyModifiedProperties();
                            PrefabUtility.SavePrefabAsset(rootGameObject);
                        }

                        switch (elementImpl.Status)
                        {
                            case ElementStatus.Natural:
                                // logic failure: Natural means nothing to override
                                genericMenu.AddDisabledItem(guiContent);
                                break;
                            case ElementStatus.Removed:
                                if (elementImpl.SourceNestCount <= nestCount)
                                {
                                    // the value is already added in the Property
                                    genericMenu.AddItem(guiContent, false, _ =>
                                    {
                                        if (nestCount == 0)
                                        {
                                            // apply target is base object: nestCount
                                            RemoveFromList(_rootProperty.FindPropertyRelative(Names.MainSet),
                                                elementImpl.Value);
                                        }
                                        else if (elementImpl.SourceNestCount == nestCount)
                                        {
                                            // apply target is addition
                                            var additionProp = _rootProperty.FindPropertyRelative(Names.PrefabLayers +
                                                $".Array.data[{nestCount - 1}]." + Names.Additions);
                                            RemoveFromList(additionProp, elementImpl.Value);
                                        }
                                        else
                                        {
                                            _setValue(
                                                AddArrayElement(_rootProperty.FindPropertyRelative(Names.PrefabLayers +
                                                    $".Array.data[{nestCount - 1}]." + Names.Removes)), elementImpl.Value);
                                        }

                                        elementImpl.Revert();
                                        ForceRebuildInspectors();
                                    }, null);
                                }
                                else
                                {
                                    genericMenu.AddDisabledItem(guiContent);
                                }
                                break;
                            case ElementStatus.NewElement:
                                // if possible, check for deleting this element in parent prefabs
                                genericMenu.AddItem(guiContent, false, (_) =>
                                {
                                    if (nestCount == 0)
                                    {
                                        AddToList(_rootProperty.FindPropertyRelative(Names.MainSet), elementImpl.Value);
                                    }
                                    else
                                    {
                                        AddToList(_rootProperty.FindPropertyRelative(Names.PrefabLayers +
                                            $".Array.data[{nestCount - 1}]." + Names.Additions), elementImpl.Value);
                                    }

                                    elementImpl.Revert();
                                    ForceRebuildInspectors();
                                }, null);
                                break;
                            case ElementStatus.AddedTwice:
                                genericMenu.AddDisabledItem(guiContent);
                                break;
                            case ElementStatus.FakeRemoved:
                                genericMenu.AddDisabledItem(guiContent);
                                break;
                            case ElementStatus.NewSlot:
                                // logic faliure
                                genericMenu.AddDisabledItem(guiContent);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

            private void HandleRevertMenuItem(GenericMenu genericMenu, ElementImpl element)
            {
                var guiContent = new GUIContent(L10n.Tr("Revert"));
                genericMenu.AddItem(guiContent, false, _ => element.Revert(), null);
            }

            private static List<Object> GetApplyTargets(
                Object instanceOrAssetObject,
                bool defaultOverrideComparedToSomeSources)
            {
                var applyTargets = new List<Object>();
                // verify the value is GameObject or Component
                if (!(instanceOrAssetObject is GameObject)) _ = (Component)instanceOrAssetObject;
                var obj = PrefabUtility.GetCorrespondingObjectFromSource(instanceOrAssetObject);
                if (obj == null)
                    return applyTargets;
                for (; obj != null; obj = PrefabUtility.GetCorrespondingObjectFromSource(obj))
                {
                    if (defaultOverrideComparedToSomeSources)
                    {
                        var gameObject2 = GetGameObject(obj);
                        if (gameObject2.transform.root == gameObject2.transform)
                            break;
                    }

                    applyTargets.Add(obj);
                }

                return applyTargets;
            }

            private static GameObject GetGameObject(Object componentOrGameObject)
            {
                var gameObject = componentOrGameObject as GameObject;
                if (gameObject)
                    return gameObject;
                var component = componentOrGameObject as Component;
                return component ? component.gameObject : null;
            }

            private static GameObject GetRootGameObject(Object componentOrGameObject)
            {
                var gameObject = GetGameObject(componentOrGameObject);
                return gameObject == null ? null : gameObject.transform.root.gameObject;
            }

            private static void ForceRebuildInspectors()
            {
                var type = typeof(EditorUtility);
                var method = type.GetMethod("ForceRebuildInspectors",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes, 
                    null);
                System.Diagnostics.Debug.Assert(method != null, nameof(method) + " != null");
                method.Invoke(null, null);
            }
        }

        private static SerializedProperty AddArrayElement(SerializedProperty array)
        {
            array.arraySize += 1;
            return array.GetArrayElementAtIndex(array.arraySize - 1);
        }

        private void RemoveArrayElementAt(SerializedProperty array, int index)
        {
            var prevProp = array.GetArrayElementAtIndex(index);
            for (var i = index + 1; i < array.arraySize; i++)
            {
                var curProp = array.GetArrayElementAtIndex(i);
                _setValue(prevProp, _getValue(curProp));;
                prevProp = curProp;
            }

            array.arraySize -= 1;
        }

        private T[] ToArray(SerializedProperty array)
        {
            var result = new T[array.arraySize];
            for (var i = 0; i < result.Length; i++)
                result[i] = _getValue(array.GetArrayElementAtIndex(i));
            return result;
        }
    }

    internal interface IElement<T>
    {
        EditorUtil<T> Container { get; }
        T Value { get; }
        ElementStatus Status { get; }
        bool Contains { get; }
        SerializedProperty ModifierProp { get; }
        void EnsureAdded();
        void Add();
        void EnsureRemoved();
        void Remove();
        void SetExistence(bool existence);
    }

    internal readonly struct PropertyScope<T> : IDisposable
    {
        private readonly SerializedProperty _property;
        private readonly Rect _totalPosition;
        public readonly IElement<T> Element;
        public readonly GUIContent Label;

        public PropertyScope(IElement<T> element, Rect totalPosition, GUIContent label)
        {
            _property = element.ModifierProp;
            Element = element;
            _totalPosition = totalPosition;
            Label = label;
            if (_property != null)
                Label = EditorGUI.BeginProperty(totalPosition, Label, _property);
        }

        public void Dispose()
        {
            if (_property != null)
            {
                if (Event.current.type == EventType.ContextClick &&
                    _totalPosition.Contains(Event.current.mousePosition))
                {
                    var genericMenu = new GenericMenu();
                    if (!_property.serializedObject.isEditingMultipleObjects 
                        && _property.isInstantiatedPrefab 
                        && _property.prefabOverride)
                    {
                        Event.current.Use();
                        Element.Container.HandleApplyRevertMenuItems(Element, genericMenu);
                        genericMenu.ShowAsContext();
                    }
                }

                EditorGUI.EndProperty();
            }
        }
    }


    internal enum ElementStatus
    {
        Natural,
        Removed,
        NewElement,
        AddedTwice,
        FakeRemoved,
        NewSlot,
    }

    internal readonly struct ArrayPropertyEnumerable : IEnumerable<SerializedProperty>
    {
        private readonly SerializedProperty _property;
        private readonly int _begin;
        private readonly int _end;

        public ArrayPropertyEnumerable(SerializedProperty property)
        {
            _property = property;
            _begin = 0;
            _end = property.arraySize;
        }

        private ArrayPropertyEnumerable(SerializedProperty property, int begin, int end)
        {
            _property = property;
            _begin = begin;
            _end = end;
        }

        public ArrayPropertyEnumerable Take(int count) =>
            new ArrayPropertyEnumerable(_property, _begin, Math.Min(_end, _begin + count));

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<SerializedProperty> IEnumerable<SerializedProperty>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<SerializedProperty>
        {
            private readonly SerializedProperty _property;
            private int _index;
            private int _size;

            public Enumerator(ArrayPropertyEnumerable enumerable)
            {
                _property = enumerable._property;
                _index = enumerable._begin - 1;
                _size = enumerable._end;
            }

            public SerializedProperty Current => _property.GetArrayElementAtIndex(_index);
            SerializedProperty IEnumerator<SerializedProperty>.Current => Current;
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                _index++;
                return _index < _size;
            }

            public void Reset() => throw new NotSupportedException();

            public void Dispose()
            {
            }
        }
    }
}
