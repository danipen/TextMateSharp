using System.Collections;
using System.Collections.Generic;
using System.Linq;

using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Themes
{
    public class ThemeRaw : Dictionary<string, object>, IRawTheme, IRawThemeSetting, IThemeSetting
    {
        private static string NAME = "name";
        private static string INCLUDE = "include";
        private static string SETTINGS = "settings";
        private static string TOKEN_COLORS = "tokenColors";
        private static string SCOPE = "scope";
        private static string FONT_STYLE = "fontStyle";
        private static string BACKGROUND = "background";
        private static string FOREGROUND = "foreground";

        public string GetName()
        {
            return TryGetObject<string>(NAME);
        }

        public string GetInclude()
        {
            return TryGetObject<string>(INCLUDE);
        }

        public ICollection<IRawThemeSetting> GetSettings()
        {
            ICollection result = TryGetObject<ICollection>(SETTINGS);

            if (result == null)
                return null;

            return result.Cast<IRawThemeSetting>().ToList();
        }

        public void SetSettings(ICollection<IRawThemeSetting> settings)
        {
            this[SETTINGS] = settings;
        }

        public ICollection<IRawThemeSetting> GetTokenColors()
        {
            ICollection result = TryGetObject<ICollection>(TOKEN_COLORS);

            if (result == null)
                return null;

            return result.Cast<IRawThemeSetting>().ToList();
        }

        public void SetTokenColors(ICollection<IRawThemeSetting> colors)
        {
            this[TOKEN_COLORS] = colors;
        }

        public object GetScope()
        {
            return TryGetObject<object>(SCOPE);
        }

        public IThemeSetting GetSetting()
        {
            return TryGetObject<IThemeSetting>(SETTINGS);
        }

        public object GetFontStyle()
        {
            return TryGetObject<object>(FONT_STYLE);
        }

        public string GetBackground()
        {
            return TryGetObject<string>(BACKGROUND);
        }

        public string GetForeground()
        {
            return TryGetObject<string>(FOREGROUND);
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