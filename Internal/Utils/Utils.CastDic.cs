using System.Collections;
using System.Collections.Generic;

namespace Anatawa12.AvatarOptimizer
{
    partial class Utils
    {
        public static DicCaster<TValueCasted> CastDic<TValueCasted>()
            => new DicCaster<TValueCasted>();

        public struct DicCaster<TValueCasted>
        {
            public IReadOnlyDictionary<TKey, TValueCasted> CastedDic<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> self)
                where TValue : class, TValueCasted 
                => new CastedDictionary<TKey, TValue, TValueCasted>(self);
        }

        private sealed class CastedDictionary<TKey, TValue, TValueCasted> : IReadOnlyDictionary<TKey, TValueCasted>
            where TValue : class, TValueCasted
        {
            private readonly IReadOnlyDictionary<TKey, TValue> _base;
            public CastedDictionary(IReadOnlyDictionary<TKey, TValue> @base) => _base = @base;

            public IEnumerator<KeyValuePair<TKey, TValueCasted>> GetEnumerator()
            {
                foreach (var (key, value) in _base)
                    yield return new KeyValuePair<TKey, TValueCasted>(key, value);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public int Count => _base.Count;
            public bool ContainsKey(TKey key) => _base.ContainsKey(key);
            public TValueCasted this[TKey key] => _base[key];
            public IEnumerable<TKey> Keys => _base.Keys;
            public IEnumerable<TValueCasted> Values => _base.Values;

            public bool TryGetValue(TKey key, out TValueCasted value)
            {
                var result = _base.TryGetValue(key, out var original);
                value = original;
                return result;
            }
        }
    }
}
