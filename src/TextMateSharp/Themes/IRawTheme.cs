using System.Collections.Generic;

namespace TextMateSharp.Themes
{
    public interface IRawTheme
    {
        string GetName();
        string GetInclude();
        ICollection<IRawThemeSetting> GetSettings();
        ICollection<IRawThemeSetting> GetTokenColors();
        void SetSettings(ICollection<IRawThemeSetting> settings);
        void SetTokenColors(ICollection<IRawThemeSetting> colors);
    }
}