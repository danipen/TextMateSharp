namespace TextMateSharp.Themes
{
    public interface IRawThemeSetting
    {
        string GetName();

        object GetScope();

        IThemeSetting GetSetting();
    }
}