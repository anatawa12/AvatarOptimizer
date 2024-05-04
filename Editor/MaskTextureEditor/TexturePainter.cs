using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.MaskTextureEditor
{
    internal class TexturePainter : ScriptableObject
    {
        private readonly static int ColorPropertyId = Shader.PropertyToID("_Color");
        private readonly static int BrushLinePropertyId = Shader.PropertyToID("_BrushLine");
        private readonly static int BrushSizePropertyId = Shader.PropertyToID("_BrushSize");
        private readonly static int BrushColorPropertyId = Shader.PropertyToID("_BrushColor");

        [SerializeField]
        private float _brushSize = 100.0f;

        [SerializeField]
        private Color _brushColor = Color.black;

        [SerializeField]
        private RenderTexture _target = null;

        [SerializeField]
        private RenderTexture _buffer = null;

        [SerializeField]
        private Material _fillMaterial = null;

        [SerializeField]
        private Material _paintMaterial = null;

        [SerializeField]
        private Material _inverseMaterial = null;

        public float BrushSize { get => _brushSize; set => _brushSize = value; }
        public Color BrushColor { get => _brushColor; set => _brushColor = value; }
        public RenderTexture Texture => _target;
        public Vector2 TextureSize => new Vector2(_target.width, _target.height);

        private void Awake()
        {
            // Set HideFlags to keep the state when reloading a scene or domain
            hideFlags = HideFlags.HideAndDontSave;
        }

        public void Init(Texture2D texture)
        {
            Init(Vector2.zero);
            Load(texture);
        }

        public void Init(Vector2 size, Color color)
        {
            Init(size);
            Fill(color);
        }

        private void Init(Vector2 size)
        {
            // Set HideFlags to keep the state when reloading a scene or domain
            _target = new RenderTexture((int)size.x, (int)size.y, 0)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _buffer = new RenderTexture((int)size.x, (int)size.y, 0)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _fillMaterial = new Material(Shader.Find("Hidden/AvatarOptimizer/MaskTextureEditor/Fill"))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _paintMaterial = new Material(Shader.Find("Hidden/AvatarOptimizer/MaskTextureEditor/Paint"))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _inverseMaterial = new Material(Shader.Find("Hidden/AvatarOptimizer/MaskTextureEditor/Inverse"))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        public void Draw(Rect rect)
        {
            // Draw the texture
            GUI.DrawTexture(rect, _target);

            // Draw the brush
            Handles.color = GUI.color * _brushColor;
            Handles.matrix = Matrix4x4.TRS(
                Event.current.mousePosition,
                Quaternion.identity,
                _brushSize * rect.size / TextureSize);
            Handles.DrawSolidDisc(Vector3.zero, Vector3.forward, 0.5f);
            Handles.matrix = Matrix4x4.identity;
        }

        public void Load(Texture2D texture)
        {
            _target.Release();
            _target.width = texture.width;
            _target.height = texture.height;
            _target.Create();

            _buffer.Release();
            _buffer.width = texture.width;
            _buffer.height = texture.height;
            _buffer.Create();

            Graphics.Blit(texture, _target);
            RenderTexture.active = null;
        }

        public void Save(Texture2D texture)
        {
            RenderTexture.active = _target;

#if UNITY_2021_2_OR_NEWER
            texture.Reinitialize(_target.width, _target.height);
#else
            texture.Resize(_target.width, _target.height);
#endif
            texture.ReadPixels(new Rect(Vector2.zero, TextureSize), 0, 0);
            texture.Apply();

            RenderTexture.active = null;
        }

        public void Fill(Color color)
        {
            _fillMaterial.SetColor(ColorPropertyId, color);

            Graphics.Blit(_target, _buffer);
            Graphics.Blit(_buffer, _target, _fillMaterial);
            RenderTexture.active = null;
        }

        public void Paint(Vector2 positionA, Vector2 positionB)
        {
            _paintMaterial.SetVector(BrushLinePropertyId, new Vector4(
                positionA.x, TextureSize.y - positionA.y,
                positionB.x, TextureSize.y - positionB.y));
            _paintMaterial.SetFloat(BrushSizePropertyId, _brushSize);
            _paintMaterial.SetColor(BrushColorPropertyId, _brushColor);

            Graphics.Blit(_target, _buffer);
            Graphics.Blit(_buffer, _target, _paintMaterial);
            RenderTexture.active = null;
        }

        public void Inverse()
        {
            Graphics.Blit(_target, _buffer);
            Graphics.Blit(_buffer, _target, _inverseMaterial);
            RenderTexture.active = null;
        }

        private void OnDestroy()
        {
            if (_target != null)
            {
                DestroyImmediate(_target);
                _target = null;
            }
            if (_buffer != null)
            {
                DestroyImmediate(_buffer);
                _buffer = null;
            }
            if (_fillMaterial != null)
            {
                DestroyImmediate(_fillMaterial);
                _fillMaterial = null;
            }
            if (_paintMaterial != null)
            {
                DestroyImmediate(_paintMaterial);
                _paintMaterial = null;
            }
            if (_inverseMaterial != null)
            {
                DestroyImmediate(_inverseMaterial);
                _inverseMaterial = null;
            }
        }
    }
}
