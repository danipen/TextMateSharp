using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using TextMateSharp.Internal.Utils;
using TextMateSharp.Registry;

namespace TextMateSharp.Themes
{
    public class Theme
    {
        private readonly ParsedTheme _theme;
        private readonly ParsedTheme _include;
        private readonly ColorMap _colorMap;
        private readonly Dictionary<string, string> _guiColorDictionary;
        private ReadOnlyDictionary<string, string> _cachedGuiColorDictionary;

        public static Theme CreateFromRawTheme(
            IRawTheme source,
            IRegistryOptions registryOptions)
        {
            ColorMap colorMap = new ColorMap();
            var guiColorsDictionary = new Dictionary<string, string>();

            var themeRuleList = ParsedTheme.ParseTheme(source, 0);

            ParsedTheme theme = ParsedTheme.CreateFromParsedTheme(
                themeRuleList,
                colorMap);

            IRawTheme themeInclude;
            ParsedTheme include = ParsedTheme.CreateFromParsedTheme(
                ParsedTheme.ParseInclude(source, registryOptions, 0, out themeInclude),
                colorMap);

            // First get colors from include, then try and overwrite with local colors..
            // I don't see this happening currently, but here just in case that ever happens.
            if (themeInclude != null)
            {
                ParsedTheme.ParsedGuiColors(themeInclude, guiColorsDictionary);
            }
            ParsedTheme.ParsedGuiColors(source, guiColorsDictionary);

            return new Theme(colorMap, theme, include, guiColorsDictionary);
        }

        Theme(ColorMap colorMap, ParsedTheme theme, ParsedTheme include, Dictionary<string, string> guiColorDictionary)
        {
            _colorMap = colorMap;
            _theme = theme;
            _include = include;
            _guiColorDictionary = guiColorDictionary;
        }

        public List<ThemeTrieElementRule> Match(IList<string> scopeNames)
        {
            List<ThemeTrieElementRule> result = new List<ThemeTrieElementRule>();

            for (int i = scopeNames.Count - 1; i >= 0; i--)
                result.AddRange(this._theme.Match(scopeNames[i]));

            for (int i = scopeNames.Count - 1; i >= 0; i--)
                result.AddRange(this._include.Match(scopeNames[i]));

            return result;
        }

