using System;

namespace TextMateSharp.Themes
{
    [Flags]
    public enum FontStyle
    {
        NotSet = -1,

        // This can are bit-flags, so it can be `Italic | Bold`
        None = 0,
        Italic = 1,
        Bold = 2,
        Underline = 4,
        Strikethrough = 8
    }
}