using System.Collections.Generic;
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
        private RenderTexture _target = null;

        [SerializeField]
        private List<RenderTexture> _stack = null;

        [SerializeField]
        private Counter _counter = null;

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
            _stack = new List<RenderTexture>();
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

            var copy = new RenderTexture(_target.width, _target.height, 0);
            Graphics.Blit(_target, copy);
            RenderTexture.active = null;

            _stack.Add(copy);

            if (_counter.Count > 0)
            {
                UnityEditor.Undo.RegisterCompleteObjectUndo(_counter, "Modify Texture");
            }
            _counter.Count++;
        }

        public void Undo()
        {
            if (CanUndo)
            {
                // Clear Undo as it will be inconsistent with the counter
                // There might be a better way
                UnityEditor.Undo.ClearUndo(_counter);

                _counter.Count--;

                Apply();
            }
        }

        public void Redo()
        {
            if (CanRedo)
            {
                // Clear Undo as it will be inconsistent with the counter
                // There might be a better way
                UnityEditor.Undo.ClearUndo(_counter);

                _counter.Count++;

                Apply();
            }
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
                _stack = null;
            }
            if (_counter != null)
            {
                DestroyImmediate(_counter);
                _counter = null;
            }
        }
    }
}
