# TextMateSharp
A port of [microsoft/vscode-textmate](https://github.com/microsoft/vscode-textmate) that brings TextMate grammars to dotnet ecosystem.

The implementation is based the Java port [eclipse/tm4e](https://github.com/eclipse/tm4e).

TextMateSharp uses [Oniguruma](https://github.com/kkos/oniguruma) regex engine bindings.

Instructions about how to build Oniguruma bindings can be found in [`onigwrap/README.md`](https://github.com/danipen/TextMateSharp/tree/master/onigwrap)

## Building
Just execute `dotnet build` under the folder [TextMateSharp](https://github.com/danipen/TextMateSharp/tree/master/src/TextMateSharp)

## Using
```csharp
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
                            line.SubstringAtIndexes(startIndex, endIndex),
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
```

OUTPUT:
```
Tokenizing line: using static System; /* comment here */
  - token from 0 to 5 -->using<-- with scopes source.cs,keyword.other.using.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
      - Matched theme rule: [bg: , fg:#569CD6, fontStyle: -1]
  - token from 5 to 6 --> <-- with scopes source.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
  - token from 6 to 12 -->static<-- with scopes source.cs,keyword.other.static.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
      - Matched theme rule: [bg: , fg:#569CD6, fontStyle: -1]
  - token from 12 to 13 --> <-- with scopes source.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
  - token from 13 to 19 -->System<-- with scopes source.cs,storage.type.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
      - Matched theme rule: [bg: , fg:#569CD6, fontStyle: -1]
  - token from 19 to 20 -->;<-- with scopes source.cs,punctuation.terminator.statement.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
  - token from 20 to 21 --> <-- with scopes source.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
  - token from 21 to 23 -->/*<-- with scopes source.cs,comment.block.cs,punctuation.definition.comment.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
      - Matched theme rule: [bg: , fg:#6A9955, fontStyle: -1]
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
  - token from 23 to 37 --> comment here <-- with scopes source.cs,comment.block.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
      - Matched theme rule: [bg: , fg:#6A9955, fontStyle: -1]
  - token from 37 to 39 -->*/<-- with scopes source.cs,comment.block.cs,punctuation.definition.comment.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
      - Matched theme rule: [bg: , fg:#6A9955, fontStyle: -1]
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
Tokenizing line: namespace Example
  - token from 0 to 9 -->namespace<-- with scopes source.cs,keyword.other.namespace.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
      - Matched theme rule: [bg: , fg:#569CD6, fontStyle: -1]
  - token from 9 to 10 --> <-- with scopes source.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
  - token from 10 to 17 -->Example<-- with scopes source.cs,entity.name.type.namespace.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
Tokenizing line: {
  - token from 0 to 1 -->{<-- with scopes source.cs,punctuation.curlybrace.open.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
Tokenizing line: }
  - token from 0 to 1 -->}<-- with scopes source.cs
      - Matched theme rule: [bg: , fg:, fontStyle: -1]
```

## Demo

There is a demo project in [TextMateSharp.Demo](https://github.com/danipen/TextMateSharp/tree/master/src/TextMateSharp.Demo) folder.

Example grammars and files are located in the [testdata](https://github.com/danipen/TextMateSharp/tree/master/src/TextMateSharp.Demo/testdata) folder.

Build and run:

```
cd src/TestMateSharp.Demo
dotnet build
dotnet run -- ./testdata/samplefiles/sample.cs ./testdata/grammars/csharp.tmLanguage.json ./testdata/themes/dark_vs.json
```