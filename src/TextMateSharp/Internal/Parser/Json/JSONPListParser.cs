using System;
using System.IO;
using System.Text.Json;

namespace TextMateSharp.Internal.Parser.Json
{
    public class JSONPListParser<T>
    {

        private bool theme;

        public JSONPListParser(bool theme)
        {
            this.theme = theme;
        }

        public T Parse(StreamReader contents)
        {
            PList<T> pList = new PList<T>(theme);

            var buffer = new byte[contents.BaseStream.Length];

            JsonReaderOptions options = new JsonReaderOptions()
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };

            contents.BaseStream.Read(buffer, 0, buffer.Length);

            var reader = new Utf8JsonReader(buffer, true, new JsonReaderState(options));

            while (reader.Read())
            {
                var nextToken = reader.TokenType;
                switch (nextToken)
                {
                    case JsonTokenType.StartArray:
                        pList.StartElement("array");
                        break;
                    case JsonTokenType.EndArray:
                        pList.EndElement("array");
                        break;
                    case JsonTokenType.StartObject:
                        pList.StartElement("dict");
                        break;
                    case JsonTokenType.EndObject:
                        pList.EndElement("dict");
                        break;
                    case JsonTokenType.PropertyName:
                        pList.StartElement("key");
                        pList.AddString(reader.GetString());
                        pList.EndElement("key");
                        break;
                    case JsonTokenType.String:
                        pList.StartElement("string");
                        pList.AddString(reader.GetString());
                        pList.EndElement("string");
                        break;
                }
            }
            return pList.GetResult();
        }
    }
}