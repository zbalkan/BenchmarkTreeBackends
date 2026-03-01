using BenchmarkTreeBackends.Backends;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BenchmarkTreeBackends
{
    /// <summary>
    /// Adapts any IBackend. Optionally wraps operations in a ReaderWriterLockSlim
    /// for backends whose internal thread-safety is undocumented.
    /// </summary>
    public sealed class BackendAdapter<TValue> : IBenchmarkTarget<TValue>
        where TValue : class
    {
        private readonly IBackend<string, TValue> _backend;
        private readonly ReaderWriterLockSlim _rwLock;
        private readonly bool _useLock;

        public BackendAdapter(IBackend<string, TValue> backend, bool requiresExternalLock = false)
        {
            _backend = backend;
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
                try { return _backend.TryAdd(key, value); }
                finally { _rwLock.ExitWriteLock(); }
            }
            return _backend.TryAdd(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(string key, out TValue value)
        {
            if (_useLock)
            {
                if (!_rwLock.TryEnterReadLock(HarnessConfig.LockTimeout))
                    throw new TimeoutException($"TryGet read-lock timeout on key '{key}'");
                try { return _backend.TryGet(key, out value); }
                finally { _rwLock.ExitReadLock(); }
            }
            return _backend.TryGet(key, out value);
        }
    }
}