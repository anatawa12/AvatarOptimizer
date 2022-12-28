using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Anatawa12.Merger
{
    internal static class Utils
    {
        public static ArraySerializedPropertyEnumerable AsEnumerable(this SerializedProperty property) =>
            new ArraySerializedPropertyEnumerable(property);
    }

    internal struct ArraySerializedPropertyEnumerable : IEnumerable<SerializedProperty>
    {
        private readonly SerializedProperty _property;

        public ArraySerializedPropertyEnumerable(SerializedProperty property)
        {
            this._property = property;
        }

        Enumerator GetEnumerator() => new Enumerator(_property);

        private struct Enumerator : IEnumerator<SerializedProperty>
        {
            private int _index;
            private readonly SerializedProperty _property;

            public Enumerator(SerializedProperty property)
            {
                _index = -1;
                _property = property;
            }

            public bool MoveNext() => ++_index < _property.arraySize;

            public void Reset() => _index = -1;

            public SerializedProperty Current => _property.GetArrayElementAtIndex(_index);

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }

        IEnumerator<SerializedProperty> IEnumerable<SerializedProperty>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
