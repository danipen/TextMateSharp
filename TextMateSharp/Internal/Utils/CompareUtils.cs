using System.Collections.Generic;

namespace TextMateSharp.Internal.Utils
{
    public class CompareUtils
    {
        public static int Strcmp(string a, string b)
        {
            if (a == null && b == null)
            {
                return 0;
            }
            if (a == null)
            {
                return -1;
            }
            if (b == null)
            {
                return 1;
            }
            //		if (a < b) {
            //			return -1;
            //		}
            //		if (a > b) {
            //			return 1;
            //		}
            //		return 0;
            int result = a.CompareTo(b);
            if (result < 0)
            {
                return -1;
            }
            else if (result > 0)
            {
                return 1;
            }
            return 0;
        }

        public static int StrArrCmp(List<string> a, List<string> b)
        {
            if (a == null && b == null)
            {
                return 0;
            }
            if (a == null)
            {
                return -1;
            }
            if (b == null)
            {
                return 1;
            }
            int len1 = a.Count;
            int len2 = b.Count;
            if (len1 == len2)
            {
                for (int i = 0; i < len1; i++)
                {
                    int res = Strcmp(a[i], b[i]);
                    if (res != 0)
                    {
                        return res;
                    }
                }
                return 0;
            }
            return len1 - len2;
        }
    }
}