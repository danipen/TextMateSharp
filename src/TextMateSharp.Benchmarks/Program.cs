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

            if (args.Length > 1 && args[0] == "--profile")
            {
                var b = new DocsHeavyTokenizationBenchmark { Language = args[1] };
                b.Setup();
                foreach (var x in System.Linq.Enumerable.Take(System.Linq.Enumerable.OrderByDescending(TextMateSharp.Internal.Oniguruma.OnigScanner.Profile, x => x.Value[0]), 15))
                    Console.WriteLine($"{x.Value[0] / 20.0 / System.Diagnostics.Stopwatch.Frequency * 1000,8:F2} ms {x.Value[1],7} calls  {x.Key.Substring(0, Math.Min(150, x.Key.Length))}");
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
                    for (int i = 0; i < 20; i++)
                    {
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        bench.TokenizeFences();
                        best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
                    }
                    long s0 = TextMateSharp.Internal.Oniguruma.OnigScanner.Scanners, p0 = TextMateSharp.Internal.Oniguruma.OnigScanner.Patterns;
                    bench.TokenizeFences();
                    Console.WriteLine($"{language,-8} {best,8:F2} ms  {TextMateSharp.Internal.Oniguruma.OnigScanner.Scanners - s0} scanners, {TextMateSharp.Internal.Oniguruma.OnigScanner.Patterns - p0} patterns compiled per iteration");
                }
                return;
            }

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
