using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace TextMateSharp.Benchmarks
{
    // Tokenizes the code fences of documentation pages, as a static site generator highlights them: line by line,
    // without the newline, with the tm-grammars 1.32.22 grammars. TEXTMATE_DIR holds those grammars (manifest.json,
    // grammars/), DOCS_FENCES the fences per language as JSON ({ "ts": ["fence", ...], ... }).
    [MemoryDiagnoser]
    [ShortRunJob]
    public class DocsHeavyTokenizationBenchmark
    {
        public const int FencesPerLanguage = 40;

        private IGrammar _grammar = null!;
        private string[][] _fences = null!;

        [Params("bash", "ts", "js", "html", "json", "yaml", "csharp")]
        public string Language { get; set; } = "";

        [GlobalSetup]
        public void Setup()
        {
            var corpus = DocsHeavyCorpus.Load();
            _fences = corpus.Fences[Language].Take(FencesPerLanguage).Select(f => f.Split('\n')).ToArray();
            _grammar = corpus.LoadGrammar(Language);
            Console.WriteLine($"// {Language}: {_fences.Length} fences, {_fences.Sum(f => f.Length)} lines");
            TokenizeFences();
        }

        [Benchmark]
        public int TokenizeFences()
        {
            int tokens = 0;
            foreach (string[] lines in _fences)
            {
                IStateStack? state = null;
                foreach (string line in lines)
                {
                    ITokenizeLineResult result = _grammar.TokenizeLine(line, state, TimeSpan.MaxValue);
                    state = result.RuleStack;
                    tokens += result.Tokens.Length;
                }
            }
            return tokens;
        }
    }

    public sealed class DocsHeavyCorpus
    {
        private static readonly Dictionary<string, string> Names = new()
        {
            ["bash"] = "shellscript", ["ts"] = "typescript", ["js"] = "javascript", ["html"] = "html",
            ["json"] = "json", ["yaml"] = "yaml", ["csharp"] = "csharp",
        };

        private readonly string _directory;
        private readonly Dictionary<string, (string Name, string[] Embedded, string[] InjectTo)> _byScope = new();
        private readonly Dictionary<string, string> _scopeByName = new();

        public Dictionary<string, List<string>> Fences { get; }

        private DocsHeavyCorpus(string directory, Dictionary<string, List<string>> fences)
        {
            _directory = directory;
            Fences = fences;
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, "manifest.json")));
            foreach (string list in new[] { "grammars", "injections" })
            {
                foreach (JsonElement item in manifest.RootElement.GetProperty("tm-grammars").GetProperty("lists").GetProperty(list).EnumerateArray())
                {
                    string name = item.GetProperty("name").GetString()!;
                    string scope = item.GetProperty("scopeName").GetString()!;
                    _byScope[scope] = (name, Strings(item, "embedded"), Strings(item, "injectTo"));
                    _scopeByName[name] = scope;
                }
            }
        }

        private static string[] Strings(JsonElement item, string property)
            => item.TryGetProperty(property, out JsonElement value) ? value.EnumerateArray().Select(x => x.GetString()!).ToArray() : Array.Empty<string>();

        public static DocsHeavyCorpus Load()
        {
            string directory = Environment.GetEnvironmentVariable("TEXTMATE_DIR") ?? throw new InvalidOperationException("TEXTMATE_DIR is not set");
            string fences = Environment.GetEnvironmentVariable("DOCS_FENCES") ?? throw new InvalidOperationException("DOCS_FENCES is not set");
            return new DocsHeavyCorpus(directory, JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(fences))!);
        }

        public IGrammar LoadGrammar(string language)
        {
            string scope = _scopeByName[Names[language]];
            var registry = new Registry.Registry(new Locator(this, Names[language]));
            return registry.LoadGrammar(scope) ?? throw new InvalidOperationException($"cannot load {scope}");
        }

        // Tokenizes every fence of every language and returns the SHA-256 of the tokens (offsets and scopes).
        public static string Dump(TextWriter? writer)
        {
            var corpus = Load();
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (string language in Names.Keys)
            {
                IGrammar grammar = corpus.LoadGrammar(language);
                foreach (string fence in corpus.Fences[language])
                {
                    IStateStack? state = null;
                    foreach (string line in fence.Split('\n'))
                    {
                        ITokenizeLineResult result = grammar.TokenizeLine(line, state, TimeSpan.MaxValue);
                        state = result.RuleStack;
                        var text = new StringBuilder();
                        foreach (IToken token in result.Tokens)
                        {
                            text.Append(token.StartIndex).Append('-').Append(token.EndIndex).Append(' ').Append(string.Join(" ", token.Scopes)).Append('\n');
                        }
                        writer?.Write(text);
                        sha.AppendData(Encoding.UTF8.GetBytes(text.ToString()));
                    }
                }
            }
            return Convert.ToHexString(sha.GetHashAndReset());
        }

        private sealed class Locator : IRegistryOptions
        {
            private readonly DocsHeavyCorpus _corpus;
            private readonly HashSet<string> _embedded = new(StringComparer.Ordinal);

            public Locator(DocsHeavyCorpus corpus, string root)
            {
                _corpus = corpus;
                var pending = new Stack<string>(new[] { root });
                while (pending.Count > 0)
                {
                    string name = pending.Pop();
                    if (_embedded.Add(name) && corpus._scopeByName.TryGetValue(name, out string? scope))
                    {
                        foreach (string embedded in corpus._byScope[scope].Embedded)
                        {
                            pending.Push(embedded);
                        }
                    }
                }
            }

            public IRawGrammar GetGrammar(string scopeName)
            {
                if (!_corpus._byScope.TryGetValue(scopeName, out var grammar))
                {
                    return null!;
                }

                using var reader = new StreamReader(Path.Combine(_corpus._directory, "grammars", grammar.Name + ".json"));
                return GrammarReader.ReadGrammarSync(reader);
            }

            public ICollection<string> GetInjections(string scopeName)
            {
                var injections = _corpus._byScope.Where(x => x.Value.InjectTo.Contains(scopeName) && _embedded.Contains(x.Value.Name)).Select(x => x.Key).ToList();
                return injections.Count == 0 ? null! : injections;
            }

            public IRawTheme GetTheme(string scopeName) => null!;

            public IRawTheme GetDefaultTheme() => EmptyTheme.Instance;
        }

        private sealed class EmptyTheme : IRawTheme
        {
            public static readonly EmptyTheme Instance = new();

            public string GetName() => "none";
            public string GetInclude() => null!;
            public ICollection<IRawThemeSetting> GetSettings() => new List<IRawThemeSetting>();
            public ICollection<IRawThemeSetting> GetTokenColors() => new List<IRawThemeSetting>();
            public ICollection<KeyValuePair<string, object>> GetGuiColors() => new List<KeyValuePair<string, object>>();
        }
    }
}
