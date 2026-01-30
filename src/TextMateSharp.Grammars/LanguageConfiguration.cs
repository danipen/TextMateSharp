using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SimpleJSON;

using TextMateSharp.Grammars.Resources;

using CharacterPair = System.Collections.Generic.IList<char>;
using StringPair = System.Collections.Generic.IList<string>;

namespace TextMateSharp.Grammars
{
    public class LanguageConfiguration
    {
        public string AutoCloseBefore { get; set; }

        public Folding Folding { get; set; }

        public StringPair[] Brackets { get; set; }

        public Comments Comments { get; set; }

        public AutoClosingPairs AutoClosingPairs { get; set; }

        public CharacterPair[] SurroundingPairs { get; set; }

        public Indentation IndentationRules { get; set; }

        public EnterRules EnterRules { get; set; }

        public static LanguageConfiguration Load(string grammarName, string configurationFile)
        {
            if (string.IsNullOrEmpty(configurationFile))
                return null;

            using (Stream stream = ResourceLoader.TryOpenLanguageConfiguration(grammarName, configurationFile))
            {
                if (stream == null)
                    return null;

                using (StreamReader reader = new StreamReader(stream))
                {
                    return Parse(reader.ReadToEnd());
                }
            }
        }

        public static LanguageConfiguration LoadFromLocal(string configurationFile)
        {
            var fileInfo = new FileInfo(configurationFile);
            if (!fileInfo.Exists)
            {
                return null;
            }
            using (var fileStream = fileInfo.OpenRead())
            using (var reader = new StreamReader(fileStream))
            {
                return Parse(reader.ReadToEnd());
            }
        }

        public static LanguageConfiguration Parse(string jsonContent)
        {
            JSONNode json = JSON.Parse(jsonContent);
            if (json == null)
                return null;

            var config = new LanguageConfiguration
            {
                AutoCloseBefore = json["autoCloseBefore"]
            };

            // Parse folding
            if (json["folding"] != null && !json["folding"].IsNull)
            {
                config.Folding = ParseFolding(json["folding"]);
            }

            // Parse brackets
            if (json["brackets"] != null && json["brackets"].IsArray)
            {
                config.Brackets = ParseBrackets(json["brackets"]);
            }

            // Parse comments
            if (json["comments"] != null && !json["comments"].IsNull)
            {
                config.Comments = ParseComments(json["comments"]);
            }

            // Parse autoClosingPairs
            if (json["autoClosingPairs"] != null && json["autoClosingPairs"].IsArray)
            {
                config.AutoClosingPairs = ParseAutoClosingPairs(json["autoClosingPairs"]);
            }

            // Parse surroundingPairs
            if (json["surroundingPairs"] != null && json["surroundingPairs"].IsArray)
            {
                config.SurroundingPairs = ParseSurroundingPairs(json["surroundingPairs"]);
            }

            // Parse indentationRules
            if (json["indentationRules"] != null && !json["indentationRules"].IsNull)
            {
                config.IndentationRules = ParseIndentation(json["indentationRules"]);
            }

            // Parse onEnterRules
            if (json["onEnterRules"] != null && json["onEnterRules"].IsArray)
            {
                config.EnterRules = ParseEnterRules(json["onEnterRules"]);
            }

            return config;
        }

        private static Folding ParseFolding(JSONNode node)
        {
            var folding = new Folding
            {
                OffSide = node["offSide"].AsBool
            };

            if (node["markers"] != null && !node["markers"].IsNull)
            {
                folding.Markers = new Markers
                {
                    Start = node["markers"]["start"],
                    End = node["markers"]["end"]
                };
            }

            return folding;
        }

        private static StringPair[] ParseBrackets(JSONNode node)
        {
            var brackets = new List<StringPair>();
            foreach (JSONNode bracketPair in node.Children)
            {
                if (bracketPair.IsArray && bracketPair.Count >= 2)
                {
                    brackets.Add(new List<string> { bracketPair[0].Value, bracketPair[1].Value });
                }
            }
            return brackets.ToArray();
        }

