using System;
using System.Collections.Generic;
using System.Text;

using TextMateSharp.Internal.Grammars.Parser;
using TextMateSharp.Internal.Themes;

namespace TextMateSharp.Internal.Parser
{
    public class PList<T>
    {
        private bool theme;
        private List<string> errors;
        private PListObject currObject;
        private T result;
        private StringBuilder text;

        public PList(bool theme)
        {
            this.theme = theme;
            this.errors = new List<string>();
            this.currObject = null;
        }

        public void StartElement(string localName)
        {
            if ("dict".Equals(localName))
            {
                this.currObject = Create(currObject, false);
            }
            else if ("array".Equals(localName))
            {
                this.currObject = Create(currObject, true);
            }
            else if ("key".Equals(localName))
            {
                if (currObject != null)
                {
                    currObject.SetLastKey(null);
                }
            }
            this.text = new StringBuilder("");
        }

        private PListObject Create(PListObject parent, bool valueAsArray)
        {
            if (theme)
            {
                return new PListTheme(parent, valueAsArray);
            }
            return new PListGrammar(parent, valueAsArray);
        }

        public void EndElement(string localName)
        {
            EndElement(localName);
        }

        private void endElement(string tagName)
        {
            object value = null;
            string text = this.text.ToString();
            if ("key".Equals(tagName))
            {
                if (currObject == null || currObject.IsValueAsArray())
                {
                    errors.Add("key can only be used inside an open dict element");
                    return;
                }
                currObject.SetLastKey(text);
                return;
            }
            else if ("dict".Equals(tagName) || "array".Equals(tagName))
            {
                if (currObject == null)
                {
                    errors.Add(tagName + " closing tag found, without opening tag");
                    return;
                }
                value = currObject.GetValue();
                currObject = currObject.parent;
            }
            else if ("string".Equals(tagName) || "data".Equals(tagName))
            {
                value = text;
            }
            else if ("date".Equals(tagName))
            {
                // TODO : parse date
            }
            else if ("integer".Equals(tagName))
            {
                try
                {
                    value = int.Parse(text);
                }
                catch (Exception)
                {
                    errors.Add(text + " is not a integer");
                    return;
                }
            }
            else if ("real".Equals(tagName))
            {
                try
                {
                    value = float.Parse(text);
                }
                catch (Exception)
                {
                    errors.Add(text + " is not a float");
                    return;
                }
            }
            else if ("true".Equals(tagName))
            {
                value = true;
            }
            else if ("false".Equals(tagName))
            {
                value = false;
            }
            else if ("plist".Equals(tagName))
            {
                return;
            }
            else
            {
                errors.Add("Invalid tag name: " + tagName);
                return;
            }
            if (currObject == null)
            {
                result = (T)value;
            }
            else if (currObject.IsValueAsArray())
            {
                currObject.AddValue(value);
            }
            else
            {
                if (currObject.GetLastKey() != null)
                {
                    currObject.AddValue(value);
                }
                else
                {
                    errors.Add("Dictionary key missing for value " + value);
                }
            }
        }

        public void AddString(string str)
        {
            this.text.Append(str);
        }

        public T GetResult()
        {
            return result;
        }
    }
}
