using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Themes
{
    public class Theme
    {
        private static Regex rrggbb = new Regex("^#[0-9a-f]{6}", RegexOptions.IgnoreCase);
        private static Regex rrggbbaa = new Regex("^#[0-9a-f]{8}", RegexOptions.IgnoreCase);
        private static Regex rgb = new Regex("^#[0-9a-f]{3}", RegexOptions.IgnoreCase);
        private static Regex rgba = new Regex("^#[0-9a-f]{4}", RegexOptions.IgnoreCase);

        private ColorMap colorMap;
        private ThemeTrieElement root;
        private ThemeTrieElementRule defaults;
        private Dictionary<string /* scopeName */, List<ThemeTrieElementRule>> cache;

        public static Theme CreateFromRawTheme(IRawTheme source)
        {
            return CreateFromParsedTheme(ParseTheme(source));
        }

        public static List<ParsedThemeRule> ParseTheme(IRawTheme source)
        {
            List<ParsedThemeRule> result = new List<ParsedThemeRule>();

            if (source == null)
                return result;

            // process theme rules in vscode-textmate format:
            // see https://github.com/microsoft/vscode-textmate/tree/main/test-cases/themes
            LookupThemeRules(source.GetSettings(), result);

            // process theme rules in vscode format
            // see https://github.com/microsoft/vscode/tree/main/extensions/theme-defaults/themes
            LookupThemeRules(source.GetTokenColors(), result);

            return result;
        }

        static void LookupThemeRules(
            ICollection<IRawThemeSetting> settings,
            List<ParsedThemeRule> parsedThemeRules)
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

                    List<string> segments = new(_scope.Split(new[] {" "}, StringSplitOptions.None));

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

        public static Theme CreateFromParsedTheme(List<ParsedThemeRule> source)
        {
            return ResolveParsedThemeRules(source);
        }

        /**
         * Resolve rules (i.e. inheritance).
         */
        public static Theme ResolveParsedThemeRules(List<ParsedThemeRule> parsedThemeRules)
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
                return a.index - b.index;
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
            ColorMap colorMap = new ColorMap();
            ThemeTrieElementRule defaults = new ThemeTrieElementRule(0, null, defaultFontStyle,
                    colorMap.GetId(defaultForeground), colorMap.GetId(defaultBackground));

            ThemeTrieElement root = new ThemeTrieElement(new ThemeTrieElementRule(0, null, FontStyle.NotSet, 0, 0),
                    new List<ThemeTrieElementRule>());
            foreach (ParsedThemeRule rule in parsedThemeRules)
            {
                root.Insert(0, rule.scope, rule.parentScopes, rule.fontStyle, colorMap.GetId(rule.foreground),
                        colorMap.GetId(rule.background));
            }

            return new Theme(colorMap, defaults, root);
        }

        public Theme(ColorMap colorMap, ThemeTrieElementRule defaults, ThemeTrieElement root)
        {
            this.colorMap = colorMap;
            this.root = root;
            this.defaults = defaults;
            this.cache = new Dictionary<string, List<ThemeTrieElementRule>>();
        }

        public IEnumerable<string> GetColorMap()
        {
            return this.colorMap.GetColorMap();
        }

        public string GetColor(int id)
        {
            return this.colorMap.GetColor(id);
        }

        public ThemeTrieElementRule GetDefaults()
        {
            return this.defaults;
        }

        public List<ThemeTrieElementRule> Match(string scopeName)
        {
            if (!this.cache.ContainsKey(scopeName))
            {
                this.cache[scopeName] = this.root.Match(scopeName);
            }
            return this.cache[scopeName];
        }

        public override int GetHashCode()
        {
            return cache.GetHashCode() +
                    colorMap.GetHashCode() +
                    defaults.GetHashCode() +
                    root.GetHashCode();
        }

        public bool equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null)
            {
                return false;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            Theme other = (Theme)obj;
            return Object.Equals(cache, other.cache) && Object.Equals(colorMap, other.colorMap) &&
                    Object.Equals(defaults, other.defaults) && Object.Equals(root, other.root);
        }
    }
}