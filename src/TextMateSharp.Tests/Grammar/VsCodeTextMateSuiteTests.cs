using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace TextMateSharp.Tests.Grammar
{
    // vscode-textmate's tokenization suites (src/tests/tokenization.test.ts over test-cases/first-mate/tests.json,
    // test-cases/suite1/tests.json and test-cases/suite1/whileTests.json): each test loads its grammars, tokenizes its
    // lines in sequence and compares every token's text and scopes. See Resources/vscode-textmate/README.md.
    [TestFixture]
    public class VsCodeTextMateSuiteTests
    {
        private static readonly string Root = Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "vscode-textmate");

        public static IEnumerable<TestCaseData> Cases()
        {
            foreach (string suite in new[] { "first-mate/tests.json", "suite1/tests.json", "suite1/whileTests.json" })
            {
                string location = Path.Combine(Root, suite);
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(location));
                int index = 0;
                foreach (JsonElement test in document.RootElement.EnumerateArray())
                {
                    string desc = test.TryGetProperty("desc", out JsonElement d) ? d.GetString() : test.GetProperty("grammarPath").GetString();
                    yield return new TestCaseData(suite, index).SetName($"{suite} #{index}: {desc}");
                    index++;
                }
            }
        }

        // Cases TextMateSharp tokenizes differently with Onigwrap and with GregsEx alike: TextMateSharp gaps, not
        // regex engine differences.
        private static readonly Dictionary<(string, int), string> KnownDifferences = new()
        {
            [("first-mate/tests.json", 21)] = "an end pattern with a back-reference to a capture of the begin match spanning the end of the line",
            [("suite1/tests.json", 1)] = "scope names with capture transforms (${1/(#)(#)?/...})",
            [("suite1/tests.json", 11)] = "nested repository includes in Markdown",
            [("suite1/tests.json", 15)] = "a rule without a match regex (ArgumentNullException in RegExpSource)",
            [("suite1/tests.json", 16)] = "injection grammars (grammarInjections)",
            [("suite1/tests.json", 21)] = "escaping of back-references resolved into end patterns",
        };

        [TestCaseSource(nameof(Cases))]
        public void Tokenizes_Like_VsCodeTextMate(string suite, int index)
        {
            if (KnownDifferences.TryGetValue((suite, index), out string reason))
            {
                Assert.Ignore("Known TextMateSharp difference: " + reason);
            }

            string location = Path.Combine(Root, suite);
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(location));
            JsonElement test = document.RootElement[index];
            string directory = Path.GetDirectoryName(location)!;

            string scopeName = test.TryGetProperty("grammarScopeName", out JsonElement s) ? s.GetString() : null;
            var grammars = new Dictionary<string, IRawGrammar>();
            foreach (JsonElement grammarPath in test.GetProperty("grammars").EnumerateArray())
            {
                IRawGrammar raw = Read(Path.Combine(directory, grammarPath.GetString()!));
                grammars[raw.GetScopeName()] = raw;
                if (scopeName == null && test.TryGetProperty("grammarPath", out JsonElement main) && main.GetString() == grammarPath.GetString())
                {
                    scopeName = raw.GetScopeName();
                }
            }

            List<string> injections = test.TryGetProperty("grammarInjections", out JsonElement i)
                ? i.EnumerateArray().Select(x => x.GetString()!).ToList() : null;
            var registry = new Registry.Registry(new Options(grammars, scopeName!, injections));
            IGrammar grammar = registry.LoadGrammar(scopeName);
            Assert.That(grammar, Is.Not.Null, "I HAVE NO GRAMMAR FOR TEST");

            IStateStack state = null;
            foreach (JsonElement testLine in test.GetProperty("lines").EnumerateArray())
            {
                string line = testLine.GetProperty("line").GetString()!;
                ITokenizeLineResult result = grammar.TokenizeLine(line, state, TimeSpan.MaxValue);
                state = result.RuleStack;

                var actual = result.Tokens
                    .Select(t => (Value: Substring(line, t.StartIndex, t.EndIndex), Scopes: string.Join(" ", t.Scopes)))
                    .ToList();
                var expected = testLine.GetProperty("tokens").EnumerateArray()
                    .Select(t => (Value: t.GetProperty("value").GetString()!, Scopes: string.Join(" ", t.GetProperty("scopes").EnumerateArray().Select(x => x.GetString()))))
                    .Where(t => line.Length == 0 || t.Value.Length > 0)
                    .ToList();
                // TextMateSharp reports the newline it appends as part of the last token; vscode-textmate does not.
                if (line.Length > 0)
                {
                    actual = actual.Where(t => t.Value.Length > 0).ToList();
                }

                Assert.That(Format(actual), Is.EqualTo(Format(expected)), "Tokenizing line " + line);
            }
        }

        private static string Substring(string line, int start, int end)
        {
            start = Math.Min(start, line.Length);
            end = Math.Min(end, line.Length);
            return line.Substring(start, Math.Max(0, end - start));
        }

        private static string Format(List<(string Value, string Scopes)> tokens)
            => string.Join("\n", tokens.Select(t => $"{JsonSerializer.Serialize(t.Value)} {t.Scopes}"));

        private static IRawGrammar Read(string path)
        {
            if (!File.Exists(path))
            {
                path += ".json";
            }

            using var reader = new StreamReader(path);
            return GrammarReader.ReadGrammarSync(reader);
        }

        private sealed class Options : IRegistryOptions
        {
            private readonly Dictionary<string, IRawGrammar> _grammars;
            private readonly string _scopeName;
            private readonly List<string> _injections;

            public Options(Dictionary<string, IRawGrammar> grammars, string scopeName, List<string> injections)
            {
                _grammars = grammars;
                _scopeName = scopeName;
                _injections = injections;
            }

            public IRawGrammar GetGrammar(string scopeName) => _grammars.TryGetValue(scopeName, out IRawGrammar g) ? g : null;

            public ICollection<string> GetInjections(string scopeName) => scopeName == _scopeName ? _injections : null;

            public IRawTheme GetTheme(string scopeName) => null;

            public IRawTheme GetDefaultTheme() => EmptyTheme.Instance;
        }

        private sealed class EmptyTheme : IRawTheme
        {
            public static readonly EmptyTheme Instance = new EmptyTheme();

            public string GetName() => "none";
            public string GetInclude() => null;
            public ICollection<IRawThemeSetting> GetSettings() => new List<IRawThemeSetting>();
            public ICollection<IRawThemeSetting> GetTokenColors() => new List<IRawThemeSetting>();
            public ICollection<KeyValuePair<string, object>> GetGuiColors() => new List<KeyValuePair<string, object>>();
        }
    }
}
