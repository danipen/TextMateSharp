namespace TextMateSharp.Themes
{
    public class FontStyle
    {
        public const int NotSet = -1;

        // This can are bit-flags, so it can be `Italic | Bold`
        public const int None = 0;
        public const int Italic = 1;
        public const int Bold = 2;
        public const int Underline = 4;
        public const int Strikethrough = 8;
    }
}