![dotnet-workflow](https://github.com/danipen/TextMateSharp/actions/workflows/dotnet.yml/badge.svg)
![GitHub license](https://img.shields.io/github/license/danipen/TextMateSharp)
[![Current stable version](https://img.shields.io/nuget/v/TextMateSharp.svg)](https://www.nuget.org/packages/TextMateSharp)
[![Downloads](https://img.shields.io/nuget/dt/TextMateSharp)](https://www.nuget.org/packages/TextMateSharp)
![Size](https://img.shields.io/github/repo-size/danipen/textmatesharp.svg) 
![GitHub language count](https://img.shields.io/github/languages/count/danipen/TextMateSharp)
![GitHub top language](https://img.shields.io/github/languages/top/danipen/TextMateSharp)

# TextMateSharp
An interpreter for grammar files as defined by TextMate. TextMate grammars use the oniguruma dialect (https://github.com/kkos/oniguruma). Supports loading grammar files only from JSON format. Cross - grammar injections are currently not supported.

TextMateSharp is a port of [microsoft/vscode-textmate](https://github.com/microsoft/vscode-textmate) that brings TextMate grammars to dotnet ecosystem. The implementation is based the Java port [eclipse/tm4e](https://github.com/eclipse/tm4e).

TextMateSharp uses a [wrapper](https://github.com/aikawayataro/Onigwrap) via [nuget packges](https://www.nuget.org/packages/Onigwrap) around [Oniguruma](https://github.com/kkos/oniguruma) regex engine. Thanks [@aikawayataro](https://github.com/aikawayataro) for your contribution.

TextMateSharp is used by [AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit).

## Building
Just execute `dotnet build` under the folder [TextMateSharp](https://github.com/danipen/TextMateSharp/tree/master/src/TextMateSharp)

## Using
```csharp
using System;
using System.Collections.Generic;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace TextMateSharp;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Registry.Registry registry = new Registry.Registry(
                new RegistryOptions(ThemeName.DarkPlus));

            List<string> textLines = new List<string>();
            textLines.Add("using static System; /* comment here */");
            textLines.Add("namespace Example");
            textLines.Add("{");
            textLines.Add("}");

            IGrammar grammar = registry.LoadGrammar("source.cs");

            IStateStack ruleStack = null;

            foreach (var line in textLines)
            {
                Console.WriteLine($"Tokenizing line: {line}");

                ITokenizeLineResult result = grammar.TokenizeLine(line, ruleStack, TimeSpan.MaxValue);

                ruleStack = result.RuleStack;

                foreach (var token in result.Tokens)
                {
                    int startIndex = (token.StartIndex > line.Length) ? line.Length : token.StartIndex;
                    int endIndex = (token.EndIndex > line.Length) ? line.Length : token.EndIndex;

                    Console.WriteLine(
                        "  - token from {0} to {1} -->{2}<-- with scopes {3}",
                        startIndex,
                        endIndex,
                        line.Substring(startIndex, endIndex - startIndex),
                        string.Join(",", token.Scopes));

                    Theme theme = registry.GetTheme();

                    foreach (var themeRule in theme.Match(token.Scopes))
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
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);
        }
    }
}
```

OUTPUT:
```
Tokenizing line: using static System; /* comment here */
  - token from 0 to 5 -->using<-- with scopes source.cs,keyword.other.directive.using.cs
      - Matched theme rule: [bg: , fg:#569CD6, fontStyle: NotSet]
  - token from 5 to 6 --> <-- with scopes source.cs
  - token from 6 to 12 -->static<-- with scopes source.cs,keyword.other.directive.static.cs
      - Matched theme rule: [bg: , fg:#569CD6, fontStyle: NotSet]
  - token from 12 to 13 --> <-- with scopes source.cs
  - token from 13 to 19 -->System<-- with scopes source.cs,entity.name.type.cs
      - Matched theme rule: [bg: , fg:#4EC9B0, fontStyle: NotSet]
  - token from 19 to 20 -->;<-- with scopes source.cs,punctuation.terminator.statement.cs
  - token from 20 to 21 --> <-- with scopes source.cs
  - token from 21 to 23 -->/*<-- with scopes source.cs,comment.block.cs,punctuation.definition.comment.cs
      - Matched theme rule: [bg: , fg:#6A9955, fontStyle: NotSet]
  - token from 23 to 37 --> comment here <-- with scopes source.cs,comment.block.cs
      - Matched theme rule: [bg: , fg:#6A9955, fontStyle: NotSet]
  - token from 37 to 39 -->*/<-- with scopes source.cs,comment.block.cs,punctuation.definition.comment.cs
      - Matched theme rule: [bg: , fg:#6A9955, fontStyle: NotSet]
Tokenizing line: namespace Example
  - token from 0 to 9 -->namespace<-- with scopes source.cs,storage.type.namespace.cs
      - Matched theme rule: [bg: , fg:#569CD6, fontStyle: NotSet]
  - token from 9 to 10 --> <-- with scopes source.cs
  - token from 10 to 17 -->Example<-- with scopes source.cs,entity.name.type.namespace.cs
      - Matched theme rule: [bg: , fg:#4EC9B0, fontStyle: NotSet]
Tokenizing line: {
  - token from 0 to 1 -->{<-- with scopes source.cs,punctuation.curlybrace.open.cs
Tokenizing line: }
  - token from 0 to 1 -->}<-- with scopes source.cs,punctuation.curlybrace.close.cs
```

## Demo

There is a demo project in [TextMateSharp.Demo](https://github.com/danipen/TextMateSharp/tree/master/src/TextMateSharp.Demo) folder.

![image](https://user-images.githubusercontent.com/501613/154065980-44b416ab-3b01-45f7-a8b3-7185413e769c.png)

Build and run:

```
cd src/TestMateSharp.Demo
dotnet build
dotnet run -- ./testdata/samplefiles/sample.cs
```
