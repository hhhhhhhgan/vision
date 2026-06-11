using System.Collections.Generic;

namespace VisionFlow {
    public static class DictionaryExtensions
    {
        public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue : class
            => dict.TryGetValue(key, out var v) ? v : null;
        
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue) where TValue : struct
            => dict.TryGetValue(key, out var v) ? v : defaultValue;
    }
}