using BenchmarkDotNet.Running;
using System;
using System.IO;

namespace TextMateSharp.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--dump")
            {
                // The SHA-256 of every sample's tokens, optionally writing the tokens to a file.
                using var writer = args.Length > 1 ? new StreamWriter(args[1]) : null;
                Console.WriteLine(Samples.Dump(writer));
                return;
            }

            if (args.Length > 0 && args[0] == "--dump-bigfile")
            {
                // The SHA-256 of bigfile.cs's tokens with the C# grammar, line by line with the newline, as BigFileTokenizationBenchmark.
                Console.WriteLine(BigFileTokenizationBenchmark.Dump());
                return;
            }

            if (args.Length > 0 && args[0] == "--profile")
            {
                // Tokenizes a sample (or bigfile) repeatedly, outside BenchmarkDotNet, for a profiler to attach to.
                string target = args.Length > 1 ? args[1] : "bigfile";
                int iterations = args.Length > 2 ? int.Parse(args[2]) : 20;
                Func<int> run;
                if (target == "bigfile")
                {
                    var bigFile = new BigFileTokenizationBenchmark();
                    bigFile.Setup();
                    run = bigFile.TokenizeAllLines;
                }
                else
                {
                    var sample = new SamplesTokenizationBenchmark { Language = target };
                    sample.Setup();
                    run = sample.TokenizeSample;
                }
                run(); // compiles the grammar's rules
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                long allocated = GC.GetTotalAllocatedBytes(true);
                for (int i = 0; i < iterations; i++)
                {
                    run();
                }
                Console.WriteLine($"{target}: {stopwatch.Elapsed.TotalMilliseconds / iterations:F3} ms, {(GC.GetTotalAllocatedBytes(true) - allocated) / iterations:N0} B per iteration");
                return;
            }

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