        public ReadOnlyDictionary<string, string> GetGuiColorDictionary()
        {
            ReadOnlyDictionary<string, string> result = Volatile.Read(ref this._cachedGuiColorDictionary);
            if (result == null)
            {
                ReadOnlyDictionary<string, string> candidate = new ReadOnlyDictionary<string, string>(this._guiColorDictionary);
                result = Interlocked.CompareExchange(ref this._cachedGuiColorDictionary, candidate, null)
                         ?? candidate;
            }

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
        private readonly ThemeTrieElement _root;
        private readonly ThemeTrieElementRule _defaults;

        private readonly ConcurrentDictionary<string /* scopeName */, List<ThemeTrieElementRule>> _cachedMatchRoot;
        private const char SpaceChar = ' ';

        // Static sort comparison to avoid delegate allocation per sort call
        private static readonly Comparison<ParsedThemeRule> _themeRuleComparison = (a, b) =>
        {
            int r = StringUtils.StrCmp(a.scope, b.scope);
            if (r != 0)
                return r;

            r = StringUtils.StrArrCmp(a.parentScopes, b.parentScopes);
            if (r != 0)
                return r;

            return a.index.CompareTo(b.index);
        };

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

        internal static void ParsedGuiColors(IRawTheme source, Dictionary<string, string> colorDictionary)
        {
            var colors = source.GetGuiColors();
            if (colors == null)
            {
                return;
            }
            foreach (var kvp in colors)
            {
                colorDictionary[kvp.Key] = (string)kvp.Value;
            }
        }


        internal static List<ParsedThemeRule> ParseInclude(
            IRawTheme source,
            IRegistryOptions registryOptions,
            int priority,
            out IRawTheme themeInclude)
        {
            string include = source.GetInclude();

            if (string.IsNullOrEmpty(include))
            {
                themeInclude = null;
                return new List<ParsedThemeRule>();
            }

            themeInclude = registryOptions.GetTheme(include);

            if (themeInclude == null)
                return new List<ParsedThemeRule>();

            return ParseTheme(themeInclude, priority);
        }

        static void LookupThemeRules(
            ICollection<IRawThemeSetting> settings,
            List<ParsedThemeRule> parsedThemeRules,
            int priority)       // TODO: @danipen, 'priority' is currently unused. Is that intentional or is this a missing piece of functionality?
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
                List<string> scopes;
                const char separator = ',';
                if (settingScope is string scopeStr)
                {
                    // remove leading and trailing commas
                    ReadOnlySpan<char> trimmedScope = scopeStr.AsSpan().Trim(separator);

                    if (trimmedScope.Length == 0)
                    {
                        // Matches original behavior: String.Split with RemoveEmptyEntries on an empty
                        // string returns an empty array, so no scopes are produced and no rules are
                        // generated for this entry
                        scopes = new List<string>();
                    }
                    else
                    {
                        // Count commas to pre-size list and avoid over-allocation
                        int commaCount = 0;
                        for (int k = 0; k < trimmedScope.Length; k++)
                        {
                            if (trimmedScope[k] == separator)
                                commaCount++;
                        }

                        scopes = new List<string>(commaCount + 1);

                        // Span-based split avoids intermediate string[] allocation from String.Split
                        while (trimmedScope.Length > 0)
                        {
                            int commaIndex = trimmedScope.IndexOf(separator);
                            if (commaIndex < 0)
                            {
                                // ToString() allocates the final string required by ParsedThemeRule.
                                // This allocation is necessary as ParsedThemeRule stores string fields.
                                scopes.Add(trimmedScope.ToString());
                                break;
                            }

                            ReadOnlySpan<char> segment = trimmedScope.Slice(0, commaIndex);
                            if (segment.Length > 0)
                                // ToString() allocates the final string required by ParsedThemeRule.
                                // This allocation is necessary as ParsedThemeRule stores string fields.
                                scopes.Add(segment.ToString());

                            trimmedScope = trimmedScope.Slice(commaIndex + 1);
                        }
                    }
                }
                else if (settingScope is IList<object> scopeList)
                {
                    // Direct cast avoids LINQ Cast<string>() iterator/IEnumerable allocation
                    scopes = new List<string>(scopeList.Count);
                    for (int k = 0; k < scopeList.Count; k++)
                    {
                        scopes.Add((string)scopeList[k]);
                    }
                }
                else
                {
                    scopes = new List<string>(1);
                    scopes.Add(string.Empty);
                }

                FontStyle fontStyle = FontStyle.NotSet;
                object settingsFontStyle = entry.GetSetting().GetFontStyle();
                if (settingsFontStyle is string fontStyleStr)
                {
                    // Span-based parsing avoids string[] allocation from String.Split.
                    // Uses SequenceEqual for allocation-free keyword matching.
                    fontStyle = ParseFontStyle(fontStyleStr.AsSpan());
                }

                string foreground = null;
                object settingsForeground = entry.GetSetting().GetForeground();
                if (settingsForeground is string fgStr && StringUtils.IsValidHexColor(fgStr))
                {
                    foreground = fgStr;
                }

                string background = null;
                object settingsBackground = entry.GetSetting().GetBackground();
                if (settingsBackground is string bgStr && StringUtils.IsValidHexColor(bgStr))
                {
                    background = bgStr;
                }
                for (int j = 0, lenJ = scopes.Count; j < lenJ; j++)
                {
                    string _scope = scopes[j].Trim();

                    // Extract scope (last segment) and parentScopes (all segments reversed)
                    // in a single method call, eliminating redundant string scans from the
                    // previous LastIndexOf + Substring + BuildReversedSegments approach.
                    ExtractScopeAndParents(_scope, out string scope, out List<string> parentScopes);

                    string name = entry.GetName();

                    ParsedThemeRule t = new ParsedThemeRule(name, scope, parentScopes, i, fontStyle, foreground, background);
                    parsedThemeRules.Add(t);
                }
                i++;
            }
        }

        /// <summary>
        /// Parses a space-delimited font style string (e.g. "italic bold") into a <see cref="FontStyle"/>
        /// flags value without allocating a string[] from String.Split.
        /// Uses <see cref="MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{char}, ReadOnlySpan{char})"/> for allocation-free keyword matching.
        /// SequenceEqual checks length first internally, so no manual length pre-check is needed.
        /// </summary>
        private static FontStyle ParseFontStyle(ReadOnlySpan<char> value)
        {
            FontStyle fontStyle = FontStyle.None;
            while (value.Length > 0)
            {
                int spaceIndex = value.IndexOf(SpaceChar);
                ReadOnlySpan<char> segment;

                if (spaceIndex < 0)
                {
                    segment = value;
                    value = ReadOnlySpan<char>.Empty;
                }
                else
                {
                    segment = value.Slice(0, spaceIndex);
                    value = value.Slice(spaceIndex + 1);
                }

                if (segment.SequenceEqual("italic".AsSpan()))
                    fontStyle |= FontStyle.Italic;
                else if (segment.SequenceEqual("bold".AsSpan()))
                    fontStyle |= FontStyle.Bold;
                else if (segment.SequenceEqual("underline".AsSpan()))
                    fontStyle |= FontStyle.Underline;
                else if (segment.SequenceEqual("strikethrough".AsSpan()))
                    fontStyle |= FontStyle.Strikethrough;
            }

            return fontStyle;
        }

