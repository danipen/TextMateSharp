using System.Collections.Generic;

using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars
{
    public class ScopeMetadata
    {

        public string scopeName;
        public int languageId;
        public int tokenType;
        public List<ThemeTrieElementRule> themeData;

        public ScopeMetadata(string scopeName, int languageId, int tokenType, List<ThemeTrieElementRule> themeData)
        {
            this.scopeName = scopeName;
            this.languageId = languageId;
            this.tokenType = tokenType;
            this.themeData = themeData;
        }
    }
}