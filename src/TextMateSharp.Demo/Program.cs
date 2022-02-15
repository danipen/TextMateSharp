using System;
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

                StackElement ruleStack = null;

                using (StreamReader sr = new StreamReader(fileToParse))
                {
                    string? line = sr.ReadLine();

                    while (line != null)
                    {
                        ITokenizeLineResult result = grammar.TokenizeLine(line, ruleStack);

                        ruleStack = result.RuleStack;

                        foreach (IToken token in result.Tokens)
                        {
                            int startIndex = (token.StartIndex > line.Length) ?
                                line.Length : token.StartIndex;
                            int endIndex = (token.EndIndex > line.Length) ?
                                line.Length : token.EndIndex;

                            int foreground = -1;
                            int background = -1;
                            int fontStyle = -1;

                            foreach (var themeRule in theme.Match(token.Scopes))
                            {
                                if (foreground == -1 && themeRule.foreground > 0)
                                    foreground = themeRule.foreground;

                                if (background == -1 && themeRule.background > 0)
                                    background = themeRule.background;

                                if (fontStyle == -1 && themeRule.fontStyle > 0)
                                    fontStyle = themeRule.fontStyle;
                            }

                            WriteToken(line.SubstringAtIndexes(startIndex, endIndex), foreground, background, fontStyle, theme);
                        }

                        Console.WriteLine();
                        line = sr.ReadLine();
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
        static void WriteToken(string text, int foreground, int background, int fontStyle, Theme theme)
        {
            if (foreground == -1)
            {
                Console.Write(text);
                return;
            }

            Decoration decoration = GetDecoration(fontStyle);

            Color backgroundColor = GetColor(background, theme);
            Color foregroundColor = GetColor(foreground, theme);

            Style style = new Style(foregroundColor, backgroundColor, decoration);
            Markup markup = new Markup(text.Replace("[", "[[").Replace("]", "]]"), style);

            AnsiConsole.Write(markup);
        }

        static Color GetColor(int colorId, Theme theme)
        {
            if (colorId == -1)
                return Color.Default;

            return HexToColor(theme.GetColor(colorId));
        }

        static Decoration GetDecoration(int fontStyle)
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
    }

    internal static class StringExtensions
    {
        internal static string SubstringAtIndexes(this string str, int startIndex, int endIndex)
        {
            return str.Substring(startIndex, endIndex - startIndex);
        }
    }
}
