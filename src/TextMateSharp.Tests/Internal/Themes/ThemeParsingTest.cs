using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TextMateSharp.Internal.Themes.Reader;
using TextMateSharp.Themes;

namespace TextMateSharp.Tests.Internal.Themes
{
    [TestFixture]
    internal class ThemeParsingTest
    {
        [TestCase()]
        public void Parse_Theme_Rule_Should_Work()
        {
            MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(THEME_JSON));

            StreamReader reader = new StreamReader(memoryStream);
            var theme = ThemeReader.ReadThemeSync(reader);

            var actualThemeRules = ParsedTheme.ParseTheme(theme, 0);

            var expectedThemeRules = new ParsedThemeRule[] {
                new ParsedThemeRule("", null, 0, FontStyle.NotSet, "#F8F8F2", "#272822"),
                new ParsedThemeRule("source", null, 1, FontStyle.NotSet, null, "#100000"),
                new ParsedThemeRule("something", null, 1, FontStyle.NotSet, null, "#100000"),
                new ParsedThemeRule("bar", null, 2, FontStyle.NotSet, null, "#010000"),
                new ParsedThemeRule("baz", null, 2, FontStyle.NotSet, null, "#010000"),
                new ParsedThemeRule("bar", new List<string>() {"bar", "selector", "source.css" }, 3, FontStyle.Bold, null, null),
                new ParsedThemeRule("constant", null, 4, FontStyle.Italic, "#ff0000", null),
                new ParsedThemeRule("constant.numeric", null, 5, FontStyle.NotSet, "#00ff00", null),
                new ParsedThemeRule("constant.numeric.hex", null, 6, FontStyle.Bold, null, null),
                new ParsedThemeRule("constant.numeric.oct", null, 7, FontStyle.Bold | FontStyle.Italic | FontStyle.Underline, null, null),
                new ParsedThemeRule("constant.numeric.bin", null, 8, FontStyle.Bold | FontStyle.Strikethrough, null, null),
                new ParsedThemeRule("constant.numeric.dec", null, 9, FontStyle.None, "#0000ff", null),
                new ParsedThemeRule("foo", null, 10, FontStyle.None, "#CFA", null)
            };

            Assert.AreEqual(expectedThemeRules.Length, actualThemeRules.Count);

            for (int i = 0; i< actualThemeRules.Count; i++)
            {
                Assert.AreEqual(expectedThemeRules[i], actualThemeRules[i]);
            }
        }

        const string THEME_JSON =
            "{ \"settings\": [" +
            "{ \"settings\": { \"foreground\": \"#F8F8F2\", \"background\": \"#272822\" } }," +
            "{ \"scope\": \"source, something\", \"settings\": { \"background\": \"#100000\" } }," +
            "{ \"scope\": [\"bar\", \"baz\"], \"settings\": { \"background\": \"#010000\" } }," +
            "{ \"scope\": \"source.css selector bar\", \"settings\": { \"fontStyle\": \"bold\" } }," +
            "{ \"scope\": \"constant\", \"settings\": { \"fontStyle\": \"italic\", \"foreground\": \"#ff0000\" } }," +
            "{ \"scope\": \"constant.numeric\", \"settings\": { \"foreground\": \"#00ff00\" } }," +
            "{ \"scope\": \"constant.numeric.hex\", \"settings\": { \"fontStyle\": \"bold\" } }," +
            "{ \"scope\": \"constant.numeric.oct\", \"settings\": { \"fontStyle\": \"bold italic underline\" } }," +
            "{ \"scope\": \"constant.numeric.bin\", \"settings\": { \"fontStyle\": \"bold strikethrough\" } }," +
            "{ \"scope\": \"constant.numeric.dec\", \"settings\": { \"fontStyle\": \"\", \"foreground\": \"#0000ff\" } }," +
            "{ \"scope\": \"foo\", \"settings\": { \"fontStyle\": \"\", \"foreground\": \"#CFA\" } }" +
            "]}";
    }
}
