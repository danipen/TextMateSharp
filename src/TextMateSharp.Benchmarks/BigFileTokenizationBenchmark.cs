using System;
using System.IO;

using BenchmarkDotNet.Attributes;

using TextMateSharp.Grammars;

namespace TextMateSharp.Benchmarks
{
    [MemoryDiagnoser]
    public class BigFileTokenizationBenchmark
    {
        private IGrammar _grammar = null!;
        private string[] _lines = null!;

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
            _lines = File.ReadAllLines(bigFilePath);
            Console.WriteLine($"Loaded {_lines.Length} lines from bigfile.cs");

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

            for (int i = 0; i < _lines.Length; i++)
            {
                ITokenizeLineResult result = _grammar.TokenizeLine(_lines[i], ruleStack, TimeSpan.MaxValue);
                ruleStack = result.RuleStack;
                totalTokens += result.Tokens.Length;
            }

            return totalTokens;
        }
    }
}
