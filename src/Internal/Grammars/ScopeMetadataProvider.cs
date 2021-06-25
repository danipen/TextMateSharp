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

        private int initialLanguage;
        private IThemeProvider themeProvider;
        private Dictionary<string, ScopeMetadata> cache;
        private ScopeMetadata defaultMetaData;
        private Dictionary<string, int> embeddedLanguages;
        private Regex embeddedLanguagesRegex;

        public ScopeMetadataProvider(int initialLanguage, IThemeProvider themeProvider,
            Dictionary<string, int> embeddedLanguages)
        {
            this.initialLanguage = initialLanguage;
            this.themeProvider = themeProvider;
            this.cache = new Dictionary<string, ScopeMetadata>();
            this.OnDidChangeTheme();

            // embeddedLanguages handling
            this.embeddedLanguages = new Dictionary<string, int>();
            if (embeddedLanguages != null)
            {
                foreach (string scope in embeddedLanguages.Keys)
                {
                    int languageId = embeddedLanguages[scope];
                    this.embeddedLanguages[scope] = languageId;
                }
            }

            // create the regex
            /*Set<string> escapedScopes = this.embeddedLanguages.keySet().stream()
                .map(ScopeMetadataProvider::escapeRegExpCharacters)
                .collect(Collectors.toSet());*/

            //if (escapedScopes.isEmpty()) {
                // no scopes registered
            this.embeddedLanguagesRegex = null;
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
            this.cache.Clear();
            this.defaultMetaData = new ScopeMetadata(
                "",
                this.initialLanguage,
                StandardTokenType.Other,
                new List<ThemeTrieElementRule>() { this.themeProvider.GetDefaults() });
        }

        public ScopeMetadata GetDefaultMetadata()
        {
            return this.defaultMetaData;
        }

        private static string EscapeRegExpCharacters(string value)
        {
            // TODO!!!
            return value; //value.replace(/[\-\\\{\}\*\+\?\|\^\$\.\,\[\]\(\)\#\s]/g, '\\$&');
        }

        public ScopeMetadata getMetadataForScope(string scopeName)
        {
            if (scopeName == null)
            {
                return ScopeMetadataProvider._NULL_SCOPE_METADATA;
            }
            ScopeMetadata value;
            this.cache.TryGetValue(scopeName, out value);
            if (value != null)
            {
                return value;
            }
            value = this.DoGetMetadataForScope(scopeName);
            this.cache[scopeName] = value;
            return value;
        }

        private ScopeMetadata DoGetMetadataForScope(string scopeName)
        {
            int languageId = this.ScopeToLanguage(scopeName);
            int standardTokenType = ScopeMetadataProvider.ToStandardTokenType(scopeName);
            List<ThemeTrieElementRule> themeData = this.themeProvider.ThemeMatch(scopeName);

            return new ScopeMetadata(scopeName, languageId, standardTokenType, themeData);
        }

        private int ScopeToLanguage(string scope)
        {
            if (scope == null)
            {
                return 0;
            }
            if (this.embeddedLanguagesRegex == null)
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