using System.Collections.Generic;
using nadena.dev.ndmf.preview;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.MaskTextureEditor
{
    internal class TextureUndoStack : ScriptableObject
    {
        private class Counter : ScriptableObject
        {
            [SerializeField]
            private int _count = 0;

            public int Count { get => _count; set => _count = value; }

            private void Awake()
            {
                // Set HideFlags to keep the state when reloading a scene or domain
                hideFlags = HideFlags.HideAndDontSave;
            }
        }

        [SerializeField]
        private RenderTexture _target = null!; // initialized by Init

        [SerializeField]
        private List<Texture2D> _stack = null!; // initialized by Init

        [SerializeField]
        private Counter _counter = null!; // initialized by Init

        public bool CanUndo => _counter.Count > 1;
        public bool CanRedo => _counter.Count < _stack.Count;

        private void Awake()
        {
            // Set HideFlags to keep the state when reloading a scene or domain
            hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnEnable()
        {
            UnityEditor.Undo.undoRedoPerformed += Apply;
        }

        private void OnDisable()
        {
            UnityEditor.Undo.undoRedoPerformed -= Apply;
        }

        public void Init(RenderTexture texture)
        {
            _target = texture;
            _stack = new List<Texture2D>();
            _counter = CreateInstance<Counter>();

            Record();
        }

        public void Record()
        {
            for (var i = _counter.Count; i < _stack.Count; i++)
            {
                if (_stack[i] != null)
                {
                    DestroyImmediate(_stack[i]);
                }
            }
            _stack.RemoveRange(_counter.Count, _stack.Count - _counter.Count);

            // Set HideFlags to keep the state when reloading a scene or domain
            var texture = new Texture2D(_target.width, _target.height)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };

            RenderTexture.active = _target;
            texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            _stack.Add(texture);

            if (_counter.Count > 0)
            {
                UnityEditor.Undo.RecordObject(_counter, "Modify Texture");
            }
            _counter.Count++;
        }

        public void Undo()
        {
            if (CanUndo)
            {
                UnityEditor.Undo.RecordObject(_counter, "Undo Texture");
                _counter.Count--;

                Apply();
            }
        }

        public void Redo()
        {
            if (CanRedo)
            {
                UnityEditor.Undo.RecordObject(_counter, "Redo Texture");
                _counter.Count++;

                Apply();
            }
        }

        public Texture2D Peek()
        {
            return _stack[_counter.Count - 1];
        }

        public Texture2D ObservePeek(ComputeContext context)
        {
            var count = context.Observe(_counter, c => c.Count);
            return _stack[count - 1];
        }

        private void Apply()
        {
            Graphics.Blit(_stack[_counter.Count - 1], _target);
            RenderTexture.active = null;
        }

        private void OnDestroy()
        {
            if (_stack != null)
            {
                foreach (var texture in _stack)
                {
                    if (texture != null)
                    {
                        DestroyImmediate(texture);
                    }
                }
                _stack = null!; // resetting
            }
            if (_counter != null)
            {
                DestroyImmediate(_counter);
                _counter = null!; // resetting
            }
        }
    }
}
