using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using TextMateSharp.Internal.Themes.Reader;
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

            Stream IRegistryOptions.GetInputStream(string scopeName)
            {
                if (scopeName.StartsWith("./"))
                    scopeName = scopeName.Replace("./", string.Empty);

                return ResourceReader.OpenStream(
                    ((IRegistryOptions)this).GetFilePath(scopeName));
            }

            ICollection<string> IRegistryOptions.GetInjections(string scopeName)
            {
                return null;
            }

            string IRegistryOptions.GetFilePath(string scopeName)
            {
                return scopeName;
            }

            IRawTheme IRegistryOptions.GetTheme()
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
