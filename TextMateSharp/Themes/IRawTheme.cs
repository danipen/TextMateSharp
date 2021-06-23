using System.Collections.Generic;

namespace TextMateSharp.Themes
{
    public interface IRawTheme
    {
        string GetName();

        ICollection<IRawThemeSetting> GetSettings();
    }
}