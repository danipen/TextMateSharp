using System.Collections;
using System.Collections.Generic;

using TextMateSharp.Internal.Grammars.Parser;
using TextMateSharp.Internal.Types;

namespace TextMateSharp.Internal.Utils
{
    public static class CloneUtils
    {
        public static object Clone(object value)
        {
            if (value is Raw)
            {
                Raw rawToClone = (Raw)value;
                Raw raw = new Raw();

                foreach (string key in rawToClone.Keys)
                {
                    raw[key] = Clone(raw[key]);
                }
                return raw;
            }
            else if (value is IList)
            {
                List<object> result = new List<object>();
                foreach (object obj in (IList)value)
                {
                    result.Add(Clone(obj));
                }
                return result;
            }
            else if (value is string)
            {
                return value;
            }
            else if (value is int)
            {
                return value;
            }
            else if (value is bool)
            {
                return value;
            }
            return value;
        }

        public static IRawRepository MergeObjects(params IRawRepository[] sources)
        {
            Raw target = new Raw();
            foreach (IRawRepository source in sources)
            {
                Raw sourceRaw = ((Raw)source);
                foreach (string key in sourceRaw.Keys)
                {
                    target[key] = sourceRaw[key];
                }
            }
            return target;
        }
    }
}