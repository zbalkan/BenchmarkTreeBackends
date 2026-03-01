using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BenchmarkTreeBackends
{
    public sealed class LatencyRecorder
    {
        private long _count;
        private long _timeouts;
        private long _totalTicks;
        private long _under1us, _under10us, _under100us, _under1ms, _over1ms;
        public long CompletedOps => _count;

        public static LatencySummary Merge(string label, IEnumerable<LatencyRecorder> recorders, bool aborted = false)
        {
            long ops = 0, u1 = 0, u10 = 0, u100 = 0, u1k = 0, o1k = 0, timeouts = 0;
            double totalUs = 0;
            foreach (var r in recorders)
            {
                ops += r._count;
                u1 += r._under1us;
                u10 += r._under10us;
                u100 += r._under100us;
                u1k += r._under1ms;
                o1k += r._over1ms;
                timeouts += r._timeouts;
                totalUs += r._count == 0 ? 0
                    : r._totalTicks * 1_000_000.0 / Stopwatch.Frequency;
            }
            return new LatencySummary
            {
                Label = label,
                TotalOps = ops,
                MeanUs = ops == 0 ? 0 : totalUs / ops,
                Under1us = u1,
                Under10us = u10,
                Under100us = u100,
                Under1ms = u1k,
                Over1ms = o1k,
                Timeouts = timeouts,
                Aborted = aborted
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Record(long elapsedTicks)
        {
            _totalTicks += elapsedTicks;
            _count++;
            double us = elapsedTicks * 1_000_000.0 / Stopwatch.Frequency;
            if (us < 1) _under1us++;
            else if (us < 10) _under10us++;
            else if (us < 100) _under100us++;
            else if (us < 1000) _under1ms++;
            else _over1ms++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordTimeout() => _timeouts++;
    }
}