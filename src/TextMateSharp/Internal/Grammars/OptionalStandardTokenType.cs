namespace TextMateSharp.Internal.Grammars
{
    public static class OptionalStandardTokenType
    {
        public static readonly int Other = StandardTokenType.Other;
        public static readonly int Comment = StandardTokenType.Comment;
        public static readonly int String = StandardTokenType.String;
        public static readonly int RegEx = StandardTokenType.RegEx;

        /**
        ** Indicates that no token type is set.
        **/
        public static readonly int NotSet = 8;
    }
}
