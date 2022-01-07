using System;
using System.Collections.Generic;
using System.IO;

using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Themes.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace TextMateSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage TextMateSharp.Demo <fileToParse.cs> <grammar> <themefile>");
                    Console.WriteLine("EXAMPLE TextMateSharp.Demo .\\testdata\\samplefiles\\sample.cs .\\testdata\\grammars\\csharp.tmLanguage.json .\\testdata\\themes\\dark_vs.json");

                    return;
                }

                string fileToParse = Path.GetFullPath(args[0]);
                string grammarFile = Path.GetFullPath(args[1]);
                string themeFile = Path.GetFullPath(args[2]);

                if (!File.Exists(fileToParse))
                {
                    Console.WriteLine("No such file to parse: {0}", args[0]);
                    return;
                }

                if (!File.Exists(grammarFile))
                {
                    Console.WriteLine("No such file to parse: {0}", args[1]);
                    return;
                }

                if (!File.Exists(themeFile))
                {
                    Console.WriteLine("No such file to parse: {0}", args[2]);
                    return;
                }

                IRegistryOptions options = new DemoRegistryOptions(grammarFile, themeFile);

                Registry.Registry registry = new Registry.Registry(options);

                int ini = Environment.TickCount;
                IGrammar grammar = registry.LoadGrammar("text.html.basic");
                Console.WriteLine("Loaded {0} in {1}ms.",
                    Path.GetFileName(grammarFile),
                    Environment.TickCount - ini);

                string[] textLines = File.ReadAllText(fileToParse).Split(Environment.NewLine);

                int tokenizeIni = Environment.TickCount;

                StackElement ruleStack = null;

                foreach (string line in textLines)
                {
                    Console.WriteLine(string.Format("Tokenizing line: {0}", line));

                    ITokenizeLineResult result = grammar.TokenizeLine(line, ruleStack);

                    ruleStack = result.RuleStack;

                    foreach (IToken token in result.Tokens)
                    {
                        int startIndex = (token.StartIndex > line.Length) ?
                            line.Length : token.StartIndex;
                        int endIndex = (token.EndIndex > line.Length) ?
                            line.Length : token.EndIndex;

                        Console.WriteLine(string.Format(
                            "  - token from {0} to {1} -->{2}<-- with scopes {3}",
                            startIndex,
                            endIndex,
                            line.Substring(startIndex, endIndex - startIndex),
                            string.Join(",", token.Scopes)));

                        foreach (string scopeName in token.Scopes)
                        {
                            Theme theme = registry.GetTheme();

                            List<ThemeTrieElementRule> themeRules =
                                theme.Match(new string[] { scopeName });

                            foreach (ThemeTrieElementRule themeRule in themeRules)
                            {
                                Console.WriteLine(
                                    "      - Matched theme rule: " +
                                    "[bg: {0}, fg:{1}, fontStyle: {2}, scopeDeph: {3}, parentScopes: {4}]",
                                    theme.GetColor(themeRule.background),
                                    theme.GetColor(themeRule.foreground),
                                    themeRule.fontStyle,
                                    themeRule.scopeDepth,
                                    string.Join(", ", themeRule.parentScopes == null ? new string[] { } : themeRule.parentScopes));
                            }
                        }
                    }
                }

                Console.WriteLine("File {0} tokenized in {1}ms.",
                    Path.GetFileName(fileToParse),
                    Environment.TickCount - tokenizeIni);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }

        class DemoRegistryOptions : IRegistryOptions
        {
            private string _grammarFile;
            private string _themeFile;
            private IThemeResolver _themeResolver;
            private IGrammarResolver _grammarResolver;
            internal DemoRegistryOptions(string grammarFile, string themeFile)
            {
                _grammarFile = grammarFile;
                _themeFile = themeFile;
                _themeResolver = new DemoThemeResolver(GetFilePath);
                _grammarResolver = new DemoGrammarResolver(GetFilePath);
            }
            class DemoThemeResolver : IThemeResolver
            {
                private readonly Func<string, string> _getFilePathMethod;

                public DemoThemeResolver(Func<string, string> getFilePathMethod)
                {
                    this._getFilePathMethod = getFilePathMethod;
                }
                public IRawTheme GetTheme(string scopeName)
                {
                    using var stream = new FileStream(_getFilePathMethod(scopeName), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);
                    return ThemeReader.ReadThemeSync(reader);
                }
            }
            class DemoGrammarResolver : IGrammarResolver
            {
                private readonly Func<string, string> _getFilePathMethod;

                public DemoGrammarResolver(Func<string, string> getFilePathMethod)
                {
                    this._getFilePathMethod = getFilePathMethod;
                }
                public IRawGrammar GetGrammar(string scopeName)
                {
                    using var stream = new FileStream(_getFilePathMethod(scopeName), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);
                    return GrammarReader.ReadGrammarSync(reader);
                }
            }
            public IThemeResolver ThemeResolver { get => _themeResolver; set => _themeResolver = value; }
            public IGrammarResolver GrammarResolver { get => _grammarResolver; set => _grammarResolver = value; }

            private string GetFilePath(string scopeName)
            {
                if (scopeName == "./dark_vs.json")
                    return Path.Combine(Path.GetDirectoryName(_themeFile), "dark_vs.json");

                if (scopeName == "text.html.cshtml")
                    return Path.Combine(Path.GetDirectoryName(_grammarFile), "cshtml.tmLanguage.json");

                if (scopeName == "text.html")
                    return Path.Combine(Path.GetDirectoryName(_grammarFile), "html-derivative.tmLanguage.json");

                if (scopeName == "text.html.basic")
                    return Path.Combine(Path.GetDirectoryName(_grammarFile), "html.tmLanguage.json");

                if (scopeName == "source.php")
                    return Path.Combine(Path.GetDirectoryName(_grammarFile), "php.tmLanguage.json");

                if (scopeName == "source.js")
                    return Path.Combine(Path.GetDirectoryName(_grammarFile), "JavaScript.tmLanguage.json");

                return null;
            }

            public ICollection<string> GetInjections(string scopeName)
            {
                return null;
            }

            public Stream GetInputStream(string scopeName)
            {
                return new FileStream(
                    GetFilePath(scopeName),
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);
            }

            public IRawTheme GetTheme()
            {
                int ini = Environment.TickCount;
                
                using (StreamReader reader = new StreamReader(_themeFile))
                {
                    IRawTheme result = ThemeReader.ReadThemeSync(reader);
                    Console.WriteLine("Loaded {0} in {1}ms.",
                        Path.GetFileName(_themeFile),
                        Environment.TickCount - ini);
                    return result;
                }
            }
        }
    }
}
