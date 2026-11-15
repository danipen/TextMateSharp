using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using TextMateSharp.Grammars;
using TextMateSharp.Themes;

using Spectre.Console;

namespace TextMateSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    Console.WriteLine("Usage TextMateSharp.Demo <fileToParse.cs>");
                    Console.WriteLine("EXAMPLE TextMateSharp.Demo .\\testdata\\samplefiles\\sample.cs");

                    return;
                }

                string fileToParse = Path.GetFullPath(args[0]);

                if (!File.Exists(fileToParse))
                {
                    Console.WriteLine("No such file to parse: {0}", args[0]);
                    return;
                }

                RegistryOptions options = new RegistryOptions(ThemeName.DarkPlus);

                Registry.Registry registry = new Registry.Registry(options);

                Theme theme = registry.GetTheme();

                int ini = Environment.TickCount;
                IGrammar grammar = registry.LoadGrammar(options.GetScopeByExtension(Path.GetExtension(fileToParse)));

                if (grammar == null)
                {
                    Console.WriteLine(File.ReadAllText(fileToParse));
                    return;
                }

                Console.WriteLine("Grammar loaded in {0}ms.",
                    Environment.TickCount - ini);

                int tokenizeIni = Environment.TickCount;

                IStateStack? ruleStack = null;

                string fileContent = File.ReadAllText(fileToParse);
                ReadOnlyMemory<char> contentMemory = fileContent.AsMemory();

                foreach (var lineRange in GetLineRanges(fileContent))
                {
                    bool needsLineBreak = true;

                    ReadOnlyMemory<char> lineMemory = contentMemory.Slice(lineRange.Start, lineRange.Length);
                    ITokenizeLineResult result = grammar.TokenizeLine(lineMemory, ruleStack, TimeSpan.MaxValue);

                    ruleStack = result.RuleStack;

                    foreach (IToken token in result.Tokens)
                    {
                        int startIndex = Math.Min(token.StartIndex, lineRange.Length);
                        int endIndex = Math.Min(token.EndIndex, lineRange.Length);

                        var styles = ResolvedStyles.Create(token, theme);

                        ReadOnlySpan<char> tokenSpan = lineMemory.Span.Slice(startIndex, endIndex - startIndex);
                        WriteToken(tokenSpan, styles, theme);

                        if (tokenSpan.IndexOf('\n') != -1)
                            needsLineBreak = false;
                    }

                    if (needsLineBreak)
                        Console.WriteLine();
                }

                var colorDictionary = theme.GetGuiColorDictionary();
                if (colorDictionary is { Count: > 0 })
                {
                    Console.WriteLine("Gui Control Colors");
                    foreach (var kvp in colorDictionary)
                    {
                        Console.WriteLine( $"  {kvp.Key}, {kvp.Value}");
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

        static void WriteToken(ReadOnlySpan<char> text, ResolvedStyles styles, Theme theme)
        {
            if (styles.ForegroundColor == ResolvedStyles.NoColor)
            {
                Console.Out.Write(text);
                return;
            }

            Decoration decoration = GetDecoration(styles.FontStyle);

            Color backgroundColor = GetColor(styles.BackgroundColor, theme);
            Color foregroundColor = GetColor(styles.ForegroundColor, theme);

            Style style = new Style(foregroundColor, backgroundColor, decoration);
            string textStr = text.ToString();
            Markup markup = new Markup(textStr.Replace("[", "[[").Replace("]", "]]"), style);

            AnsiConsole.Write(markup);
        }

        static Color GetColor(int colorId, Theme theme)
        {
            if (colorId == -1)
                return Color.Default;

            return HexToColor(theme.GetColor(colorId));
        }

        static Decoration GetDecoration(FontStyle fontStyle)
        {
            Decoration result = Decoration.None;

            if (fontStyle == FontStyle.NotSet)
                return result;

            if ((fontStyle & FontStyle.Italic) != 0)
                result |= Decoration.Italic;

            if ((fontStyle & FontStyle.Underline) != 0)
                result |= Decoration.Underline;

            if ((fontStyle & FontStyle.Bold) != 0)
                result |= Decoration.Bold;

            return result;
        }

        static Color HexToColor(string hexString)
        {
            //replace # occurences
            if (hexString.IndexOf('#') != -1)
                hexString = hexString.Replace("#", "");

            byte r, g, b = 0;

            r = byte.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            g = byte.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            b = byte.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier);

            return new Color(r, g, b);
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
