using System;
using System.Collections.Generic;
using System.IO;

using TextMateSharp.Grammars;
using TextMateSharp.Internal.Themes.Reader;
using TextMateSharp.Internal.Utils;
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
                IRegistryOptions options = new LocalRegistryOptions();
                Registry.Registry registry = new Registry.Registry(options);

                List<string> textLines = new List<string>();
                textLines.Add("using static System; /* comment here */");
                textLines.Add("namespace Example");
                textLines.Add("{");
                textLines.Add("}");

                IGrammar grammar = registry.LoadGrammar("source.cs");

                foreach (string line in textLines)
                {
                    Console.WriteLine(string.Format("Tokenizing line: {0}", line));

                    ITokenizeLineResult result = grammar.TokenizeLine(line);

                    foreach (IToken token in result.GetTokens())
                    {
                        int startIndex = (token.StartIndex > line.Length) ?
                            line.Length : token.StartIndex;
                        int endIndex = (token.EndIndex > line.Length) ?
                            line.Length : token.EndIndex;

                        if (startIndex == endIndex)
                            continue;

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
                                theme.Match(scopeName);

                            foreach (ThemeTrieElementRule themeRule in themeRules)
                            {
                                Console.WriteLine(
                                    "      - Matched theme rule: " +
                                    "[bg: {0}, fg:{1}, fontStyle: {2}]",
                                    theme.GetColor(themeRule.background),
                                    theme.GetColor(themeRule.foreground),
                                    themeRule.fontStyle);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }

        class LocalRegistryOptions : IRegistryOptions
        {
            public string GetFilePath(string scopeName)
            {
                string result = Path.GetFullPath(
                    @"../../../../test/grammars/csharp.tmLanguage.json");
                return result;
            }

            public ICollection<string> GetInjections(string scopeName)
            {
                return null;
            }

            public StreamReader GetInputStream(string scopeName)
            {
                return new StreamReader(GetFilePath(scopeName));
            }

            public IRawTheme GetTheme()
            {
                string themePath = Path.GetFullPath(
                    @"../../../../test/themes/dark_vs.json");

                using (StreamReader reader = new StreamReader(themePath))
                {
                    return ThemeReader.ReadThemeSync(reader);
                }
            }
        }
    }
}
