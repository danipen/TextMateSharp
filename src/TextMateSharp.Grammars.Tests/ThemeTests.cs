using System;
using System.Collections.Generic;

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

        [Test]
        public void Theme_Match_Should_Add_Rule_Names()
        {
            RegistryOptions options = new RegistryOptions(ThemeName.DarkPlus);

            Registry.Registry registry = new Registry.Registry(options);

            Theme theme = registry.GetTheme();

            var rule = theme.Match(new List<string>() { "source.cs", "keyword.other.using.cs" });

            Assert.That(rule.Count, Is.GreaterThan(0));
            Assert.That(rule[0].name, Is.Not.Null.Or.Empty);
        }
    }
}
