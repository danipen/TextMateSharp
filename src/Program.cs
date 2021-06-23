using System;
using System.Collections.Generic;
using System.IO;

using TextMateSharp.Grammars;
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
                textLines.Add("using System;");
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
                        Console.WriteLine(string.Format("Token from {0} to {1} ->{2}<- with scopes {2}",
                            token.GetStartIndex(),
                            token.GetEndIndex(),
                            line.Substring(token.GetStartIndex(), token.GetEndIndex()),
                            string.Join(",", token.GetScopes())));
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
                string result = Path.GetFullPath(@"../../../../test/testgrammars/csharp.tmLanguage.json");
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
                return null;
            }
        }
    }
}
