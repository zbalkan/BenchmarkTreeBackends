using BenchmarkDotNet.Attributes;
using BenchmarkTreeBackends.Backends;
using BenchmarkTreeBackends.Backends.ByteTree;
using BenchmarkTreeBackends.Backends.LMDB;
using BenchmarkTreeBackends.Backends.MMAP;
using BenchmarkTreeBackends.Backends.QP;
using BenchmarkTreeBackends.Codecs;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;

namespace BenchmarkTreeBackends
{

    [MemoryDiagnoser]
    [SimpleJob(warmupCount: HarnessConfig.WarmupIterations,
               iterationCount: HarnessConfig.BenchmarkIterations)]
    public class DomainTreeBenchmark
    {
        private IBenchmarkTarget<string> _adaptedDefaultTree;

        // Adapted wrappers — typed as the interface, not the concrete adapter
        private IBenchmarkTarget<string> _adaptedDict;

        private IBenchmarkTarget<string> _adaptedLmdb;

        private IBenchmarkTarget<string> _adaptedLmdb2;

        private IBenchmarkTarget<string> _adaptedMmap;

        private IBenchmarkTarget<string> _adaptedMmap2;

        private IBenchmarkTarget<string> _adaptedTrie;

        private IBenchmarkTarget<string> _adaptedTrieWire;

        // Raw backing stores
        private ConcurrentDictionary<string, string> _concurrentDict;

        private DatabaseBackedDomainTree<string> _dbBackedTree;
        private DatabaseBackedDomainTree<string> _dbBackedTree2;
        private DomainTree<string> _defaultTree;
        private DnsTrie<string> _dnsTrie;
        private DnsTrie<string> _dnsTrieWireFormat;
        private MmapBackedDomainTree<string> _mmapBackedTree;
        private MmapBackedDomainTree<string> _mmapBackedTree2;

        [GlobalCleanup]
        public void Cleanup()
        {
            _defaultTree.Clear();
            _dbBackedTree.Dispose();
            _dbBackedTree2.Dispose();
            _mmapBackedTree.Dispose();
            _mmapBackedTree2.Dispose();

            foreach (string? dir in new[] { "ct_lmdb1", "ct_lmdb2" })
                if (Directory.Exists(dir)) Directory.Delete(dir, true);

            foreach (string? file in new[] { "ct_mmap1", "ct_mmap2" })
                if (File.Exists(file)) File.Delete(file);
        }

        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Concurrent_ConcurrentDictionary()
            => ConcurrentBenchmarkRunner.Run("ConcurrentDictionary", _adaptedDict, "bench-value");

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Concurrent_DnsTrie()
            => ConcurrentBenchmarkRunner.Run("DnsTrie", _adaptedTrie, "bench-value");

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Concurrent_DnsTrieWireFormat()
            => ConcurrentBenchmarkRunner.Run("DnsTrie_Wire", _adaptedTrieWire, "bench-value");

