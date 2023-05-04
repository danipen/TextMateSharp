using System;

using NUnit.Framework;
using TextMateSharp.Themes;

namespace TextMateSharp.Grammars.Tests
{
    public class ThemeTests
    {
        [Test]
        public void Assert_Every_Theme_Can_Be_Parsed()
        {
            RegistryOptions options = new RegistryOptions(ThemeName.Light);

            foreach (var themeName in Enum.GetValues<ThemeName>())
            {
                try
                {
                    IRawTheme theme = options.LoadTheme(themeName);
                    Assert.That(theme.GetTokenColors(), Has.Count, "Failed: " + themeName);
                }
                catch (Exception ex)
                {
                    Assert.Fail(
                        string.Format("[{0} theme]: {1}", themeName, ex.Message));
                }
            }
        }
    }
}
