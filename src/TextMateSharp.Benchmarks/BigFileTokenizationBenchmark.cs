using System;
using System.Collections.Generic;
using System.IO;

using BenchmarkDotNet.Attributes;

using TextMateSharp.Grammars;

namespace TextMateSharp.Benchmarks
{
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
            Console.WriteLine($"Loaded bigfile.cs");

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
    }
}
