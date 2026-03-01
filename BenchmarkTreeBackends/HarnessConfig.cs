using System;

namespace BenchmarkTreeBackends
{
    public static class HarnessConfig
    {
        public const int ThreadCount = 100;
        public const int OpsPerThread = 100_000;
        public const double WriteFraction = 0.20;
        public const int WarmupIterations = 3;
        public const int BenchmarkIterations = 5;

        /// <summary>Per-operation lock acquisition timeout inside the adapters.</summary>
        public static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(5);

        /// <summary>Wall-clock budget for the entire concurrent run before the watchdog fires.</summary>
        public static readonly TimeSpan RunTimeout = TimeSpan.FromSeconds(300);

        /// <summary>
        /// Grace period after Interrupt() before the watchdog declares a native deadlock
        /// and calls Environment.Exit(). Long enough for managed waits to unwind,
        /// short enough to avoid sitting here if the backend is truly stuck.
        /// </summary>
        public static readonly TimeSpan InterruptGrace = TimeSpan.FromSeconds(5);
    }
}