using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace BenchmarkTreeBackends
{
    /// <summary>
    /// ConcurrentDictionary is natively thread-safe; no external lock is needed.
    /// </summary>
    public sealed class ConcurrentDictionaryAdapter<TValue> : IBenchmarkTarget<TValue>
    {
        private readonly ConcurrentDictionary<string, TValue> _dict;

        public ConcurrentDictionaryAdapter(ConcurrentDictionary<string, TValue> dict)
            => _dict = dict;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(string key, TValue value)
            => _dict.TryAdd(key, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(string key, out TValue value)
            => _dict.TryGetValue(key, out value);
    }
}