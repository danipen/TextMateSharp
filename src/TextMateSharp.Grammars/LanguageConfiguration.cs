using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using TextMateSharp.Grammars.Resources;
using TextMateSharp.Internal.Grammars;

using CharacterPair = System.Collections.Generic.IList<char>;
using StringPair = System.Collections.Generic.IList<string>;

namespace TextMateSharp.Grammars
{
    public class LanguageConfiguration
    {
        [JsonPropertyName("autoCloseBefore")]
        public string AutoCloseBefore { get; set; }

        [JsonPropertyName("folding")]
        public Folding Folding { get; set; }

        [JsonPropertyName("brackets")]
        public StringPair[] Brackets { get; set; }

        [JsonPropertyName("comments")]
        public Comments Comments { get; set; }

        [JsonPropertyName("autoClosingPairs")]
        [JsonConverter(typeof(ClosingPairJsonConverter))]
        public AutoClosingPairs AutoClosingPairs { get; set; }

        [JsonPropertyName("indentationRules")]
        [JsonConverter(typeof(IntentationRulesJsonConverter))]
        public Indentation IndentationRules { get; set; }

        [JsonPropertyName("onEnterRules")]
        [JsonConverter(typeof(EnterRulesJsonConverter))]
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
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
                    return JsonSerializer.Deserialize<LanguageConfiguration>(stream, new JsonSerializerOptions { AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip });
#pragma warning restore IL2026
                }
            }
        }
    }

    public class Region
    {
        [JsonPropertyName("prefix")]
        public string Prefix { get; set; }

        [JsonPropertyName("body")]
        public string[] Body { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class Markers
    {
        [JsonPropertyName("start")]
        public string Start { get; set; }

        [JsonPropertyName("end")]
        public string End { get; set; }
    }

    public class Folding
    {
        [JsonPropertyName("offSide")]
        public bool OffSide { get; set; }

        [JsonPropertyName("markers")]
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
        [JsonPropertyName("lineComment")]
        public string LineComment { get; set; }

        [JsonPropertyName("blockComment")]
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

    [JsonConverter(typeof(EnterRuleJsonConverter))]
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

    [JsonConverter(typeof(LanguageSnippetJsonConverter))]
    public class LanguageSnippet
    {
        public string Prefix { get; set; }

        public string[] Body { get; set; }

        public string Description { get; set; }
    }

    [JsonConverter(typeof(LanguageSnippetsJsonConverter))]
    public class LanguageSnippets
    {
        public IDictionary<string, LanguageSnippet> Snippets { get; set; } = new Dictionary<string, LanguageSnippet>();

        public static LanguageSnippets Load(string grammarName, Contributes contributes)
        {
            if (contributes == null || contributes.Snippets == null)
                return null;

            var result = new LanguageSnippets();

            foreach (var snippet in contributes.Snippets)
            {
                using (Stream stream = ResourceLoader.TryOpenLanguageSnippet(grammarName, snippet.Path))
                {
                    if (stream == null)
                        continue;

                    using (StreamReader reader = new StreamReader(stream))
                    {
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
                        return JsonSerializer.Deserialize<LanguageSnippets>(stream, new JsonSerializerOptions { AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip });
#pragma warning restore IL2026
                    }
                }
            }

            return result;
        }
    }

    public class ClosingPairJsonConverter : JsonConverter<AutoClosingPairs>
    {
        public override AutoClosingPairs Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var autoClosingPairs = new AutoClosingPairs();

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var charPairs = new List<CharacterPair>();
                var autoPairs = new List<AutoPair>();

                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.StartArray:
                            var charPair = new List<char>();
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                switch (reader.TokenType)
                                {
                                    case JsonTokenType.String:
                                        charPair.Add(reader.GetString().ToCharArray().First());
                                        break;
                                }
                            }
                            //var charPair = JsonSerializer.Deserialize<CharPair>(ref reader, CharacterPairSerializationContext.Default.CharPair);
                            charPairs.Add(charPair);
                            break;

                        case JsonTokenType.StartObject:
                            var autoPair = new AutoPair();
                            string propName = string.Empty;
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                            {
                                switch(reader.TokenType)
                                {
                                    case JsonTokenType.StartArray:
                                        if (string.Compare(propName, "notIn") == 0)
                                        {
                                            autoPair.NotIn = JsonSerializer.Deserialize<StringPair>(ref reader, StringPairSerializationContext.Default.IListString);
                                        }
                                        break;
                                    case JsonTokenType.PropertyName:
                                        propName = reader.GetString();
                                        break;
                                    case JsonTokenType.String:
                                        switch (propName)
                                        {
                                            case "open":
                                                autoPair.Open = reader.GetString();
                                                break;
                                            case "close":
                                                autoPair.Close = reader.GetString();
                                                break;
                                        }
                                        break;
                                }
                            }
                                autoPairs.Add(autoPair);
                            break;
                    }
                }

                autoClosingPairs.CharPairs = charPairs.ToArray();
                autoClosingPairs.AutoPairs = autoPairs.ToArray();
            }

            return autoClosingPairs;
        }

        public override void Write(Utf8JsonWriter writer, AutoClosingPairs value, JsonSerializerOptions options)
        {
        }
    }

    public class IntentationRulesJsonConverter : JsonConverter<Indentation>
    {
        public override Indentation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var rules = new Indentation();

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                string propName = string.Empty;
                string internalName = string.Empty;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            propName = reader.GetString();
                            break;
                        case JsonTokenType.String:
                            switch (propName)
                            {
                                case "increaseIndentPattern":
                                    rules.Increase = reader.GetString();
                                    break;
                                case "decreaseIndentPattern":
                                    rules.Decrease = reader.GetString();
                                    break;
                                case "unIndentedLinePattern":
                                    rules.Unindent = reader.GetString();
                                    break;
                            }

                            break;

                        case JsonTokenType.StartObject:
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                            {
                                switch (reader.TokenType)
                                {
                                    case JsonTokenType.PropertyName:
                                        internalName = reader.GetString();
                                        break;
                                    case JsonTokenType.String:
                                        switch (internalName)
                                        {
                                            case "pattern":
                                                switch (propName)
                                                {
                                                    case "increaseIndentPattern":
                                                        rules.Increase = reader.GetString();
                                                        break;
                                                    case "decreaseIndentPattern":
                                                        rules.Decrease = reader.GetString();
                                                        break;
                                                    case "unIndentedLinePattern":
                                                        rules.Unindent = reader.GetString();
                                                        break;
                                                }

                                                break;
                                        }

                                        break;
                                }
                            }

                            break;
                    }
                }
            }

            return rules;
        }

        public override void Write(Utf8JsonWriter writer, Indentation value, JsonSerializerOptions options)
        {
        }
    }

    public class EnterRulesJsonConverter : JsonConverter<EnterRules>
    {
        public override EnterRules Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var enterRules = new EnterRules();

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.StartArray:
                            break;

                        case JsonTokenType.StartObject:
                            EnterRule rule = JsonSerializer.Deserialize<EnterRule>(ref reader, EnterRuleSerializationContext.Default.EnterRule);
                            enterRules.Rules.Add(rule);
                            break;
                    }
                }
            }

            return enterRules;
        }

        public override void Write(Utf8JsonWriter writer, EnterRules value, JsonSerializerOptions options)
        {
        }
    }

    public class EnterRuleJsonConverter : JsonConverter<EnterRule>
    {
        public override EnterRule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var enterRule = new EnterRule();
            string propName = string.Empty;
            string internalName = string.Empty;
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            propName = reader.GetString();
                            break;
                        case JsonTokenType.StartObject:
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                            {
                                switch (reader.TokenType)
                                {
                                    case JsonTokenType.PropertyName:
                                        internalName = reader.GetString();
                                        break;
                                    case JsonTokenType.String:
                                        switch (internalName)
                                        {
                                            case "pattern":
                                                switch (propName)
                                                {
                                                    case "beforeText":
                                                        enterRule.BeforeText = reader.GetString();
                                                        break;
                                                    case "afterText":
                                                        enterRule.AfterText = reader.GetString();
                                                        break;
                                                }

                                                break;
                                            case "indent":
                                                enterRule.ActionIndent = reader.GetString();
                                                break;
                                            case "appendText":
                                                enterRule.AppendText = reader.GetString();
                                                break;
                                        }

                                        break;
                                }
                            }

                            break;
                        case JsonTokenType.String:
                            switch (propName)
                            {
                                case "beforeText":
                                    enterRule.BeforeText = reader.GetString();
                                    break;
                                case "afterText":
                                    enterRule.AfterText = reader.GetString();
                                    break;
                            }

                            break;
                    }
                }
            }

            return enterRule;
        }

        public override void Write(Utf8JsonWriter writer, EnterRule value, JsonSerializerOptions options)
        {
        }
    }

    public class LanguageSnippetsJsonConverter : JsonConverter<LanguageSnippets>
    {
        public override LanguageSnippets Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var snippets = new LanguageSnippets();

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                string propName = string.Empty;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            propName = reader.GetString();
                            break;
                        case JsonTokenType.StartObject:
                            LanguageSnippet snippet = JsonSerializer.Deserialize<LanguageSnippet>(ref reader, LanguageSnippetSerializationContext.Default.LanguageSnippet);
                            snippets.Snippets.Add(propName, snippet);
                            break;
                    }
                }
            }

            return snippets;
        }

        public override void Write(Utf8JsonWriter writer, LanguageSnippets value, JsonSerializerOptions options)
        {
        }
    }

    public class LanguageSnippetJsonConverter : JsonConverter<LanguageSnippet>
    {
        public override LanguageSnippet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var snippet = new LanguageSnippet();

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                string propName = string.Empty;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            propName = reader.GetString();
                            break;
                        case JsonTokenType.StartArray:
                            IList<string> body = new List<string>();
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                if (string.Compare(propName, "body") == 0)
                                {
                                    switch (reader.TokenType)
                                    {
                                        case JsonTokenType.String:
                                            body.Add(reader.GetString());
                                            break;
                                    }
                                }
                            }

                            snippet.Body = body.ToArray();

                            break;
                        case JsonTokenType.String:
                            switch (propName)
                            {
                                case "prefix":
                                    snippet.Prefix = reader.GetString();
                                    break;
                                case "description":
                                    snippet.Description = reader.GetString();
                                    break;
                            }

                            break;
                    }
                }
            }

            return snippet;
        }

        public override void Write(Utf8JsonWriter writer, LanguageSnippet value, JsonSerializerOptions options)
        {
        }
    }

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(StringPair))]
    internal sealed partial class StringPairSerializationContext : JsonSerializerContext
    {
    }

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(AutoPair))]
    internal sealed partial class AutoPairSerializationContext : JsonSerializerContext
    {
    }

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(EnterRule))]
    internal sealed partial class EnterRuleSerializationContext : JsonSerializerContext
    {
    }

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(LanguageConfiguration))]
    internal sealed partial class LanguageConfigurationSerializationContext : JsonSerializerContext
    {
    }

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(LanguageSnippet))]
    internal sealed partial class LanguageSnippetSerializationContext : JsonSerializerContext
    {
    }

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(LanguageSnippets))]
    internal sealed partial class LanguageSnippetsSerializationContext : JsonSerializerContext
    {
    }
}