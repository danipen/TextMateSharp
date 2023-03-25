using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TextMateSharp.Grammars
{
    public class Engines
    {
        [JsonPropertyName("engines")]
        public string VsCode { get; set; }
    }

    public class Scripts
    {
        [JsonPropertyName("update-grammar")]
        public string UpdateGrammar { get; set; }
    }

    public class Language
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("extensions")]
        public List<string> Extensions { get; set; }
        [JsonPropertyName("aliases")]
        public List<string> Aliases { get; set; }
        [JsonPropertyName("configuration")]
        public string Configuration { get; set; }

        public override string ToString()
        {
            if (Aliases != null && Aliases.Count > 0)
                return string.Format("{0} ({1})", Aliases[0], Id);

            return Id;
        }
    }

    public class Grammar
    {
        [JsonPropertyName("language")]
        public string Language { get; set; }
        [JsonPropertyName("scopeName")]
        public string ScopeName { get; set; }
        [JsonPropertyName("path")]
        public string Path { get; set; }
    }

    public class Snippet
    {
        [JsonPropertyName("language")]
        public string Language { get; set; }
        [JsonPropertyName("path")]
        public string Path { get; set; }
    }

    public class Contributes
    {
        [JsonPropertyName("languages")]
        public List<Language> Languages { get; set; }
        [JsonPropertyName("grammars")]
        public List<Grammar> Grammars { get; set; }
        [JsonPropertyName("snippets")]
        public List<Snippet> Snippets { get; set; }
    }

    public class Repository
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class GrammarDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("version")]
        public string Version { get; set; }
        [JsonPropertyName("publisher")]
        public string Publisher { get; set; }
        [JsonPropertyName("license")]
        public string License { get; set; }
        [JsonPropertyName("engines")]
        public Engines Engines { get; set; }
        [JsonPropertyName("scripts")]
        public Scripts Scripts { get; set; }
        [JsonPropertyName("contributes")]
        public Contributes Contributes { get; set; }
        [JsonPropertyName("repository")]
        public Repository Repository { get; set; }
    }
}
