using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using TextMateSharp.Internal.Utils;
using TextMateSharp.Registry;
using TextMateSharp.Internal.Themes.Reader;
using System.IO;

namespace TextMateSharp.Themes
{
    public class Theme
    {
        private ParsedTheme _theme;
        private ParsedTheme _include;
        private ColorMap _colorMap;

        public static Theme CreateFromRawTheme(
            IRawTheme source,
            IRegistryOptions registryOptions)
        {
            ColorMap colorMap = new ColorMap();

            ParsedTheme theme = ParsedTheme.CreateFromParsedTheme(
                ParsedTheme.ParseTheme(source, 0),
                colorMap);

            ParsedTheme include = ParsedTheme.CreateFromParsedTheme(
                ParsedTheme.ParseInclude(source, registryOptions, 0),
                colorMap);

            return new Theme(colorMap, theme, include);
        }

        Theme(ColorMap colorMap, ParsedTheme theme, ParsedTheme include)
        {
            _colorMap = colorMap;
            _theme = theme;
            _include = include;
        }

        public List<ThemeTrieElementRule> Match(IEnumerable<string> scopeNames)
        {
            List<ThemeTrieElementRule> result = new List<ThemeTrieElementRule>();

            foreach (string scope in scopeNames)
                result.AddRange(this._theme.Match(scope));

            foreach (string scope in scopeNames)
                result.AddRange(this._include.Match(scope));

            return result;
        }

        public ICollection<string> GetColorMap()
        {
            return this._colorMap.GetColorMap();
        }

        public int GetColorId(string color)
        {
            return this._colorMap.GetId(color);
        }

        public string GetColor(int id)
        {
            return this._colorMap.GetColor(id);
        }

        internal ThemeTrieElementRule GetDefaults()
        {
            return this._theme.GetDefaults();
        }
    }

    class ParsedTheme
    {
        private static Regex rrggbb = new Regex("^#[0-9a-f]{6}", RegexOptions.IgnoreCase);
        private static Regex rrggbbaa = new Regex("^#[0-9a-f]{8}", RegexOptions.IgnoreCase);
        private static Regex rgb = new Regex("^#[0-9a-f]{3}", RegexOptions.IgnoreCase);
        private static Regex rgba = new Regex("^#[0-9a-f]{4}", RegexOptions.IgnoreCase);

        private ThemeTrieElement root;
        private ThemeTrieElementRule defaults;

        private Dictionary<string /* scopeName */, List<ThemeTrieElementRule>> cache;

        internal static List<ParsedThemeRule> ParseTheme(IRawTheme source, int priority)
        {
            List<ParsedThemeRule> result = new List<ParsedThemeRule>();

            // process theme rules in vscode-textmate format:
            // see https://github.com/microsoft/vscode-textmate/tree/main/test-cases/themes
            LookupThemeRules(source.GetSettings(), result, priority);

            // process theme rules in vscode format
            // see https://github.com/microsoft/vscode/tree/main/extensions/theme-defaults/themes
            LookupThemeRules(source.GetTokenColors(), result, priority);

            return result;
        }

        internal static List<ParsedThemeRule> ParseInclude(
            IRawTheme source,
            IRegistryOptions registryOptions,
            int priority)
        {
            List<ParsedThemeRule> result = new List<ParsedThemeRule>();

            string include = source.GetInclude();

            if (string.IsNullOrEmpty(include))
                return result;

            Stream stream = registryOptions.GetInputStream(include);

            if (stream == null)
                return result;

            using (stream)
            using (StreamReader reader = new StreamReader(stream))
            {
                IRawTheme themeInclude = ThemeReader.ReadThemeSync(reader);

                if (themeInclude == null)
                    return result;

                return ParseTheme(themeInclude, priority);
            }
        }

