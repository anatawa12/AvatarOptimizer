using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// The class manages Object location mapping
    /// </summary>
    internal class ObjectMappingBuilder
    {
        // Those may not be an actual GameObject, may be a hierarchy root 'null'
        private readonly VGameObject _tree = new VGameObject(null, "<root gameobject>");

        public void RecordMoveObject(GameObject from, GameObject newParent)
        {
            var oldPath = Utils.RelativePath(null, from.transform);
            var newParentPath = Utils.RelativePath(null, newParent.transform);

            var oldVGameObject = _tree.GetGameObject(oldPath);
            var newParentVGameObject = _tree.GetGameObject(newParentPath);

            oldVGameObject.MoveTo(newParentVGameObject);
        }

        /// <summary> Represents a GameObject in Hierarchy </summary>
        class VGameObject
        {
            private readonly VGameObject _originalParent;
            private readonly string _originalName;
            private VGameObject _newParent;
            private string _newName;

            private readonly Dictionary<string, VGameObject> _originalChildren = new Dictionary<string, VGameObject>();
            private readonly Dictionary<string, VGameObject> _newChildren = new Dictionary<string, VGameObject>();

            private readonly Dictionary<Type, VComponent> _originalComponents = new Dictionary<Type, VComponent>();
            private readonly Dictionary<Type, VComponent> _newComponents = new Dictionary<Type, VComponent>();

            public VGameObject(VGameObject parent, string name)
            {
                _originalParent = _newParent = parent;
                _originalName = _newName = name;
            }

            public VComponent GetComponent(Type type)
            {
                if (_newComponents.TryGetValue(type, out var component))
                    return component;
                return _newComponents[type] = _originalComponents[type] = new VComponent();
            }

            public VGameObject GetGameObject([NotNull] string path)
            {
                if (path == null) throw new ArgumentNullException(nameof(path));

                var cursor = this;
                foreach (var pathComponent in path.Split('/'))
                {
                    if (!cursor._newChildren.TryGetValue(pathComponent, out var child))
                    {
                        cursor._newChildren[pathComponent] =
                            cursor._originalChildren[pathComponent] =
                                child = new VGameObject(cursor, pathComponent);
                    }

                    cursor = child;
                }

                return cursor;
            }

            public void MoveTo(VGameObject newParent)
            {
                if (_newParent == newParent) return; // nothing to move
                System.Diagnostics.Debug.Assert(_newParent._newChildren.ContainsKey(_newName));
                System.Diagnostics.Debug.Assert(!newParent._newChildren.ContainsKey(_newName));
                _newParent._newChildren.Remove(_newName);
                newParent._newChildren[_newName] = this;
                _newParent = newParent;
            }
        }

        /// <summary> Represents a component </summary>
        class VComponent
        {
            private readonly Dictionary<string, VProperty> _originalProperties = new Dictionary<string, VProperty>();
            private readonly Dictionary<string, VProperty> _newProperties = new Dictionary<string, VProperty>();
        }

        /// <summary> Represents a property </summary>
        class VProperty
        {
        }
    }
}
