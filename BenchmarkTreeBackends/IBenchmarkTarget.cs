namespace BenchmarkTreeBackends
{
    public interface IBenchmarkTarget<TValue>
    {
        bool TryAdd(string key, TValue value);

        bool TryGet(string key, out TValue value);
    }
}