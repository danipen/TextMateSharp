using BenchmarkDotNet.Running;

namespace TextMateSharp.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<BigFileTokenizationBenchmark>();
        }
    }
}