       [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Concurrent_InMemoryDomainTree()
            => ConcurrentBenchmarkRunner.Run("InMemoryDomainTree", _adaptedDefaultTree, "bench-value");

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Concurrent_LmdbDomainTree_MessagePack()
            => ConcurrentBenchmarkRunner.Run("LmdbDomainTree_MP", _adaptedLmdb, "bench-value");

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Concurrent_LmdbDomainTree_Utf8()
            => ConcurrentBenchmarkRunner.Run("LmdbDomainTree_Utf8", _adaptedLmdb2, "bench-value");

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Concurrent_MmapDomainTree_MessagePack()
            => ConcurrentBenchmarkRunner.Run("MmapDomainTree_MP", _adaptedMmap, "bench-value");

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Concurrent_MmapDomainTree_Utf8()
            => ConcurrentBenchmarkRunner.Run("MmapDomainTree_Utf8", _adaptedMmap2, "bench-value");

        [GlobalSetup]
        public void Setup()
        {
            // Construct backing stores
            _concurrentDict = new ConcurrentDictionary<string, string>();
            _defaultTree = new DomainTree<string>();
            _dbBackedTree = new DatabaseBackedDomainTree<string>("ct_lmdb1", new MessagePackCodec<string>());
            _dbBackedTree2 = new DatabaseBackedDomainTree<string>("ct_lmdb2", new Utf8StringCodec());
            _mmapBackedTree = new MmapBackedDomainTree<string>("ct_mmap1", new MessagePackCodec<string>());
            _mmapBackedTree2 = new MmapBackedDomainTree<string>("ct_mmap2", new Utf8StringCodec());
            _dnsTrie = new DnsTrie<string>();
            _dnsTrieWireFormat = new DnsTrie<string>(wireFormat: true);

            // Seed all stores before creating adapters
            SeedAll();

            // Wire up adapters using the correct concrete type per backing store.
            // requiresExternalLock = true for any store without a documented
            // concurrent-safe contract; remove the lock once safety is confirmed.
            _adaptedDict = new ConcurrentDictionaryAdapter<string>(_concurrentDict);
            _adaptedDefaultTree = new BackendAdapter<string>(_defaultTree, requiresExternalLock: true);
            _adaptedLmdb = new BackendAdapter<string>(_dbBackedTree, requiresExternalLock: true);
            _adaptedLmdb2 = new BackendAdapter<string>(_dbBackedTree2, requiresExternalLock: true);
            _adaptedMmap = new BackendAdapter<string>(_mmapBackedTree, requiresExternalLock: true);
            _adaptedMmap2 = new BackendAdapter<string>(_mmapBackedTree2, requiresExternalLock: true);
            _adaptedTrie = new DnsTrieAdapter<string>(_dnsTrie, requiresExternalLock: true);
            _adaptedTrieWire = new DnsTrieAdapter<string>(_dnsTrieWireFormat, requiresExternalLock: true);
        }

        // ------------------------------------------------------------------
        // Seed helpers
        // ------------------------------------------------------------------

        private static string BuildDeepDomain()
        {
            string d = "a";
            for (int i = 0; i < 25; i++) d = $"{d}.a";
            return d + ".com";
        }

        private static void SeedBackend(IBackend<string, string> b)
        {
            b.TryAdd("com", "com-root");
            b.TryAdd("org", "org-root");
            foreach (string? sub in new[] { "google", "microsoft", "github", "example" })
            {
                b.TryAdd($"{sub}.com", sub);
                b.TryAdd($"www.{sub}.com", sub);
                b.TryAdd($"api.{sub}.com", sub);
                b.TryAdd($"mail.{sub}.com", sub);
            }
            b.TryAdd(BuildDeepDomain(), "deep");
        }

        private static void SeedDict(ConcurrentDictionary<string, string> d)
        {
            d.TryAdd("com", "com-root");
            d.TryAdd("org", "org-root");
            foreach (string? sub in new[] { "google", "microsoft", "github", "example" })
            {
                d.TryAdd($"{sub}.com", sub);
                d.TryAdd($"www.{sub}.com", sub);
                d.TryAdd($"api.{sub}.com", sub);
                d.TryAdd($"mail.{sub}.com", sub);
            }
            d.TryAdd(BuildDeepDomain(), "deep");
        }

        private static void SeedTrie(DnsTrie<string> t)
        {
            t.Set("com", "com-root");
            t.Set("org", "org-root");
            foreach (string? sub in new[] { "google", "microsoft", "github", "example" })
            {
                t.Set($"{sub}.com", sub);
                t.Set($"www.{sub}.com", sub);
                t.Set($"api.{sub}.com", sub);
                t.Set($"mail.{sub}.com", sub);
            }
            t.Set(BuildDeepDomain(), "deep");
        }

        private void SeedAll()
        {
            SeedBackend(_defaultTree);
            SeedBackend(_dbBackedTree);
            SeedBackend(_dbBackedTree2);
            SeedBackend(_mmapBackedTree);
            SeedBackend(_mmapBackedTree2);
            SeedDict(_concurrentDict);
            SeedTrie(_dnsTrie);
            SeedTrie(_dnsTrieWireFormat);
        }
    }
}