using System.Collections.Generic;

namespace TextMateSharp.Themes
{
    public interface IRawTheme
    {
        string GetName();
        string GetInclude();
        ICollection<IRawThemeSetting> GetSettings();
        ICollection<IRawThemeSetting> GetTokenColors();
        ICollection<KeyValuePair<string,object>> GetGuiColors();
    }
}