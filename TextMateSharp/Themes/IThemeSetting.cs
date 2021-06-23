namespace TextMateSharp.Themes
{
    public interface IThemeSetting
    {
        object GetFontStyle();

        string GetBackground();

        string GetForeground();
    }
}