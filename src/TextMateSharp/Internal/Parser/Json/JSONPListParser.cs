using System;
using System.IO;
using System.Text.Json;

namespace TextMateSharp.Internal.Parser.Json
{
    public class JSONPListParser<T>
    {
        private readonly bool theme;

        public JSONPListParser(bool theme)
        {
            this.theme = theme;
        }

        public T Parse(StreamReader contents)
        {
            PList<T> pList = new PList<T>(theme);

            var buffer = new byte[2048];
            var valueBuffer = new char[256];

            JsonReaderOptions options = new JsonReaderOptions()
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };

            int size = contents.BaseStream.Read(buffer, 0, buffer.Length);
            var reader = new Utf8JsonReader(buffer.AsSpan(0, size), isFinalBlock: false, state: new JsonReaderState(options));

            while (true)
            {
                int bytesRead = -1;

                while (!reader.Read())
                {
                    bytesRead = GetMoreBytesFromStream(contents.BaseStream, ref buffer, ref reader);

                    if (bytesRead == 0)
                        break;
                }

                if (bytesRead == 0)
                    break;

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
                        AddElement(reader, pList, "key", ref valueBuffer);
                        break;
                    case JsonTokenType.String:
                        AddElement(reader, pList, "string", ref valueBuffer);
                        break;
                }
            }
            return pList.GetResult();
        }

        private static void AddElement(Utf8JsonReader reader, PList<T> pList, string elementName, ref char[] buffer)
        {
            pList.StartElement(elementName);

            long valuelength = reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length;

            if (buffer.Length < valuelength)
                Array.Resize(ref buffer, (int)valuelength);

            int copiedLength = reader.CopyString(buffer);
            pList.AddString(buffer, 0, copiedLength);

            pList.EndElement(elementName);
        }

        private static int GetMoreBytesFromStream(Stream stream, ref byte[] buffer, ref Utf8JsonReader reader)
        {
            int bytesRead;
            if (reader.BytesConsumed < buffer.Length)
            {
                ReadOnlySpan<byte> leftover = buffer.AsSpan((int)reader.BytesConsumed);

                if (leftover.Length == buffer.Length)
                {
                    Array.Resize(ref buffer, buffer.Length * 2);
                }

                leftover.CopyTo(buffer);
                bytesRead = stream.Read(buffer, leftover.Length, buffer.Length - leftover.Length);
                reader = new Utf8JsonReader(buffer.AsSpan(0, bytesRead + leftover.Length), isFinalBlock: bytesRead == 0, reader.CurrentState);
            }
            else
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                reader = new Utf8JsonReader(buffer.AsSpan(0, bytesRead), isFinalBlock: bytesRead == 0, reader.CurrentState);
            }

            return bytesRead;
        }
    }
}