using System.Collections.Generic;

namespace TextMateSharp.Internal.Parser
{
    public abstract class PListObject
    {

        public PListObject parent;
        private List<object> arrayValues;
        private Dictionary<string, object> mapValues;

        private string lastKey;

        public PListObject(PListObject parent, bool valueAsArray)
        {
            this.parent = parent;
            if (valueAsArray)
            {
                this.arrayValues = new List<object>();
                this.mapValues = null;
            }
            else
            {
                this.arrayValues = null;
                this.mapValues = CreateRaw();
            }
        }

        public string GetLastKey()
        {
            return lastKey;
        }

        public void SetLastKey(string lastKey)
        {
            this.lastKey = lastKey;
        }

        public void AddValue(object value)
        {
            if (IsValueAsArray())
            {
                arrayValues.Add(value);
            }
            else
            {
                mapValues[GetLastKey()] = value;
            }
        }

        public bool IsValueAsArray()
        {
            return arrayValues != null;
        }

        public object GetValue()
        {
            if (IsValueAsArray())
            {
                return arrayValues;
            }
            return mapValues;
        }

        protected abstract Dictionary<string, object> CreateRaw();
    }
}