using System;

using NUnit.Framework;

namespace TextMateSharp.Grammars.Tests
{
    public class RegistryOptionsTests
    {
        [Test]
        public void Get_Available_Languages_Should_Return_Content()
        {
            RegistryOptions options = new RegistryOptions(ThemeName.Light);

            Assert.That(options.GetAvailableLanguages().Count, Is.EqualTo(61));
        }

        [Test]
        public void Get_Language_By_Extensions_Should_Return_Language()
        {
            RegistryOptions options = new RegistryOptions(ThemeName.Light);

            Assert.That(options.GetLanguageByExtension(".cs").Id, Is.EqualTo("csharp"));
        }

        [Test]
        public void Get_Scope_By_Extensions_Should_Return_Scope()
        {
            RegistryOptions options = new RegistryOptions(ThemeName.Light);

            Assert.That(options.GetScopeByExtension(".cs"), Is.EqualTo("source.cs"));
        }

        [Test]
        public void Get_Scope_By_Language_Id_Should_Return_Scope()
        {
            RegistryOptions options = new RegistryOptions(ThemeName.Light);

            Assert.That(options.GetScopeByLanguageId("csharp"), Is.EqualTo("source.cs"));
        }

        [Test]
        public void Load_Theme_Should_Return_Theme()
        {
            RegistryOptions options = new RegistryOptions(ThemeName.Light);

            foreach (ThemeName themeName in Enum.GetValues<ThemeName>())
            Assert.That(options.LoadTheme(themeName), Is.Not.Null, "Failed: " + themeName.ToString());
        }

        [Test]
        public void Get_Theme_By_Scrope_Name_Should_Return_Theme()
        {
            RegistryOptions options = new RegistryOptions(ThemeName.Light);

            Assert.That(options.GetTheme("./dark_vs.json"), Is.Not.Null);
        }

        [Test]
        public void Get_Grammar_By_Scope_Name_Should_Return_Grammar()
        {
            RegistryOptions options = new RegistryOptions(ThemeName.Light);

            foreach (var language in options.GetAvailableLanguages())
            {
                string scopeName = options.GetScopeByLanguageId(language.Id);
                Assert.That(options.GetGrammar(scopeName), Is.Not.Null, "Failed: " + language.Id);
            }
        }

        [Test]
        public void Get_Default_Theme_Should_Return_Current_Theme()
        {
            RegistryOptions options = new RegistryOptions(ThemeName.Light);

            Assert.IsTrue(options.GetDefaultTheme().GetName().Contains("Light"));
        }

        [Test]
        public void Assert_Every_Grammar_Parses_A_Line()
        {
            string sampleLine = "sample line";

            RegistryOptions options = new RegistryOptions(ThemeName.Light);

            Registry.Registry registry = new Registry.Registry(options);

            foreach (var language in options.GetAvailableLanguages())
            {
                try
                {
                    string scopeName = options.GetScopeByLanguageId(language.Id);
                    IGrammar grammar = registry.LoadGrammar(scopeName);
                    Assert.That(grammar.TokenizeLine(sampleLine), Is.Not.Null, "Failed: " + language.Id);
                }
                catch (Exception ex)
                {
                    Assert.Fail(
                        string.Format("[{0} grammar]: {1}", language.Id, ex.Message));
                }
            }
        }

        [Test]
        public void Assert_Every_Grammar_With_Language_Configuration_File_Has_Language_Configuration()
        {
            RegistryOptions options = new RegistryOptions(ThemeName.Light);

            foreach (var language in options.GetAvailableLanguages())
            {
                try
                {
                    if (!string.IsNullOrEmpty(language.ConfigurationFile))
                    {
                        Assert.That(language.Configuration, Is.Not.Null);
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail(
                        string.Format("[{0} grammar]: {1}", language.Id, ex.Message));
                }
            }
        }

        [Test]
        public void Assert_Every_Grammar_With_Snippets_Can_Load_Snippets()
        {
            RegistryOptions options = new RegistryOptions(ThemeName.Light);

            foreach (var grammarDefinition in options.GetAvailableGrammarDefinitions())
            {
                try
                {
                    if (grammarDefinition.Contributes == null || grammarDefinition.Contributes.Snippets == null)
                        continue;

                    Assert.That(grammarDefinition.LanguageSnippets, Is.Not.Null);
                }
                catch (Exception ex)
                {
                    Assert.Fail(
                        string.Format("[{0} grammar definition]: {1}", grammarDefinition.Name, ex.Message));
                }
            }
        }
    }
}