        private static Comments ParseComments(JSONNode node)
        {
            var comments = new Comments
            {
                LineComment = node["lineComment"]
            };

            if (node["blockComment"] != null && node["blockComment"].IsArray && node["blockComment"].Count >= 2)
            {
                comments.BlockComment = new List<string>
                {
                    node["blockComment"][0].Value,
                    node["blockComment"][1].Value
                };
            }

            return comments;
        }

        private static AutoClosingPairs ParseAutoClosingPairs(JSONNode node)
        {
            var autoClosingPairs = new AutoClosingPairs();
            var charPairs = new List<CharacterPair>();
            var autoPairs = new List<AutoPair>();

            foreach (JSONNode pairNode in node.Children)
            {
                if (pairNode.IsArray)
                {
                    // It's a simple character pair like ["(", ")"]
                    var pair = new List<char>();
                    foreach (JSONNode charNode in pairNode.Children)
                    {
                        string value = charNode.Value;
                        if (!string.IsNullOrEmpty(value))
                        {
                            pair.Add(value[0]);
                        }
                    }
                    if (pair.Count > 0)
                    {
                        charPairs.Add(pair);
                    }
                }
                else if (pairNode.IsObject)
                {
                    // It's an object like {"open": "(", "close": ")", "notIn": ["string"]}
                    var autoPair = new AutoPair
                    {
                        Open = pairNode["open"],
                        Close = pairNode["close"]
                    };

                    if (pairNode["notIn"] != null && pairNode["notIn"].IsArray)
                    {
                        var notIn = new List<string>();
                        foreach (JSONNode notInNode in pairNode["notIn"].Children)
                        {
                            notIn.Add(notInNode.Value);
                        }
                        autoPair.NotIn = notIn;
                    }

                    autoPairs.Add(autoPair);
                }
            }

            autoClosingPairs.CharPairs = charPairs.ToArray();
            autoClosingPairs.AutoPairs = autoPairs.ToArray();

            return autoClosingPairs;
        }

        private static CharacterPair[] ParseSurroundingPairs(JSONNode node)
        {
            var surroundingPairs = new List<CharacterPair>();

            foreach (JSONNode pairNode in node.Children)
            {
                if (pairNode.IsArray)
                {
                    var pair = new List<char>();
                    foreach (JSONNode charNode in pairNode.Children)
                    {
                        string value = charNode.Value;
                        if (!string.IsNullOrEmpty(value))
                        {
                            pair.Add(value[0]);
                        }
                    }
                    if (pair.Count > 0)
                    {
                        surroundingPairs.Add(pair.ToArray());
                    }
                }
                else if (pairNode.IsObject)
                {
                    string open = pairNode["open"];
                    string close = pairNode["close"];
                    if (!string.IsNullOrEmpty(open) && !string.IsNullOrEmpty(close))
                    {
                        surroundingPairs.Add(new char[] { open[0], close[0] });
                    }
                }
            }

            return surroundingPairs.ToArray();
        }

        private static Indentation ParseIndentation(JSONNode node)
        {
            var indentation = new Indentation();

            // Handle both string format and object format with pattern property
            indentation.Increase = GetPatternValue(node["increaseIndentPattern"]);
            indentation.Decrease = GetPatternValue(node["decreaseIndentPattern"]);
            indentation.Unindent = GetPatternValue(node["unIndentedLinePattern"]);

            return indentation;
        }

        private static string GetPatternValue(JSONNode node)
        {
            if (node == null || node.IsNull)
                return string.Empty;

            if (node.IsString)
                return node.Value;

            if (node.IsObject && node["pattern"] != null)
                return node["pattern"].Value;

            return string.Empty;
        }

        private static EnterRules ParseEnterRules(JSONNode node)
        {
            var enterRules = new EnterRules();

            foreach (JSONNode ruleNode in node.Children)
            {
                if (ruleNode.IsObject)
                {
                    enterRules.Rules.Add(ParseEnterRule(ruleNode));
                }
            }

            return enterRules;
        }

