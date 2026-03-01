using BenchmarkTreeBackends.Backends.QP;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BenchmarkTreeBackends
{
    /// <summary>
    /// Wraps DnsTrie behind IBenchmarkTarget. A ReaderWriterLockSlim is applied by
    /// default because DnsTrie's concurrency contract is not publicly documented.
    /// Set requiresExternalLock = false once concurrent safety is confirmed.
    /// </summary>
    public sealed class DnsTrieAdapter<TValue> : IBenchmarkTarget<TValue>
        where TValue : class
    {
        private readonly ReaderWriterLockSlim _rwLock;
        private readonly DnsTrie<TValue> _trie;
        private readonly bool _useLock;

        public DnsTrieAdapter(DnsTrie<TValue> trie, bool requiresExternalLock = true)
        {
            _trie = trie;
            _useLock = requiresExternalLock;
            _rwLock = requiresExternalLock ? new ReaderWriterLockSlim() : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(string key, TValue value)
        {
            if (_useLock)
            {
                if (!_rwLock.TryEnterWriteLock(HarnessConfig.LockTimeout))
                    throw new TimeoutException($"TryAdd write-lock timeout on key '{key}'");
                try { _trie.Set(key, value); return true; }
                finally { _rwLock.ExitWriteLock(); }
            }
            _trie.Set(key, value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(string key, out TValue value)
        {
            if (_useLock)
            {
                if (!_rwLock.TryEnterReadLock(HarnessConfig.LockTimeout))
                    throw new TimeoutException($"TryGet read-lock timeout on key '{key}'");
                try { return _trie.TryGet(key, out value); }
                finally { _rwLock.ExitReadLock(); }
            }
            return _trie.TryGet(key, out value);
        }
    }
}