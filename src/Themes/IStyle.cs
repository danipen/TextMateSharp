namespace TextMateSharp.Themes
{
   public interface IStyle
    {
        RGB GetColor();
        RGB GetBackgroundColor();
        bool IsBold();
        bool IsItalic();
        bool IsUnderline();
        bool IsStrikeThrough();
    }
}