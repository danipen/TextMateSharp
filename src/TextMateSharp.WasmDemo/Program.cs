using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace TextMateSharp.WasmDemo;

public static partial class Program
{
    // The page drives highlighting through the exports below.
    public static void Main() => Console.WriteLine($"TextMateSharp ready on {System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}");
}

// Highlights text for the page: the bundled grammars and themes of TextMateSharp.Grammars.
[SupportedOSPlatform("browser")]
public static partial class Highlighter
{
    private static readonly RegistryOptions s_options = new(ThemeName.DarkPlus);
    private static readonly Registry.Registry s_registry = new(s_options);
    private static readonly Dictionary<string, IGrammar?> s_grammars = [];

    // One line per language with a grammar: id, display name and scope, tab-separated, sorted by name.
    [JSExport]
    public static string Languages()
    {
        var lines = s_options.GetAvailableLanguages()
            .Select(l => (l.Id, Name: l.Aliases[0], Scope: s_options.GetScopeByLanguageId(l.Id)))
            .Where(l => l.Scope is not null)
            .DistinctBy(l => l.Id)
            .OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
            .Select(l => $"{l.Id}\t{l.Name}\t{l.Scope}");
        return string.Join('\n', lines);
    }

    // The bundled theme names, one per line.
    [JSExport]
    public static string Themes() => string.Join('\n', Enum.GetNames<ThemeName>());

    // Switches the theme; returns the editor background and foreground as "background;foreground".
    [JSExport]
    public static string SetTheme(string name)
    {
        s_registry.SetTheme(s_options.LoadTheme(Enum.Parse<ThemeName>(name)));
        var colors = s_registry.GetTheme().GetGuiColorDictionary();
        return $"{colors.GetValueOrDefault("editor.background", "#1e1e1e")};{colors.GetValueOrDefault("editor.foreground", "#d4d4d4")}";
    }

    // The text as HTML spans colored by the scope's grammar and the theme, after a status line and U+0001.
    [JSExport]
    public static string Highlight(string scope, string text)
    {
        var watch = Stopwatch.StartNew();
        if (!s_grammars.TryGetValue(scope, out var grammar))
        {
            s_grammars[scope] = grammar = s_registry.LoadGrammar(scope);
        }

        if (grammar is null)
        {
            return $"No grammar for {scope}\u0001{WebUtility.HtmlEncode(text)}";
        }

        var theme = s_registry.GetTheme();
        var html = new StringBuilder(text.Length * 4);
        IStateStack? state = null;
        var lines = text.Split('\n');
        var tokens = 0;
        foreach (var line in lines)
        {
            var result = grammar.TokenizeLine(line, state, TimeSpan.MaxValue);
            state = result.RuleStack;
            foreach (var token in result.Tokens)
            {
                var start = Math.Min(token.StartIndex, line.Length);
                var end = Math.Min(token.EndIndex, line.Length);
                if (end <= start)
                {
                    continue;
                }

                tokens++;
                AppendToken(html, theme, token.Scopes, line.AsSpan(start, end - start));
            }

            html.Append('\n');
        }

        return $"{tokens} tokens, {lines.Length} lines, highlighted in {watch.Elapsed.TotalMilliseconds:F1} ms\u0001{html}";
    }

    private static void AppendToken(StringBuilder html, Theme theme, List<string> scopes, ReadOnlySpan<char> text)
    {
        var foreground = -1;
        var style = FontStyle.NotSet;
        foreach (var rule in theme.Match(scopes))
        {
            if (foreground == -1 && rule.foreground > 0)
            {
                foreground = rule.foreground;
            }

            if (style == FontStyle.NotSet && rule.fontStyle > 0)
            {
                style = rule.fontStyle;
            }
        }

        html.Append("<span title=\"").Append(WebUtility.HtmlEncode(string.Join(' ', scopes))).Append("\" style=\"");
        if (foreground > 0)
        {
            html.Append("color:").Append(theme.GetColor(foreground)).Append(';');
        }

        if (style != FontStyle.NotSet)
        {
            if ((style & FontStyle.Italic) != 0) html.Append("font-style:italic;");
            if ((style & FontStyle.Bold) != 0) html.Append("font-weight:bold;");
            if ((style & FontStyle.Underline) != 0) html.Append("text-decoration:underline;");
        }

        html.Append("\">").Append(WebUtility.HtmlEncode(text.ToString())).Append("</span>");
    }
}
