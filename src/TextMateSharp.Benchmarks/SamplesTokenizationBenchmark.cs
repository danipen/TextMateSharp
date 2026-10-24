using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TextMateSharp.Grammars;

namespace TextMateSharp.Benchmarks
{
    // Tokenizes a language's sample (src/TextMateSharp.WasmDemo/wwwroot/samples) with the bundled grammars, line by line
    // and without the newline, as an editor or a documentation generator highlights a code block.
    [MemoryDiagnoser]
    public class SamplesTokenizationBenchmark
    {
        private IGrammar _grammar = null!;
        private string[] _lines = null!;

        [Params("shellscript", "typescript", "javascript", "html", "json", "yaml", "csharp")]
        public string Language { get; set; } = "";

        [GlobalSetup]
        public void Setup()
        {
            var samples = new Samples();
            _grammar = samples.LoadGrammar(Language);
            _lines = samples.Lines(Language);
        }

        [Benchmark]
        public int TokenizeSample()
        {
            int tokens = 0;
            IStateStack? state = null;
            foreach (string line in _lines)
            {
                ITokenizeLineResult result = _grammar.TokenizeLine(line, state, TimeSpan.MaxValue);
                state = result.RuleStack;
                tokens += result.Tokens.Length;
            }
            return tokens;
        }
    }

    // The per-language samples and the bundled grammars that highlight them.
    public sealed class Samples
    {
        private readonly RegistryOptions _options = new(ThemeName.DarkPlus);
        private readonly Registry.Registry _registry;

        public Samples()
        {
            _registry = new Registry.Registry(_options);
            Directory = Find();
        }

        public string Directory { get; }

        // The language ids that have a sample, sorted.
        public IEnumerable<string> Languages()
            => System.IO.Directory.EnumerateFiles(Directory, "*.txt").Select(Path.GetFileNameWithoutExtension).OrderBy(x => x, StringComparer.Ordinal)!;

        public string[] Lines(string language) => File.ReadAllText(Path.Combine(Directory, language + ".txt")).Split('\n');

        public IGrammar LoadGrammar(string language)
            => _registry.LoadGrammar(_options.GetScopeByLanguageId(language)) ?? throw new InvalidOperationException($"no grammar for {language}");

        // Tokenizes every sample and returns the SHA-256 of the tokens (offsets and scopes), to compare regex engines.
        public static string Dump(TextWriter? writer)
        {
            var samples = new Samples();
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (string language in samples.Languages())
            {
                IGrammar grammar = samples.LoadGrammar(language);
                IStateStack? state = null;
                foreach (string line in samples.Lines(language))
                {
                    ITokenizeLineResult result = grammar.TokenizeLine(line, state, TimeSpan.MaxValue);
                    state = result.RuleStack;
                    var text = new StringBuilder();
                    foreach (IToken token in result.Tokens)
                    {
                        text.Append(language).Append(' ').Append(token.StartIndex).Append('-').Append(token.EndIndex).Append(' ')
                            .Append(string.Join(" ", token.Scopes)).Append('\n');
                    }
                    writer?.Write(text);
                    sha.AppendData(Encoding.UTF8.GetBytes(text.ToString()));
                }
            }
            return Convert.ToHexString(sha.GetHashAndReset());
        }

        private static string Find()
        {
            for (string? dir = AppDomain.CurrentDomain.BaseDirectory; dir != null; dir = Path.GetDirectoryName(dir))
            {
                string candidate = Path.Combine(dir, "src", "TextMateSharp.WasmDemo", "wwwroot", "samples");
                if (System.IO.Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new DirectoryNotFoundException("Could not find src/TextMateSharp.WasmDemo/wwwroot/samples; run from the TextMateSharp repository.");
        }
    }
}
