using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkTreeBackends
{
    public static class Program
    {
        public static void Main(string[] args)
        {
                BenchmarkRunner.Run<DomainTreeBenchmark>(
                    ManualConfig.Create(DefaultConfig.Instance)
                                .WithOptions(ConfigOptions.DisableOptimizationsValidator));
        }
    }
}