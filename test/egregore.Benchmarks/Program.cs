using BenchmarkDotNet.Running;

namespace egregore.Benchmarks
{
    internal static class Program
    {
        public static void Main(params string[] args)
        {
            //BenchmarkRunner.Run<SequenceBenchmarks>();
            //BenchmarkRunner.Run<LogStoreBenchmarks>();
            BenchmarkRunner.Run<SocketBenchmarks>();
        }
    }
}
