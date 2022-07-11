namespace TextMateSharp.Internal.Grammars
{
    public static class OptionalStandardTokenType
    {
        public static int Other = StandardTokenType.Other;
        public static int Comment = StandardTokenType.Comment;
        public static int String = StandardTokenType.String;
        public static int RegEx = StandardTokenType.RegEx;

        /**
        ** Indicates that no token type is set.
        **/
        public static int NotSet = 8;
    }
}
