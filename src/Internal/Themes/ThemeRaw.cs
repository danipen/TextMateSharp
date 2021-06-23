using System.Collections.Generic;

using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Themes
{
    public class ThemeRaw : Dictionary<string, object>, IRawTheme, IRawThemeSetting, IThemeSetting
    {
        public string GetName()
        {
            return TryGetObject<string>("name");
        }

        public ICollection<IRawThemeSetting> GetSettings()
        {
            return TryGetObject<ICollection<IRawThemeSetting>>("settings");
        }

        public object GetScope()
        {
            return TryGetObject<object>("scope");
        }

        public IThemeSetting GetSetting()
        {
            return TryGetObject<IThemeSetting>("settings");
        }

        public object GetFontStyle()
        {
            return TryGetObject<object>("fontStyle");
        }

        public string GetBackground()
        {
            return TryGetObject<string>("background");
        }

        public string GetForeground()
        {
            return TryGetObject<string>("foreground");
        }

        T TryGetObject<T>(string key)
        {
            object result;
            if (!TryGetValue(key, out result))
                return default(T);

            return (T)result;
        }
    }
}