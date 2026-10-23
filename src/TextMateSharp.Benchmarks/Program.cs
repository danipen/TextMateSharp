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
                using var writer = args.Length > 1 ? new StreamWriter(args[1]) : null;
                Console.WriteLine(DocsHeavyCorpus.Dump(writer));
                return;
            }

            if (args.Length > 1 && args[0] == "--trace")
            {
                // Records the scanner calls of tokenizing the benchmark fences, per language.
                var trace = new System.Collections.Generic.Dictionary<string, object>();
                foreach (string language in new[] { "bash", "ts", "js", "html", "json", "yaml", "csharp" })
                {
                    var b = new DocsHeavyTokenizationBenchmark { Language = language };
                    b.Setup();
                    TextMateSharp.Internal.Oniguruma.OnigScanner.TraceScanners = new();
                    TextMateSharp.Internal.Oniguruma.OnigScanner.TraceLines = new();
                    TextMateSharp.Internal.Oniguruma.OnigScanner.TraceCalls = new();
                    b.TokenizeFences();
                    var lines = new string[TextMateSharp.Internal.Oniguruma.OnigScanner.TraceLines.Count];
                    foreach (var kv in TextMateSharp.Internal.Oniguruma.OnigScanner.TraceLines) lines[kv.Value] = kv.Key;
                    trace[language] = new { scanners = TextMateSharp.Internal.Oniguruma.OnigScanner.TraceScanners, lines, calls = TextMateSharp.Internal.Oniguruma.OnigScanner.TraceCalls };
                    Console.WriteLine($"{language}: {TextMateSharp.Internal.Oniguruma.OnigScanner.TraceScanners.Count} scanners, {lines.Length} lines, {TextMateSharp.Internal.Oniguruma.OnigScanner.TraceCalls.Count} calls");
                    TextMateSharp.Internal.Oniguruma.OnigScanner.TraceCalls = null;
                }
                File.WriteAllText(args[1], System.Text.Json.JsonSerializer.Serialize(trace));
                return;
            }

            if (args.Length > 0 && args[0] == "--time")
            {
                // Tokenizes every fence once per iteration and prints the best of N, for quick comparisons.
                var bench = new DocsHeavyTokenizationBenchmark();
                foreach (string language in new[] { "bash", "ts", "js", "html", "json", "yaml", "csharp" })
                {
                    bench.Language = language;
                    bench.Setup();
                    double best = double.MaxValue;
                    long allocated = 0;
                    for (int i = 0, n = int.Parse(Environment.GetEnvironmentVariable("TM_ITER") ?? "20"); i < n; i++)
                    {
                        long before = GC.GetAllocatedBytesForCurrentThread();
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        bench.TokenizeFences();
                        best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
                        allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                    }
                    Console.WriteLine($"{language,-8} {best,8:F2} ms {allocated / 1024.0,10:F0} KB");
                }
                return;
            }

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
