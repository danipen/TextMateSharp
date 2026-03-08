using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Metrology;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TextMateSharp.Grammars;

namespace TextMateSharp.Benchmarks
{
    [Config(typeof(CustomBenchmarksConfig))]
    [MemoryDiagnoser]
    public class BigFileTokenizationBenchmark
    {
        private IGrammar _grammar = null!;
        private string _content = null!;

        [GlobalSetup]
        public void Setup()
        {
            // Walk up directories to find the solution root
            string? dir = AppDomain.CurrentDomain.BaseDirectory;
            string bigFilePath = "";

            while (dir != null)
            {
                string candidate = Path.Combine(dir, "src", "TextMateSharp.Demo",
                    "testdata", "samplefiles", "bigfile.cs");
                if (File.Exists(candidate))
                {
                    bigFilePath = candidate;
                    break;
                }
                dir = Path.GetDirectoryName(dir);
            }

            if (string.IsNullOrEmpty(bigFilePath) || !File.Exists(bigFilePath))
            {
                throw new FileNotFoundException(
                    "Could not find bigfile.cs. Make sure you're running from the TextMateSharp solution directory.");
            }


            // Load the file into memory
            _content = File.ReadAllText(bigFilePath);
            Console.WriteLine("Loaded bigfile.cs");

            // Load the C# grammar
            RegistryOptions options = new RegistryOptions(ThemeName.DarkPlus);
            Registry.Registry registry = new Registry.Registry(options);
            _grammar = registry.LoadGrammar("source.cs");

            if (_grammar == null)
            {
                throw new InvalidOperationException("Failed to load C# grammar");
            }
        }

        [Benchmark]
        public int TokenizeAllLines()
        {
            int totalTokens = 0;
            IStateStack? ruleStack = null;

            ReadOnlyMemory<char> contentMemory = _content.AsMemory();

            foreach (var lineRange in GetLineRanges(_content))
            {
                ReadOnlyMemory<char> lineMemory = contentMemory.Slice(lineRange.Start, lineRange.Length);
                ITokenizeLineResult result = _grammar.TokenizeLine(lineMemory, ruleStack, TimeSpan.MaxValue);
                ruleStack = result.RuleStack;
                totalTokens += result.Tokens.Length;
            }

            return totalTokens;
        }

        static IEnumerable<(int Start, int Length)> GetLineRanges(string content)
        {
            int lineStart = 0;

            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == '\n')
                {
                    int lineLength = i - lineStart + 1; // Include the \n
                    yield return (lineStart, lineLength);
                    lineStart = i + 1;
                }
            }

            // Handle last line without terminator
            if (lineStart < content.Length)
            {
                yield return (lineStart, content.Length - lineStart);
            }
        }

        #region helper classes for benchmarks

        public sealed class CustomBenchmarksConfig : ManualConfig
        {
            public CustomBenchmarksConfig()
            {
                // Use the default summary style with size unit in kilobytes.
                // We have a separate column to measure in bytes so we can measure even small differences in memory usage.
                SummaryStyle = SummaryStyle.Default
                    .WithSizeUnit(SizeUnit.KB)
                    .WithCultureInfo(CultureInfo.CurrentCulture);

                AddColumn(new AllocatedBytesColumn());
            }
        }

        public sealed class AllocatedBytesColumn : IColumn
        {
            public string Id => nameof(AllocatedBytesColumn);

            public string ColumnName => "Allocated B";

            public bool AlwaysShow => true;

            public ColumnCategory Category => ColumnCategory.Custom;

            public int PriorityInCategory => 0;

            public bool IsNumeric => true;

            public UnitType UnitType => UnitType.Dimensionless;

            public string Legend => "Bytes allocated per operation";

            public bool IsAvailable(Summary summary) => true;

            public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

            public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
            {
                BenchmarkReport? report = summary[benchmarkCase];
                long? bytesAllocatedPerOperation = report?.GcStats.GetBytesAllocatedPerOperation(benchmarkCase);
                if (!bytesAllocatedPerOperation.HasValue)
                {
                    return "NA";
                }

                return bytesAllocatedPerOperation.Value.ToString("N0", style.CultureInfo);
            }

            public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
                => GetValue(summary, benchmarkCase, summary.Style);

            public override string ToString() => ColumnName;
        }

        #endregion helper classes for benchmarks
    }
}
