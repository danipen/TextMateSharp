using System.Collections.Generic;
using System.IO;

using SimpleJSON;

using TextMateSharp.Grammars.Resources;

namespace TextMateSharp.Grammars
{
    public class Engines
    {
        public string VsCode { get; set; }
    }

    public class Scripts
    {
        public string UpdateGrammar { get; set; }
    }

    public class Language
    {
        public string Id { get; set; }
        public List<string> Extensions { get; set; }
        public List<string> Aliases { get; set; }
        public string ConfigurationFile { get; set; }
        public LanguageConfiguration Configuration {get; set;}
        public List<string> MimeTypes { get; set; }

        public override string ToString()
        {
            if (Aliases != null && Aliases.Count > 0)
                return string.Format("{0} ({1})", Aliases[0], Id);

            return Id;
        }
    }

    public class Grammar
    {
        public string Language { get; set; }
        public string ScopeName { get; set; }
        public string Path { get; set; }
    }

    public class Snippet
    {
        public string Language { get; set; }
        public string Path { get; set; }
    }

    public class Contributes
    {
        public List<Language> Languages { get; set; }
        public List<Grammar> Grammars { get; set; }
        public List<Snippet> Snippets { get; set; }
    }

    public class Repository
    {
        public string Type { get; set; }
        public string Url { get; set; }
    }

    public class GrammarDefinition
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public string License { get; set; }
        public Engines Engines { get; set; }
        public Scripts Scripts { get; set; }
        public Contributes Contributes { get; set; }
        public Repository Repository { get; set; }
        public LanguageSnippets LanguageSnippets { get; set; }

        public static GrammarDefinition Parse(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                return Parse(reader.ReadToEnd());
            }
        }

        public static GrammarDefinition Parse(string jsonContent)
        {
            JSONNode json = JSON.Parse(jsonContent);
            if (json == null)
                return null;

            var definition = new GrammarDefinition
            {
                Name = json["name"],
                DisplayName = json["displayName"],
                Description = json["description"],
                Version = json["version"],
                Publisher = json["publisher"],
                License = json["license"]
            };

            if (json["engines"] != null && !json["engines"].IsNull)
            {
                definition.Engines = new Engines
                {
                    VsCode = json["engines"]["vscode"]
                };
            }

            if (json["scripts"] != null && !json["scripts"].IsNull)
            {
                definition.Scripts = new Scripts
                {
                    UpdateGrammar = json["scripts"]["update-grammar"]
                };
            }

            if (json["repository"] != null && !json["repository"].IsNull)
            {
                definition.Repository = new Repository
                {
                    Type = json["repository"]["type"],
                    Url = json["repository"]["url"]
                };
            }

            if (json["contributes"] != null && !json["contributes"].IsNull)
            {
                definition.Contributes = ParseContributes(json["contributes"]);
            }

            return definition;
        }

        private static Contributes ParseContributes(JSONNode node)
        {
            var contributes = new Contributes();

            if (node["languages"] != null && node["languages"].IsArray)
            {
                contributes.Languages = new List<Language>();
                foreach (JSONNode langNode in node["languages"].Children)
                {
                    var language = new Language
                    {
                        Id = langNode["id"],
                        ConfigurationFile = langNode["configuration"]
                    };

                    if (langNode["extensions"] != null && langNode["extensions"].IsArray)
                    {
                        language.Extensions = new List<string>();
                        foreach (JSONNode ext in langNode["extensions"].Children)
                        {
                            language.Extensions.Add(ext.Value);
                        }
                    }

                    if (langNode["aliases"] != null && langNode["aliases"].IsArray)
                    {
                        language.Aliases = new List<string>();
                        foreach (JSONNode alias in langNode["aliases"].Children)
                        {
                            language.Aliases.Add(alias.Value);
                        }
                    }

                    if (langNode["mimetypes"] != null && langNode["mimetypes"].IsArray)
                    {
                        language.MimeTypes = new List<string>();
                        foreach (JSONNode mime in langNode["mimetypes"].Children)
                        {
                            language.MimeTypes.Add(mime.Value);
                        }
                    }

                    contributes.Languages.Add(language);
                }
            }

            if (node["grammars"] != null && node["grammars"].IsArray)
            {
                contributes.Grammars = new List<Grammar>();
                foreach (JSONNode grammarNode in node["grammars"].Children)
                {
                    contributes.Grammars.Add(new Grammar
                    {
                        Language = grammarNode["language"],
                        ScopeName = grammarNode["scopeName"],
                        Path = grammarNode["path"]
                    });
                }
            }

            if (node["snippets"] != null && node["snippets"].IsArray)
            {
                contributes.Snippets = new List<Snippet>();
                foreach (JSONNode snippetNode in node["snippets"].Children)
                {
                    contributes.Snippets.Add(new Snippet
                    {
                        Language = snippetNode["language"],
                        Path = snippetNode["path"]
                    });
                }
            }

            return contributes;
        }
    }
}
