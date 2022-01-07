using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Themes.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Tests.Resources;
using TextMateSharp.Themes;

namespace TextMateSharp.Tests.Internal.Themes.Reader
{
    class ThemeReaderTests
    {
        [Test]
        public void TestReadTheme()
        {
            Registry.Registry registry = new Registry.Registry(
                new TestRegistry("dark_vs.json"));

            Assert.AreEqual(15, registry.GetColorMap().Count);
        }


        [Test]
        public void TestIncludeTheme()
        {
            Registry.Registry registry = new Registry.Registry(
                new TestRegistry("dark_plus.json"));

            Assert.AreEqual(20, registry.GetColorMap().Count);
        }

        class TestRegistry : IRegistryOptions
        {
            private string _theme;

            internal TestRegistry(string theme)
            {
                _theme = theme;
            }
            public IRawTheme GetTheme(string scopeName)
            {
                if (scopeName.StartsWith("./"))
                    scopeName = scopeName.Replace("./", string.Empty);

                using var stream = ResourceReader.OpenStream(GetFilePath(scopeName));
                using var reader = new StreamReader(stream);
                return ThemeReader.ReadThemeSync(reader);
            }


            public IRawGrammar GetGrammar(string scopeName)
            {
                if (scopeName.StartsWith("./"))
                    scopeName = scopeName.Replace("./", string.Empty);

                using var stream = ResourceReader.OpenStream(GetFilePath(scopeName));
                using var reader = new StreamReader(stream);
                return GrammarReader.ReadGrammarSync(reader);
            }

            ICollection<string> IRegistryOptions.GetInjections(string scopeName)
            {
                return null;
            }

            string GetFilePath(string scopeName)
            {
                return scopeName;
            }

            IRawTheme IRegistryOptions.GetDefaultTheme()
            {
                using (Stream stream = ResourceReader.OpenStream(_theme))
                using (StreamReader reader = new StreamReader(stream))
                {
                    return ThemeReader.ReadThemeSync(reader);
                }
            }
        }
    }
}
