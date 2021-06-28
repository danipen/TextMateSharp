using System.Collections.Generic;
using System.IO;

using TextMateSharp.Internal.Parser.Json;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Themes.Reader
{
    public class ThemeReader
    {
        public static IRawTheme ReadThemeSync(StreamReader reader, IRegistryOptions registry)
        {
            IRawTheme result = ParseTheme(reader);

            string include = result.GetInclude();

            if (string.IsNullOrEmpty(include))
                return result;

            Stream stream = registry.GetInputStream(include);

            if (stream == null)
                return result;

            using (stream)
            using (StreamReader includeReader = new StreamReader(stream))
            {
                IRawTheme themeInclude = ParseTheme(includeReader);
                AddInclude(result, themeInclude);
                return result;
            }
        }

        static IRawTheme ParseTheme(StreamReader reader)
        {
            JSONPListParser<IRawTheme> parser = new JSONPListParser<IRawTheme>(true);
            return parser.Parse(reader);
        }

        static void AddInclude(IRawTheme theme, IRawTheme other)
        {
            if (other == null)
                return;

            theme.SetTokenColors(
                MergeSettings(theme.GetTokenColors(), other.GetTokenColors()));
            theme.SetSettings(
                MergeSettings(theme.GetSettings(), other.GetSettings()));
        }

        static ICollection<IRawThemeSetting> MergeSettings(
            ICollection<IRawThemeSetting> settings,
            ICollection<IRawThemeSetting> other)
        {
            if (settings == null)
                return null;

            if (other == null)
                return settings;

            foreach (IRawThemeSetting setting in other)
                settings.Add(setting);

            return settings;
        }
    }
}