        /// <summary>
        /// Extracts the scope (last segment) and parentScopes (all segments in reverse order)
        /// from a space-delimited scope string using two linear passes over the input.
        /// The first pass counts segments and enables a fast path for single-segment scopes;
        /// the second pass walks backward once to extract the scope and parent scopes in reverse order.
        /// This replaces the previous three-step approach (LastIndexOf + Substring + BuildReversedSegments),
        /// which scanned the string 3 times and allocated an extra Substring for the scope.
        /// 
        /// Note: Substring allocations are necessary here because ParsedThemeRule and ThemeTrieElement
        /// store and operate on string fields. While ReadOnlySpan is used for parsing efficiency,
        /// the final strings must be allocated for storage in the theme data structures.
        /// Further allocation reduction would require architectural changes to use ReadOnlyMemory&lt;char&gt;
        /// or string pooling throughout the theme infrastructure.
        /// </summary>
        /// <param name="value">The space-delimited scope string (e.g. "text.html.basic source.js").</param>
        /// <param name="scope">The last segment (e.g. "source.js").</param>
        /// <param name="parentScopes">All segments in reverse order, or null if single-segment.</param>
        private static void ExtractScopeAndParents(string value, out string scope, out List<string> parentScopes)
        {
            ReadOnlySpan<char> span = value.AsSpan();

            // Count segments with a single forward pass
            int segmentCount = 1;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == SpaceChar)
                    segmentCount++;
            }

            // Fast path: single-segment scope (most common case) avoids all further work
            if (segmentCount == 1)
            {
                scope = value;
                parentScopes = null;
                return;
            }

            parentScopes = new List<string>(segmentCount);

            // Walk backwards through the span to build the reversed segment list.
            // The first segment encountered (rightmost in original string) is the scope.
            int end = span.Length;
            scope = null;

            for (int i = span.Length - 1; i >= 0; i--)
            {
                if (span[i] == SpaceChar)
                {
                    // Substring allocates a new string. This is necessary because downstream
                    // consumers (ParsedThemeRule, ThemeTrieElement) store and operate on strings
                    string segment = value.Substring(i + 1, end - i - 1);
                    scope ??= segment;
                    parentScopes.Add(segment);
                    end = i;
                }
            }

            // Add first (leftmost) segment. Substring allocation is necessary for storage
            string firstSegment = value.Substring(0, end);
            parentScopes.Add(firstSegment);
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
            parsedThemeRules.Sort(_themeRuleComparison);

            // Determine defaults
            FontStyle defaultFontStyle = FontStyle.None;
            string defaultForeground = "#000000";
            string defaultBackground = "#ffffff";

            // Use an index cursor instead of RemoveAt(0) which is O(n) due to array shifting
            int startIndex = 0;
            while (startIndex < parsedThemeRules.Count && string.IsNullOrEmpty(parsedThemeRules[startIndex].scope))
            {
                ParsedThemeRule incomingDefaults = parsedThemeRules[startIndex];
                startIndex++;

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
            ThemeTrieElementRule defaults = new ThemeTrieElementRule(string.Empty, 0, null, defaultFontStyle,
                    colorMap.GetId(defaultForeground), colorMap.GetId(defaultBackground));

            ThemeTrieElement root = new ThemeTrieElement(new ThemeTrieElementRule(string.Empty, 0, null, FontStyle.NotSet, 0, 0),
                    new List<ThemeTrieElementRule>());

            // Iterate from startIndex to skip already-processed default rules
            for (int i = startIndex; i < parsedThemeRules.Count; i++)
            {
                ParsedThemeRule rule = parsedThemeRules[i];
                root.Insert(rule.name, 0, rule.scope, rule.parentScopes, rule.fontStyle, colorMap.GetId(rule.foreground),
                        colorMap.GetId(rule.background));
            }
            return new ParsedTheme(defaults, root);
        }

        ParsedTheme(ThemeTrieElementRule defaults, ThemeTrieElement root)
        {
            this._root = root;
            this._defaults = defaults;
            this._cachedMatchRoot = new ConcurrentDictionary<string, List<ThemeTrieElementRule>>();
        }

        internal List<ThemeTrieElementRule> Match(string scopeName)
        {
            if (scopeName == null) throw new ArgumentNullException(nameof(scopeName));

            // TryGetValue + TryAdd pattern avoids the Func<> delegate allocation that
            // ConcurrentDictionary.GetOrAdd(key, factory) would incur on every call
            // (even on cache hits) due to the lambda capturing 'this'.
            if (!this._cachedMatchRoot.TryGetValue(scopeName, out List<ThemeTrieElementRule> value))
            {
                // Compute the value locally, then attempt to cache it. If another thread
                // wins the race to add the value for this scopeName, read back the value
                // that actually ended up in the cache to ensure consistency.
                value = this._root.Match(scopeName);
                if (!this._cachedMatchRoot.TryAdd(scopeName, value))
                {
                    if (!this._cachedMatchRoot.TryGetValue(scopeName, out value))
                    {
                        // In the unlikely event the key was removed between TryAdd and TryGetValue,
                        // recompute to ensure a non-null result is always returned
                        value = this._root.Match(scopeName);
                    }
                }
            }
            return value;
        }

        internal ThemeTrieElementRule GetDefaults()
        {
            return this._defaults;
        }
    }
}