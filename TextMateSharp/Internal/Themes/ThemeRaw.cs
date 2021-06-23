using System.Collections.Generic;

using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Themes
{
    public class ThemeRaw : Dictionary<string, object>, IRawTheme, IRawThemeSetting, IThemeSetting
    {
        public string GetName()
        {
            return (string)this["name"];
        }

        public ICollection<IRawThemeSetting> GetSettings()
        {
            return (ICollection<IRawThemeSetting>)this["settings"];
        }

        public object GetScope()
        {
            return this["scope"];
        }

        public IThemeSetting GetSetting()
        {
            return (IThemeSetting)this["settings"];
        }

        public object GetFontStyle()
        {
            return this["fontStyle"];
        }

        public string GetBackground()
        {
            return (string)this["background"];
        }

        public string GetForeground()
        {
            return (string)this["foreground"];
        }
    }
}