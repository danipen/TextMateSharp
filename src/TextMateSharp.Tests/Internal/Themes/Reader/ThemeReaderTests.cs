using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;
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
            private IThemeResolver _themeResolver;
            private IGrammarResolver _grammarResolver;
            public IThemeResolver ThemeResolver { get => _themeResolver; set => _themeResolver = value; }
            public IGrammarResolver GrammarResolver { get => _grammarResolver; set => _grammarResolver = value; }

            internal TestRegistry(string theme)
            {
                _theme = theme;
                _themeResolver = new DemoThemeResolver(GetFilePath);
                _grammarResolver = new DemoGrammarResolver(GetFilePath);
            }
            class DemoThemeResolver : IThemeResolver
            {
                private readonly Func<string, string> _getFilePathMethod;

                public DemoThemeResolver(Func<string, string> getFilePathMethod)
                {
                    this._getFilePathMethod = getFilePathMethod;
                }
                public IRawTheme GetTheme(string scopeName)
                {
                    if (scopeName.StartsWith("./"))
                        scopeName = scopeName.Replace("./", string.Empty);

                    using var stream = ResourceReader.OpenStream(_getFilePathMethod(scopeName));
                    using var reader = new StreamReader(stream);
                    return ThemeReader.ReadThemeSync(reader);
                }
            }
            class DemoGrammarResolver : IGrammarResolver
            {
                private readonly Func<string, string> _getFilePathMethod;

                public DemoGrammarResolver(Func<string, string> getFilePathMethod)
                {
                    this._getFilePathMethod = getFilePathMethod;
                }
                public IRawGrammar GetGrammar(string scopeName)
                {
                    if (scopeName.StartsWith("./"))
                        scopeName = scopeName.Replace("./", string.Empty);

                    using var stream = ResourceReader.OpenStream(_getFilePathMethod(scopeName));
                    using var reader = new StreamReader(stream);
                    return GrammarReader.ReadGrammarSync(reader);
                }
            }

            ICollection<string> IRegistryOptions.GetInjections(string scopeName)
            {
                return null;
            }

            string GetFilePath(string scopeName)
            {
                return scopeName;
            }

            IRawTheme IRegistryOptions.GetCurrentTheme()
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
