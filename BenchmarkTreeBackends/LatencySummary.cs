namespace BenchmarkTreeBackends
{
    public sealed class LatencySummary
    {
        public string Label { get; init; }
        public double MeanUs { get; init; }
        public long Over1ms { get; init; }
        public long TotalOps { get; init; }
        public long Under100us { get; init; }
        public long Under10us { get; init; }
        public long Under1ms { get; init; }
        public long Under1us { get; init; }
        public long Timeouts { get; init; }
        public bool Aborted { get; init; }

        public override string ToString()
        {
            string abortFlag = Aborted ? " [ABORTED]" : "";
            return
                $"[{Label,-40}] ops={TotalOps,12:N0}  mean={MeanUs,8:F2}µs  " +
                $"<1µs={Under1us,10:N0}  <10µs={Under10us,10:N0}  " +
                $"<100µs={Under100us,10:N0}  <1ms={Under1ms,8:N0}  " +
                $">=1ms={Over1ms,8:N0}  timeouts={Timeouts,6:N0}{abortFlag}";
        }
    }
}