        private static EnterRule ParseEnterRule(JSONNode node)
        {
            var rule = new EnterRule();

            // beforeText can be a string or an object with pattern
            rule.BeforeText = GetPatternValue(node["beforeText"]);
            rule.AfterText = GetPatternValue(node["afterText"]);

            // action is an object with indent/appendText
            if (node["action"] != null && !node["action"].IsNull)
            {
                rule.ActionIndent = node["action"]["indent"];
                rule.AppendText = node["action"]["appendText"];
            }

            return rule;
        }
    }


    public class Region
    {
        public string Prefix { get; set; }

        public string[] Body { get; set; }

        public string Description { get; set; }
    }

    public class Markers
    {
        public string Start { get; set; }

        public string End { get; set; }
    }

    public class Folding
    {
        public bool OffSide { get; set; }

        public Markers Markers { get; set; }

        public bool IsEmpty
        {
            get
            {
                return Markers == null || string.IsNullOrEmpty(Markers.Start) || string.IsNullOrEmpty(Markers.End);
            }
        }
    }

    public class Comments
    {
        public string LineComment { get; set; }

        public StringPair BlockComment { get; set; }
    }

    public class Indentation
    {
        public string Increase { get; set; } = string.Empty;

        public string Decrease { get; set; } = string.Empty;

        public string Unindent { get; set; } = string.Empty;

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(Increase) || string.IsNullOrEmpty(Decrease);
            }
        }
    }

    public class EnterRule
    {
        public string BeforeText { get; set; }

        public string AfterText { get; set; }

        public string ActionIndent { get; set; }

        public string AppendText { get; set; }
    }

    public class EnterRules
    {
        public IList<EnterRule> Rules { get; set; } = new List<EnterRule>();
    }

    public class AutoPair
    {
        public string Open { get; set; }

        public string Close { get; set; }

        public StringPair NotIn { get; set; }
    }

    public class AutoClosingPairs
    {
        public CharacterPair[] CharPairs { get; set; } = new CharacterPair[] { };

        public AutoPair[] AutoPairs { get; set; } = new AutoPair[] { };
    }

    public class LanguageSnippet
    {
        public string Prefix { get; set; }

        public string[] Body { get; set; }

        public string Description { get; set; }
    }

    public class LanguageSnippets
    {
        public IDictionary<string, LanguageSnippet> Snippets { get; set; } = new Dictionary<string, LanguageSnippet>();

        public static LanguageSnippets Load(string grammarName, Contributes contributes)
        {
            if (contributes == null || contributes.Snippets == null)
                return null;

            foreach (var snippet in contributes.Snippets)
            {
                using (Stream stream = ResourceLoader.TryOpenLanguageSnippet(grammarName, snippet.Path))
                {
                    if (stream == null)
                        continue;

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return Parse(reader.ReadToEnd());
                    }
                }
            }

            return new LanguageSnippets();
        }

        public static LanguageSnippets LoadFromLocal(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                return null;
            }
            using (var fileStream = fileInfo.OpenRead())
            using (var reader = new StreamReader(fileStream))
            {
                return Parse(reader.ReadToEnd());
            }
        }

        public static LanguageSnippets Parse(string jsonContent)
        {
            JSONNode json = JSON.Parse(jsonContent);
            if (json == null)
                return null;

            var snippets = new LanguageSnippets();

            foreach (var kvp in json.Linq)
            {
                var snippet = new LanguageSnippet
                {
                    Prefix = kvp.Value["prefix"],
                    Description = kvp.Value["description"]
                };

                if (kvp.Value["body"] != null)
                {
                    if (kvp.Value["body"].IsArray)
                    {
                        var bodyList = new List<string>();
                        foreach (JSONNode bodyNode in kvp.Value["body"].Children)
                        {
                            bodyList.Add(bodyNode.Value);
                        }
                        snippet.Body = bodyList.ToArray();
                    }
                    else if (kvp.Value["body"].IsString)
                    {
                        snippet.Body = new string[] { kvp.Value["body"].Value };
                    }
                }

                snippets.Snippets.Add(kvp.Key, snippet);
            }

            return snippets;
        }
    }
}
