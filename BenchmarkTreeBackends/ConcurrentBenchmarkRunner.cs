using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace BenchmarkTreeBackends
{
    public static class ConcurrentBenchmarkRunner
    {
        public static LatencySummary Run<TValue>(
                string label,
                IBenchmarkTarget<TValue> adapter,
                TValue seedValue,
                int threadCount = HarnessConfig.ThreadCount,
                int opsPerThread = HarnessConfig.OpsPerThread,
                double writeFraction = HarnessConfig.WriteFraction)
        {
            var cts = new CancellationTokenSource();
            var barrier = new Barrier(threadCount);
            var recorders = Enumerable.Range(0, threadCount)
                                      .Select(_ => new LatencyRecorder())
                                      .ToArray();

            // Track raw Thread objects so the watchdog can Interrupt() managed waits.
            // For native waits we escalate to Environment.Exit().
            var threads = new Thread[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                int threadIdx = i;  // capture for closure
                threads[i] = new Thread(() =>
                {
                    var rng = new Random(threadIdx * 31 + 7);
                    var writeDomains = DomainPool.GenerateWriteDomains(
                                           threadIdx, (int)(opsPerThread * writeFraction) + 1);
                    var recorder = recorders[threadIdx];
                    int writeIdx = 0;

                    try
                    {
                        barrier.SignalAndWait();   // plain wait — watchdog handles timeout
                    }
                    catch (OperationCanceledException) { return; }

                    for (int op = 0; op < opsPerThread; op++)
                    {
                        if (cts.IsCancellationRequested) break;

                        bool isWrite = rng.NextDouble() < writeFraction;
                        long t0 = Stopwatch.GetTimestamp();

                        try
                        {
                            if (isWrite)
                                adapter.TryAdd(
                                    writeDomains[writeIdx++ % writeDomains.Length], seedValue);
                            else
                                adapter.TryGet(
                                    DomainPool.ReadDomains[rng.Next(DomainPool.ReadDomains.Length)],
                                    out _);

                            recorder.Record(Stopwatch.GetTimestamp() - t0);
                        }
                        catch (TimeoutException)
                        {
                            recorder.RecordTimeout();
                        }
                        catch (ThreadInterruptedException)
                        {
                            // Watchdog fired Interrupt() on this thread — stop cleanly.
                            Thread.ResetAbort();   // clear interrupted state in case CLR set it
                            break;
                        }
                    }
                })
                {
                    IsBackground = true,
                    Name = $"BenchThread-{i}"
                };
            }

            // -------------------------------------------------------------------
            // Watchdog — three-stage escalation
            // -------------------------------------------------------------------
            // Stage 1 (LockTimeout):   TryEnter inside adapter already threw TimeoutException.
            // Stage 2 (RunTimeout):    Cancel token + Interrupt() all threads — handles managed blocks.
            // Stage 3 (ExitGrace):     If threads still alive, the backend is stuck in native code.
            //                          Print diagnostics and call Environment.Exit(2).
            // -------------------------------------------------------------------

            var watchdog = new Thread(() =>
            {
                // Sleep until the wall-clock budget is exhausted.
                bool completedNormally = cts.Token.WaitHandle.WaitOne(HarnessConfig.RunTimeout);
                if (completedNormally) return;

                // Stage 2 — signal cancellation and interrupt every managed wait.
                Console.WriteLine(
                    $"\n  [WATCHDOG] {label}: {HarnessConfig.RunTimeout.TotalSeconds}s " +
                    $"wall-clock limit reached — signalling threads.");

                cts.Cancel();
                foreach (var t in threads)
                {
                    try { if (t.IsAlive) t.Interrupt(); }
                    catch { /* thread may have exited between check and interrupt */ }
                }

                // Give threads a short grace window to exit managed waits.
                Thread.Sleep(HarnessConfig.InterruptGrace);

                // Stage 3 — if any thread is still alive it is blocked in native code.
                int stuck = threads.Count(t => t.IsAlive);
                if (stuck > 0)
                {
                    Console.WriteLine(
                        $"  [WATCHDOG] {label}: {stuck} thread(s) stuck in native/unmanaged " +
                        $"code after interrupt grace — forcing process exit.\n" +
                        $"  Completed ops so far: " +
                        $"{recorders.Sum(r => r.CompletedOps):N0} / " +
                        $"{(long)threadCount * opsPerThread:N0}");

                    // Print whatever partial results we have so they are not lost.
                    var partial = LatencyRecorder.Merge(label + " [PARTIAL]", recorders, aborted: true);
                    Console.WriteLine(partial);

                    // Environment.Exit is the only way to unblock native-locked threads
                    // in .NET 5+. Thread.Abort() no longer exists.
                    Environment.Exit(2);
                }
            })
            {
                IsBackground = true,
                Name = "BenchWatchdog"
            };

            // Start everything
            watchdog.Start();
            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();

            // If all threads finished normally, cancel the watchdog's sleep.
            cts.Cancel();   // harmless if already cancelled

            bool timedOut = cts.IsCancellationRequested &&
                            recorders.Sum(r => r.CompletedOps) < (long)threadCount * opsPerThread;

            return LatencyRecorder.Merge(label, recorders, aborted: timedOut);
        }
    }
}