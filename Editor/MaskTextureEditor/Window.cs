using System;
using System.IO;
using JetBrains.Annotations;
using nadena.dev.ndmf.preview;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.MaskTextureEditor
{
    internal class Window : EditorWindow
    {
        private static class Styles
        {
            public readonly static GUIStyle Toolbar = new GUIStyle("Toolbar")
            {
                fixedHeight = 0.0f,
            };
            public readonly static GUIStyle Button = new GUIStyle("button")
            {
                fixedHeight = 24.0f,
            };
        }

        private static class Events
        {
            public static bool Paint => Event.current.button == 0
                && !Event.current.alt
                && !Event.current.shift
                && !Event.current.control
                && !Event.current.command;
            public static bool BrushSize => Event.current.shift;
        }

        private const float ViewScaleMin = 0.1f;
        private const float ViewScaleMax = 10.0f;
        private const float ViewScaleFactor = 0.1f;
        private const float BrushSizeMin = 10.0f;
        private const float BrushSizeMax = 1000.0f;
        private const float BrushSizeFactor = 0.1f;

        [SerializeField]
        private SkinnedMeshRenderer _renderer = null!; // Initialized by Open

        [SerializeField]
        private int _subMesh = 0;

        [SerializeField]
        private Texture2D? _texture = null;

        [SerializeField]
        private Vector2 _viewPosition = Vector2.zero;

        [SerializeField]
        private float _viewScale = 1.0f;

        [SerializeField]
        private float _viewOpacity = 0.5f;

        [SerializeField]
        private bool _requestResetView = true;

        [SerializeField]
        private UvMapDrawer _uvMapDrawer = null!; // Initialized by Open

        [SerializeField]
        private TexturePainter _texturePainter = null!; // Initialized by Open

        [SerializeField]
        private TextureUndoStack _textureUndoStack = null!; // Initialized by Open

        [SerializeField]
        private int _previewTextureInstanceIdWhenSaved = 0;

        private static PublishedValue<bool> IsWindowOpen = new PublishedValue<bool>(false);

        public static Window? Instance
        {
            get
            {
                if (!HasOpenInstances<Window>())
                {
                    return null;
                }

                return GetWindow<Window>(string.Empty, false);
            }
        }

        public static Texture2D? ObservePreviewTextureFor(SkinnedMeshRenderer renderer, int subMesh, ComputeContext context)
        {
            if (!context.Observe(IsWindowOpen)) return null;

            var window = GetWindow<Window>(string.Empty, false);
            var targetMatches = context.Observe(window, w => w._renderer == renderer && w._subMesh == subMesh);
            if (!targetMatches) return null;

            return window._textureUndoStack.ObservePeek(context);
        }

        public Texture2D PreviewTexture => _textureUndoStack.Peek();

        public static bool IsOpen(SkinnedMeshRenderer renderer)
        {
            if (!HasOpenInstances<Window>())
            {
                return false;
            }

            var window = GetWindow<Window>(string.Empty, false);
            return window._renderer == renderer;
        }

        public static bool IsOpen(SkinnedMeshRenderer renderer, int subMesh)
        {
            if (!HasOpenInstances<Window>())
            {
                return false;
            }

            var window = GetWindow<Window>(string.Empty, false);
            return window._renderer == renderer && window._subMesh == subMesh;
        }

        public void Open(SkinnedMeshRenderer renderer, int subMesh, Texture2D texture)
        {
            Undo.RecordObject(this, "Open Mask Texture Editor");
            _renderer = renderer;
            _subMesh = subMesh;
            _texture = texture;

            _uvMapDrawer = CreateInstance<UvMapDrawer>();
            _uvMapDrawer.Init(renderer, subMesh);

            _texturePainter = CreateInstance<TexturePainter>();
            _texturePainter.Init(texture);

            _textureUndoStack = CreateInstance<TextureUndoStack>();
            _textureUndoStack.Init(_texturePainter.Texture);

            _previewTextureInstanceIdWhenSaved = PreviewTexture.GetInstanceID();
        }

        public void SafeClose()
        {
            if (hasUnsavedChanges)
            {
                switch (EditorUtility.DisplayDialogComplex(
                    AAOL10N.Tr("MaskTextureEditor:title"),
                    saveChangesMessage,
                    AAOL10N.Tr("MaskTextureEditor:saveChangesButtonSave"),
                    AAOL10N.Tr("MaskTextureEditor:saveChangesButtonCancel"),
                    AAOL10N.Tr("MaskTextureEditor:saveChangesButtonDiscard")))
                {
                    case 0:
                    {
                        SaveChanges();
                        Close();
                        break;
                    }
                    case 2:
                    {
                        Close();
                        break;
                    }
                }
            }
            else
            {
                Close();
            }
        }

        private void OnGUI()
        {
            wantsMouseMove = true;
            titleContent.text = AAOL10N.Tr("MaskTextureEditor:title");
            hasUnsavedChanges = _previewTextureInstanceIdWhenSaved != PreviewTexture.GetInstanceID();
            saveChangesMessage = AAOL10N.Tr("MaskTextureEditor:saveChangesMessage");

            if (_renderer == null ||
                _renderer.sharedMesh == null ||
                _subMesh >= _renderer.sharedMesh.subMeshCount ||
                _texture == null)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(Styles.Toolbar))
            {
                DrawToolbar();
            }

            var scrollRect = GUILayoutUtility.GetRect(
                0.0f,
                0.0f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            var viewportRect = new Rect(
                scrollRect.position.x,
                scrollRect.position.y,
                scrollRect.size.x - GUI.skin.verticalScrollbar.fixedWidth,
                scrollRect.size.y - GUI.skin.horizontalScrollbar.fixedHeight);

            // Add margins of half of the viewport on each side to make it easier to edit the edges of the texture
            var viewRect = new Rect(Vector2.zero, _texturePainter.TextureSize * _viewScale + viewportRect.size);
            var contentRect = new Rect(viewportRect.size * 0.5f, _texturePainter.TextureSize * _viewScale);

            using (var scroll = new GUI.ScrollViewScope(scrollRect, _viewPosition, viewRect, true, true)
            {
                handleScrollWheel = false,
            })
            {
                _viewPosition = scroll.scrollPosition;

                DrawContents(contentRect);
            }

            HandleEvents(viewportRect);
        }

        private void DrawToolbar()
        {
            AAOL10N.DrawLanguagePicker();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    AAOL10N.Tr("MaskTextureEditor:renderer"),
                    _renderer, typeof(SkinnedMeshRenderer), true);

                EditorGUILayout.LabelField(
                    AAOL10N.Tr("MaskTextureEditor:subMesh"),
                    string.Format(AAOL10N.Tr("MaskTextureEditor:subMeshIndex"), _subMesh));

                EditorGUILayout.ObjectField(
                    AAOL10N.Tr("MaskTextureEditor:texture"),
                    _texture, typeof(Texture2D), true, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            if (GUILayout.Button(AAOL10N.Tr("MaskTextureEditor:save"), Styles.Button))
            {
                SaveChanges();
            }

            EditorGUILayout.Space();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var prev = _viewScale;
                _viewScale = EditorGUILayout.Slider(
                    AAOL10N.Tr("MaskTextureEditor:viewScale"),
                    _viewScale, ViewScaleMin, ViewScaleMax);
                if (check.changed)
                {
                    _viewPosition -= _viewPosition * (1.0f - _viewScale / prev);
                }
            }

            _viewOpacity = EditorGUILayout.Slider(
                AAOL10N.Tr("MaskTextureEditor:viewOpacity"),
                _viewOpacity, 0.0f, 1.0f);

            if (GUILayout.Button(AAOL10N.Tr("MaskTextureEditor:resetView"), Styles.Button))
            {
                _requestResetView = true;
            }

            EditorGUILayout.Space();

            _texturePainter.BrushSize = EditorGUILayout.Slider(
                AAOL10N.Tr("MaskTextureEditor:brushSize"),
                _texturePainter.BrushSize, BrushSizeMin, BrushSizeMax);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(AAOL10N.Tr("MaskTextureEditor:brushColor"));

                var colors = new[]
                {
                    Color.black,
                    Color.white,
                };
                var texts = new[]
                {
                    AAOL10N.Tr("MaskTextureEditor:black"),
                    AAOL10N.Tr("MaskTextureEditor:white"),
                };
                var index = Array.IndexOf(colors, _texturePainter.BrushColor);
                _texturePainter.BrushColor = colors[GUILayout.Toolbar(index, texts)];
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!_textureUndoStack.CanUndo))
                {
                    if (GUILayout.Button(EditorGUIUtility.TrIconContent("tab_prev"),
                        Styles.Button, GUILayout.ExpandWidth(false)))
                    {
                        _textureUndoStack.Undo();
                    }
                }
                using (new EditorGUI.DisabledScope(!_textureUndoStack.CanRedo))
                {
                    if (GUILayout.Button(EditorGUIUtility.TrIconContent("tab_next"),
                        Styles.Button, GUILayout.ExpandWidth(false)))
                    {
                        _textureUndoStack.Redo();
                    }
                }
                if (GUILayout.Button(AAOL10N.Tr("MaskTextureEditor:fillBlack"), Styles.Button))
                {
                    _texturePainter.Fill(Color.black);
                    _textureUndoStack.Record();
                }
                if (GUILayout.Button(AAOL10N.Tr("MaskTextureEditor:fillWhite"), Styles.Button))
                {
                    _texturePainter.Fill(Color.white);
                    _textureUndoStack.Record();
                }
                if (GUILayout.Button(AAOL10N.Tr("MaskTextureEditor:inverse"), Styles.Button))
                {
                    _texturePainter.Inverse();
                    _textureUndoStack.Record();
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawContents(Rect rect)
        {
            GUI.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            _uvMapDrawer.Draw(rect);

            GUI.color = new Color(1.0f, 1.0f, 1.0f, _viewOpacity);
            _texturePainter.Draw(rect);
        }

        private void HandleEvents(Rect rect)
        {
            var control = GUIUtility.GetControlID(FocusType.Passive);
            var type = Event.current.GetTypeForControl(control);
            switch (type)
            {
                case EventType.Repaint:
                {
                    if (_requestResetView)
                    {
                        // Fit the view to the window
                        _viewScale = Mathf.Min(
                            rect.size.x / _texturePainter.TextureSize.x,
                            rect.size.y / _texturePainter.TextureSize.y);
                        _viewScale = Mathf.Clamp(_viewScale, ViewScaleMin, ViewScaleMax);
                        _viewPosition = _texturePainter.TextureSize * _viewScale * 0.5f;
                        _requestResetView = false;
                        Repaint();
                    }
                    break;
                }
                case EventType.MouseMove when rect.Contains(Event.current.mousePosition):
                {
                    Repaint();
                    break;
                }
                case EventType.MouseDown when rect.Contains(Event.current.mousePosition):
                {
                    // Set HotControl to continue dragging outside of the window
                    GUIUtility.hotControl = control;

                    if (Events.Paint)
                    {
                        // Paint the texture
                        var position = Event.current.mousePosition - rect.center + _viewPosition;
                        _texturePainter.Paint(position / _viewScale, position / _viewScale);
                        Repaint();
                    }
                    break;
                }
                case EventType.MouseUp when GUIUtility.hotControl == control:
                {
                    // Unset HotControl to allow other controls to respond
                    GUIUtility.hotControl = 0;

                    if (Events.Paint)
                    {
                        _textureUndoStack.Record();
                        Repaint();
                    }
                    break;
                }
                case EventType.MouseDrag when GUIUtility.hotControl == control:
                {
                    var delta = Event.current.delta;
                    if (Events.Paint)
                    {
                        // Paint the texture
                        var position = Event.current.mousePosition - rect.center + _viewPosition;
                        _texturePainter.Paint((position - delta) / _viewScale, position / _viewScale);
                        Repaint();
                    }
                    else
                    {
                        // Move the view
                        _viewPosition -= delta;
                        Repaint();
                    }
                    break;
                }
                case EventType.ScrollWheel when rect.Contains(Event.current.mousePosition):
                {
                    var delta = Event.current.delta.x + Event.current.delta.y;
                    if (Events.BrushSize)
                    {
                        // Adjust the brush size
                        _texturePainter.BrushSize *= 1.0f - delta * BrushSizeFactor;
                        _texturePainter.BrushSize = Mathf.Clamp(_texturePainter.BrushSize, BrushSizeMin, BrushSizeMax);
                        Repaint();
                    }
                    else
                    {
                        // Scale the view around the mouse position
                        var prev = _viewScale;
                        _viewScale *= 1.0f - delta * ViewScaleFactor;
                        _viewScale = Mathf.Clamp(_viewScale, ViewScaleMin, ViewScaleMax);
                        var position = Event.current.mousePosition - rect.center + _viewPosition;
                        _viewPosition -= position * (1.0f - _viewScale / prev);
                        Repaint();
                    }
                    break;
                }
                case EventType.ValidateCommand:
                {
                    Repaint();
                    break;
                }
            }

            // Ensure non-negative position to avoid scrollbars flickering
            _viewPosition = Vector2.Max(_viewPosition, Vector2.zero);
        }

        public override void SaveChanges()
        {
            base.SaveChanges();
            var path = AssetDatabase.GetAssetPath(_texture);

            var texture = new Texture2D(0, 0);
            _texturePainter.Save(texture);

            try
            {
                File.WriteAllBytes(path, texture.EncodeToPNG());

                AssetDatabase.ImportAsset(path);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.isReadable = true;
                importer.SaveAndReimport();

                _previewTextureInstanceIdWhenSaved = PreviewTexture.GetInstanceID();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    AAOL10N.Tr("MaskTextureEditor:errorTitle"),
                    AAOL10N.Tr("MaskTextureEditor:errorMessageSaveFailed"),
                    "OK");

                Debug.LogError(e);
            }
            finally
            {
                DestroyImmediate(texture);
            }
        }

        private void Awake()
        {
            IsWindowOpen.Value = true;
        }

        private void OnDestroy()
        {
            IsWindowOpen.Value = false;
            if (_uvMapDrawer != null)
            {
                DestroyImmediate(_uvMapDrawer);
                _uvMapDrawer = null!; // reset
            }
            if (_texturePainter != null)
            {
                DestroyImmediate(_texturePainter);
                _texturePainter = null!; // reset
            }
            if (_textureUndoStack != null)
            {
                DestroyImmediate(_textureUndoStack);
                _textureUndoStack = null!; // reset
            }
        }
    }
}
