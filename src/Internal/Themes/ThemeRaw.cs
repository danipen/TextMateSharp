using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
            ICollection result = TryGetObject<ICollection>("settings");

            if (result == null)
                return null;

            return result.Cast<IRawThemeSetting>().ToList();
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