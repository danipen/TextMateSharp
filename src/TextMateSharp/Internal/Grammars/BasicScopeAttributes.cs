using System.Collections.Generic;

using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars
{
    internal sealed class BasicScopeAttributes
    {
        internal int LanguageId { get; private set; }
        internal int TokenType { get; private set; } /* OptionalStandardTokenType */
        internal List<ThemeTrieElementRule> ThemeData { get; private set; }

        internal BasicScopeAttributes(
            int languageId,
            int tokenType,
            List<ThemeTrieElementRule> themeData)
        {
            LanguageId = languageId;
            TokenType = tokenType;
            ThemeData = themeData;
        }
    }
}