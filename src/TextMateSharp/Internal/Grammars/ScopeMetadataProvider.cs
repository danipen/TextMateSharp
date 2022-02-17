using System.Collections.Generic;
using System.Text.RegularExpressions;

using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars
{
    public class ScopeMetadataProvider
    {

        private static ScopeMetadata _NULL_SCOPE_METADATA = new ScopeMetadata("", 0, StandardTokenType.Other, null);

        private static Regex STANDARD_TOKEN_TYPE_REGEXP = new Regex("\\b(comment|string|regex)\\b");
        private const string COMMENT_TOKEN_TYPE = "comment";
        private const string STRING_TOKEN_TYPE = "string";
        private const string REGEX_TOKEN_TYPE = "regex";

        private int _initialLanguage;
        private IThemeProvider _themeProvider;
        private Dictionary<string, ScopeMetadata> _cache;
        private ScopeMetadata _defaultMetaData;
        private Dictionary<string, int> _embeddedLanguages;
        private Regex _embeddedLanguagesRegex;

        public ScopeMetadataProvider(int initialLanguage, IThemeProvider themeProvider,
            Dictionary<string, int> embeddedLanguages)
        {
            this._initialLanguage = initialLanguage;
            this._themeProvider = themeProvider;
            this._cache = new Dictionary<string, ScopeMetadata>();
            this.OnDidChangeTheme();

            // embeddedLanguages handling
            this._embeddedLanguages = new Dictionary<string, int>();
            if (embeddedLanguages != null)
            {
                foreach (string scope in embeddedLanguages.Keys)
                {
                    int languageId = embeddedLanguages[scope];
                    this._embeddedLanguages[scope] = languageId;
                }
            }

            // create the regex
            /*Set<string> escapedScopes = this.embeddedLanguages.keySet().stream()
                .map(ScopeMetadataProvider::escapeRegExpCharacters)
                .collect(Collectors.toSet());*/

            //if (escapedScopes.isEmpty()) {
                // no scopes registered
            this._embeddedLanguagesRegex = null;
            //} else {
                // TODO!!!
                //this.embeddedLanguagesRegex = null;
                // escapedScopes.sort();
                // escapedScopes.reverse();
                // this._embeddedLanguagesRegex = new
                // RegExp(`^((${escapedScopes.join(')|(')}))($|\\.)`, '');
            //}
        }

        public void OnDidChangeTheme()
        {
            this._cache.Clear();
            this._defaultMetaData = new ScopeMetadata(
                "",
                this._initialLanguage,
                StandardTokenType.Other,
                new List<ThemeTrieElementRule>() { this._themeProvider.GetDefaults() });
        }

        public ScopeMetadata GetDefaultMetadata()
        {
            return this._defaultMetaData;
        }

        private static string EscapeRegExpCharacters(string value)
        {
            // TODO!!!
            return value; //value.replace(/[\-\\\{\}\*\+\?\|\^\$\.\,\[\]\(\)\#\s]/g, '\\$&');
        }

        public ScopeMetadata GetMetadataForScope(string scopeName)
        {
            if (scopeName == null)
            {
                return ScopeMetadataProvider._NULL_SCOPE_METADATA;
            }
            ScopeMetadata value;
            this._cache.TryGetValue(scopeName, out value);
            if (value != null)
            {
                return value;
            }
            value = this.DoGetMetadataForScope(scopeName);
            this._cache[scopeName] = value;
            return value;
        }

        private ScopeMetadata DoGetMetadataForScope(string scopeName)
        {
            int languageId = this.ScopeToLanguage(scopeName);
            int standardTokenType = ScopeMetadataProvider.ToStandardTokenType(scopeName);
            List<ThemeTrieElementRule> themeData = this._themeProvider.ThemeMatch(new string[] { scopeName });

            return new ScopeMetadata(scopeName, languageId, standardTokenType, themeData);
        }

        private int ScopeToLanguage(string scope)
        {
            if (scope == null)
            {
                return 0;
            }
            if (this._embeddedLanguagesRegex == null)
            {
                // no scopes registered
                return 0;
            }

            // TODO!!!!

            /*let m = scope.match(this._embeddedLanguagesRegex);
			if (!m) {
				// no scopes matched
				return 0;
			}

			let language = this._embeddedLanguages[m[1]] || 0;
			if (!language) {
				return 0;
			}

			return language;*/
            return 0;
        }

        private static int ToStandardTokenType(string tokenType)
        {
            Match m = STANDARD_TOKEN_TYPE_REGEXP.Match(tokenType); // tokenType.match(ScopeMetadataProvider.STANDARD_TOKEN_TYPE_REGEXP);
            if (!m.Success)
            {
                return StandardTokenType.Other;
            }
            string group = m.Value;
            if (COMMENT_TOKEN_TYPE.Equals(group))
            {
                return StandardTokenType.Comment;
            }
            else if (STRING_TOKEN_TYPE.Equals(group))
            {
                return StandardTokenType.String;
            }
            if (REGEX_TOKEN_TYPE.Equals(group))
            {
                return StandardTokenType.RegEx;
            }
            throw new TMException("Unexpected match for standard token type!");
        }
    }
}