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

        public void StartElement(string tagName)
        {
            if ("dict".Equals(tagName))
            {
                this.currObject = Create(currObject, false);
            }
            else if ("array".Equals(tagName))
            {
                this.currObject = Create(currObject, true);
            }
            else if ("key".Equals(tagName))
            {
                if (currObject != null)
                {
                    currObject.SetLastKey(null);
                }
            }
            this.text ??= new StringBuilder("");
            this.text.Clear();
        }

        private PListObject Create(PListObject parent, bool valueAsArray)
        {
            if (theme)
            {
                return new PListTheme(parent, valueAsArray);
            }
            return new PListGrammar(parent, valueAsArray);
        }

        public void EndElement(string tagName)
        {
            object value = null;
            string t = this.text.ToString();
            if ("key".Equals(tagName))
            {
                if (currObject == null || currObject.IsValueAsArray())
                {
                    errors.Add("key can only be used inside an open dict element");
                    return;
                }
                currObject.SetLastKey(t);
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
                value = t;
            }
            else if ("date".Equals(tagName))
            {
                // TODO : parse date
            }
            else if ("integer".Equals(tagName))
            {
                if (!int.TryParse(t, out int i))
                {
                    errors.Add(t + " is not an integer");
                    return;
                }
                value = i;
            }
            else if ("real".Equals(tagName))
            {
                if (!float.TryParse(t, out float f))
                {
                    errors.Add(t + " is not a float");
                    return;
                }
                value = f;
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
