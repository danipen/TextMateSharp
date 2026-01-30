using System.IO;

using SimpleJSON;

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

            string jsonContent = contents.ReadToEnd();
            JSONNode root = JSON.Parse(jsonContent);

            ProcessNode(root, pList);

            return pList.GetResult();
        }

        private void ProcessNode(JSONNode node, PList<T> pList)
        {
            if (node == null)
                return;

            if (node.IsArray)
            {
                pList.StartElement("array");
                foreach (JSONNode child in node.Children)
                {
                    ProcessNode(child, pList);
                }
                pList.EndElement("array");
            }
            else if (node.IsObject)
            {
                pList.StartElement("dict");
                foreach (var kvp in node.Linq)
                {
                    pList.StartElement("key");
                    pList.AddString(kvp.Key);
                    pList.EndElement("key");

                    ProcessNode(kvp.Value, pList);
                }
                pList.EndElement("dict");
            }
            else if (node.IsString)
            {
                pList.StartElement("string");
                pList.AddString(node.Value);
                pList.EndElement("string");
            }
            else if (node.IsNumber)
            {
                pList.StartElement("string");
                pList.AddString(node.Value);
                pList.EndElement("string");
            }
            else if (node.IsBoolean)
            {
                pList.StartElement("string");
                pList.AddString(node.Value);
                pList.EndElement("string");
            }
        }
    }
}