        static void LookupThemeRules(
            ICollection<IRawThemeSetting> settings,
            List<ParsedThemeRule> parsedThemeRules,
            int priority)
        {
            if (settings == null)
                return;

            int i = 0;
            foreach (IRawThemeSetting entry in settings)
            {
                if (entry.GetSetting() == null)
                {
                    continue;
                }

                object settingScope = entry.GetScope();
                List<string> scopes = new List<string>();
                if (settingScope is string)
                {
                    string scope = (string)settingScope;

                    scopes = new List<string>(scope.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries));
                }
                else if (settingScope is IList<object>)
                {
                    scopes = new List<string>(((IList<object>)settingScope).Cast<string>());
                }
                else
                {
                    scopes.Add("");
                }

                int fontStyle = FontStyle.NotSet;
                object settingsFontStyle = entry.GetSetting().GetFontStyle();
                if (settingsFontStyle is string)
                {
                    fontStyle = FontStyle.None;

                    string[] segments = ((string) settingsFontStyle).Split(new[] {" "}, StringSplitOptions.None);
                    foreach (string segment in segments)
                    {
                        if ("italic".Equals(segment))
                        {
                            fontStyle = fontStyle | FontStyle.Italic;
                        }
                        else if ("bold".Equals(segment))
                        {
                            fontStyle = fontStyle | FontStyle.Bold;
                        }
                        else if ("underline".Equals(segment))
                        {
                            fontStyle = fontStyle | FontStyle.Underline;
                        }
                    }
                }

                string foreground = null;
                object settingsForeground = entry.GetSetting().GetForeground();
                if (settingsForeground is string && IsValidHexColor((string)settingsForeground))
                {
                    foreground = (string)settingsForeground;
                }

                string background = null;
                object settingsBackground = entry.GetSetting().GetBackground();
                if (settingsBackground is string && IsValidHexColor((string)settingsBackground))
                {
                    background = (string)settingsBackground;
                }
                for (int j = 0, lenJ = scopes.Count; j < lenJ; j++)
                {
                    string _scope = scopes[j].Trim();

                    List<string> segments = new List<string>(_scope.Split(new[] {" "}, StringSplitOptions.None));

                    string scope = segments[segments.Count - 1];
                    List<string> parentScopes = null;
                    if (segments.Count > 1)
                    {
                        parentScopes = new List<string>(segments);
                        parentScopes.Reverse();
                    }

                    ParsedThemeRule t = new ParsedThemeRule(scope, parentScopes, i, fontStyle, foreground, background);
                    parsedThemeRules.Add(t);
                }
                i++;
            }
        }

        private static bool IsValidHexColor(string hex)
        {
            if (hex == null || hex.Length < 1)
            {
                return false;
            }

            if (rrggbb.Match(hex).Success)
            {
                // #rrggbb
                return true;
            }

            if (rrggbbaa.Match(hex).Success)
            {
                // #rrggbbaa
                return true;
            }

            if (rgb.Match(hex).Success)
            {
                // #rgb
                return true;
            }

            if (rgba.Match(hex).Success)
            {
                // #rgba
                return true;
            }

            return false;
        }

        public static ParsedTheme CreateFromParsedTheme(
            List<ParsedThemeRule> source,
            ColorMap colorMap)
        {
            return ResolveParsedThemeRules(source, colorMap);
        }

        /**
         * Resolve rules (i.e. inheritance).
         */
        static ParsedTheme ResolveParsedThemeRules(
            List<ParsedThemeRule> parsedThemeRules,
            ColorMap colorMap)
        {
            // Sort rules lexicographically, and then by index if necessary
            parsedThemeRules.Sort((a, b) =>
            {
                int r = CompareUtils.Strcmp(a.scope, b.scope);
                if (r != 0)
                {
                    return r;
                }
                r = CompareUtils.StrArrCmp(a.parentScopes, b.parentScopes);
                if (r != 0)
                {
                    return r;
                }
                return a.index.CompareTo(b.index);
            });

            // Determine defaults
            int defaultFontStyle = FontStyle.None;
            string defaultForeground = "#000000";
            string defaultBackground = "#ffffff";
            while (parsedThemeRules.Count >= 1 && "".Equals(parsedThemeRules[0].scope))
            {
                ParsedThemeRule incomingDefaults = parsedThemeRules[0];
                parsedThemeRules.RemoveAt(0); // shift();
                if (incomingDefaults.fontStyle != FontStyle.NotSet)
                {
                    defaultFontStyle = incomingDefaults.fontStyle;
                }
                if (incomingDefaults.foreground != null)
                {
                    defaultForeground = incomingDefaults.foreground;
                }
                if (incomingDefaults.background != null)
                {
                    defaultBackground = incomingDefaults.background;
                }
            }
            ThemeTrieElementRule defaults = new ThemeTrieElementRule(0, null, defaultFontStyle,
                    colorMap.GetId(defaultForeground), colorMap.GetId(defaultBackground));

            ThemeTrieElement root = new ThemeTrieElement(new ThemeTrieElementRule(0, null, FontStyle.NotSet, 0, 0),
                    new List<ThemeTrieElementRule>());
            foreach (ParsedThemeRule rule in parsedThemeRules)
            {
                root.Insert(0, rule.scope, rule.parentScopes, rule.fontStyle, colorMap.GetId(rule.foreground),
                        colorMap.GetId(rule.background));
            }

            return new ParsedTheme(defaults, root);
        }

        ParsedTheme(ThemeTrieElementRule defaults, ThemeTrieElement root)
        {
            this.root = root;
            this.defaults = defaults;
            cache = new Dictionary<string, List<ThemeTrieElementRule>>();
        }

        internal List<ThemeTrieElementRule> Match(string scopeName)
        {
            if (!this.cache.ContainsKey(scopeName))
            {
                this.cache[scopeName] = this.root.Match(scopeName);
            }
            return this.cache[scopeName];
        }

        internal ThemeTrieElementRule GetDefaults()
        {
            return this.defaults;
        }
    }
}