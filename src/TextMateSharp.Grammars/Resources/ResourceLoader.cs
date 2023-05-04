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

        internal static Stream TryOpenLanguageConfiguration(string grammarName, string configurationFileName)
        {
            configurationFileName = configurationFileName.Replace('/', '.').TrimStart('.');
            string grammarPackage = GrammarPrefix + grammarName.ToLowerInvariant() + "." + configurationFileName;

            var result = typeof(ResourceLoader).GetTypeInfo().Assembly.GetManifestResourceStream(
                grammarPackage);

            return result;
        }

        internal static Stream TryOpenLanguageSnippet(string grammarName, string snippetFileName)
        {
            snippetFileName = snippetFileName.Replace('/', '.').TrimStart('.');
            string snippetPackage = SnippetPrefix + grammarName.ToLowerInvariant() + "." + snippetFileName;

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
