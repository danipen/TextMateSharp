using System;
using System.IO;
using System.Reflection;

namespace TextMateSharp.Grammars.Resources
{
    internal class ResourceLoader
    {
        const string GrammarPrefix = "TextMateSharp.Grammars.Resources.Grammars.";
        const string ThemesPrefix = "TextMateSharp.Grammars.Resources.Themes.";
        private const string SnippetPrefix = "TextMateSharp.Grammars.Resources.Grammars.";

        internal static Stream OpenGrammarPackage(string grammarName)
        {
            string grammarPackage = GrammarPrefix + grammarName.ToLowerInvariant() + "." + "package.json";

            var result = typeof(ResourceLoader).GetTypeInfo().Assembly.GetManifestResourceStream(
                grammarPackage);

            if (result == null)
                throw new FileNotFoundException("The grammar package '" + grammarPackage + "' was not found.");

            return result;
        }

        internal static Stream OpenLanguageConfiguration(string language, string configurationName)
        {
            var result = TryOpenLanguageConfiguration(language, configurationName);

            if (result == null)
                throw new FileNotFoundException("The language configuration for'" + language + "' was not found.");

            return result;
        }

        internal static Stream TryOpenLanguageConfiguration(string language, string configurationName)
        {
            string grammarPackage = GrammarPrefix + language.ToLowerInvariant() + "." + Path.GetFileName(configurationName);

            var result = typeof(ResourceLoader).GetTypeInfo().Assembly.GetManifestResourceStream(
                grammarPackage);

            return result;
        }

        internal static Stream OpenLanguageSnippet(string language, string snippetName)
        {
            var result = TryOpenLanguageSnippet(language, snippetName);
            if (result == null)
                throw new FileNotFoundException("The language snippet for'" + language + "' was not found.");

            return result;
        }

        internal static Stream TryOpenLanguageSnippet(string language, string snippetName)
        {
            snippetName = snippetName.Replace('/', '.').TrimStart('.');
            string snippetPackage = SnippetPrefix + language.ToLowerInvariant() + "." + snippetName;

            var result = typeof(ResourceLoader).GetTypeInfo().Assembly.GetManifestResourceStream(
                snippetPackage);

            return result;
        }

        internal static Stream TryOpenGrammarStream(string path)
        {
            return typeof(ResourceLoader).GetTypeInfo().Assembly.GetManifestResourceStream(
                GrammarPrefix + path);
        }

        internal static Stream TryOpenThemeStream(string path)
        {
            return typeof(ResourceLoader).GetTypeInfo().Assembly.GetManifestResourceStream(
                ThemesPrefix + path);
        }
    }
}
