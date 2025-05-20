using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TextMateSharp.Grammars.Resources;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Themes.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace TextMateSharp.Grammars
{
    public class RegistryOptions : IRegistryOptions
    {
        private readonly ThemeName _defaultTheme;

        private readonly Dictionary<string, GrammarDefinition> _availableGrammars
            = new Dictionary<string, GrammarDefinition>();

        public RegistryOptions(ThemeName defaultTheme)
        {
            _defaultTheme = defaultTheme;
            InitializeAvailableGrammars();
        }

        /// <summary>
        /// load from local folder like structure in project
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="overwrite"></param>
        public void LoadFromLocalDir(string dirPath, bool overwrite = false)
        {
            var directories = new DirectoryInfo(dirPath).GetDirectories();
            foreach (var directory in directories)
            {
                var grammar = directory.Name.ToUpper();
                var packageFileInfo = directory.GetFiles("package.json").FirstOrDefault();
                if (packageFileInfo == null)
                    continue;
                LoadFromLocalFile(grammar, packageFileInfo, overwrite);
            }
        }


        /// <summary>
        /// load from local file
        /// </summary>
        /// <param name="grammarName"></param>
        /// <param name="packageJsonFileInfo"></param>
        /// <param name="overwrite"></param>
        public void LoadFromLocalFile(string grammarName, string packageJsonFileInfo, bool overwrite = false)
        {
            LoadFromLocalFile(grammarName, new FileInfo(packageJsonFileInfo), overwrite);
        }

        /// <summary>
        /// load from local file
        /// </summary>
        /// <param name="grammarName"></param>
        /// <param name="packageJsonFileInfo"></param>
        /// <param name="overwrite"></param>
        public void LoadFromLocalFile(string grammarName, FileInfo packageJsonFileInfo, bool overwrite = false)
        {
            if (_availableGrammars.ContainsKey(grammarName) && !overwrite)
            {
                return;
            }

            if (!packageJsonFileInfo.Exists)
                return;
            var baseDir = packageJsonFileInfo.Directory?.FullName ?? string.Empty;
            using (Stream stream = packageJsonFileInfo.OpenRead())
            {
                var definition = JsonSerializer.Deserialize(
                    stream, JsonSerializationContext.Default.GrammarDefinition);
                if (definition == null)
                {
                    return;
                }

                foreach (var language in definition.Contributes.Languages)
                {
                    if (language.ConfigurationFile == null)
                    {
                        language.Configuration = null;
                        continue;
                    }

                    var path = Path.GetFullPath(Path.Combine(baseDir, language.ConfigurationFile));
                    language.Configuration = LanguageConfiguration.LoadFromLocal(path);
                }

                if (definition.Contributes?.Snippets != null)
                {
                    definition.LanguageSnippets = new LanguageSnippets();
                    foreach (var snippet in definition.Contributes.Snippets)
                    {
                        var path = Path.GetFullPath(Path.Combine(baseDir, snippet.Path));
                        var configuration = LanguageSnippets.LoadFromLocal(path);
                        if (configuration == null) continue;
                        definition.LanguageSnippets = configuration;
                        break;
                    }
                }

                _availableGrammars.Add(grammarName, definition);
            }
        }

        public List<Language> GetAvailableLanguages()
        {
            List<Language> result = new List<Language>();

            foreach (GrammarDefinition definition in _availableGrammars.Values)
            {
                foreach (Language language in definition.Contributes.Languages)
                {
                    if (language.Aliases == null || language.Aliases.Count == 0)
                        continue;

                    if (!HasGrammar(language.Id, definition.Contributes.Grammars))
                        continue;

                    result.Add(language);
                }
            }

            return result;
        }

        public IEnumerable<GrammarDefinition> GetAvailableGrammarDefinitions()
        {
            return new List<GrammarDefinition>(_availableGrammars.Values);
        }

        public Language GetLanguageByExtension(string extension)
        {
            foreach (GrammarDefinition definition in _availableGrammars.Values)
            {
                foreach (var language in definition.Contributes.Languages)
                {
                    if (language.Extensions == null)
                        continue;

                    foreach (var languageExtension in language.Extensions)
                    {
                        if (extension.Equals(languageExtension,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            return language;
                        }
                    }
                }
            }

            return null;
        }

        public string GetScopeByExtension(string extension)
        {
            foreach (GrammarDefinition definition in _availableGrammars.Values)
            {
                foreach (var language in definition.Contributes.Languages)
                {
                    if (language.Extensions == null)
                        continue;

                    foreach (var languageExtension in language.Extensions)
                    {
                        if (extension.Equals(languageExtension,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var grammar in definition.Contributes.Grammars)
                            {
                                return grammar.ScopeName;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public string GetScopeByLanguageId(string languageId)
        {
            if (string.IsNullOrEmpty(languageId))
                return null;

            foreach (GrammarDefinition definition in _availableGrammars.Values)
            {
                foreach (var grammar in definition.Contributes.Grammars)
                {
                    if (languageId.Equals(grammar.Language))
                        return grammar.ScopeName;
                }
            }

            return null;
        }

        public IRawTheme LoadTheme(ThemeName name)
        {
            return GetTheme(GetThemeFile(name));
        }

        public ICollection<string> GetInjections(string scopeName)
        {
            return null;
        }

        public IRawTheme GetTheme(string scopeName)
        {
            Stream themeStream = ResourceLoader.TryOpenThemeStream(scopeName.Replace("./", string.Empty));

            if (themeStream == null)
                return null;

            using (themeStream)
            using (StreamReader reader = new StreamReader(themeStream))
            {
                return ThemeReader.ReadThemeSync(reader);
            }
        }

        public IRawGrammar GetGrammar(string scopeName)
        {
            Stream grammarStream = ResourceLoader.TryOpenGrammarStream(GetGrammarFile(scopeName));

            if (grammarStream == null)
                return null;

            using (grammarStream)
            using (StreamReader reader = new StreamReader(grammarStream))
            {
                return GrammarReader.ReadGrammarSync(reader);
            }
        }

        public IRawTheme GetDefaultTheme()
        {
            return LoadTheme(_defaultTheme);
        }

        void InitializeAvailableGrammars()
        {
            foreach (string grammar in GrammarNames.SupportedGrammars)
            {
                using (Stream stream = ResourceLoader.OpenGrammarPackage(grammar))
                {
                    GrammarDefinition definition = JsonSerializer.Deserialize(
                        stream,
                        JsonSerializationContext.Default.GrammarDefinition);

                    foreach (var language in definition.Contributes.Languages)
                    {
                        language.Configuration = LanguageConfiguration.Load(
                            grammar,
                            language.ConfigurationFile);
                    }

                    definition.LanguageSnippets = LanguageSnippets.Load(
                        grammar,
                        definition.Contributes);

                    _availableGrammars.Add(grammar, definition);
                }
            }
        }

        string GetGrammarFile(string scopeName)
        {
            foreach (string grammarName in _availableGrammars.Keys)
            {
                GrammarDefinition definition = _availableGrammars[grammarName];

                foreach (Grammar grammar in definition.Contributes.Grammars)
                {
                    if (scopeName.Equals(grammar.ScopeName))
                    {
                        string grammarPath = grammar.Path;

                        if (grammarPath.StartsWith("./"))
                            grammarPath = grammarPath.Substring(2);

                        grammarPath = grammarPath.Replace("/", ".");

                        return grammarName.ToLower() + "." + grammarPath;
                    }
                }
            }

            return null;
        }

        string GetThemeFile(ThemeName name)
        {
            switch (name)
            {
                case ThemeName.Abbys:
                    return "abyss-color-theme.json";
                case ThemeName.Dark:
                    return "dark_vs.json";
                case ThemeName.DarkPlus:
                    return "dark_plus.json";
                case ThemeName.DimmedMonokai:
                    return "dimmed-monokai-color-theme.json";
                case ThemeName.KimbieDark:
                    return "kimbie-dark-color-theme.json";
                case ThemeName.Light:
                    return "light_vs.json";
                case ThemeName.LightPlus:
                    return "light_plus.json";
                case ThemeName.Monokai:
                    return "monokai-color-theme.json";
                case ThemeName.Onedark:
                    return "onedark-color-theme.json";
                case ThemeName.QuietLight:
                    return "quietlight-color-theme.json";
                case ThemeName.Red:
                    return "Red-color-theme.json";
                case ThemeName.SolarizedDark:
                    return "solarized-dark-color-theme.json";
                case ThemeName.SolarizedLight:
                    return "solarized-light-color-theme.json";
                case ThemeName.TomorrowNightBlue:
                    return "tomorrow-night-blue-color-theme.json";
                case ThemeName.HighContrastLight:
                    return "hc_light.json";
                case ThemeName.HighContrastDark:
                    return "hc_black.json";
                case ThemeName.AtomOneLight:
                    return "atom-one-light-color-theme.json";
            }

            return null;
        }

        static bool HasGrammar(string id, List<Grammar> grammars)
        {
            foreach (Grammar grammar in grammars)
            {
                if (id == grammar.Language)
                    return true;
            }

            return false;
        }
    }
}