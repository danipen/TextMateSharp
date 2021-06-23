using System.Collections.Generic;

using TextMateSharp.Internal.Parser;

namespace TextMateSharp.Internal.Themes
{
    public class PListTheme : PListObject
    {
        public PListTheme(PListObject parent, bool valueAsArray) : base(parent, valueAsArray)
        {
        }

        protected override Dictionary<string, object> CreateRaw()
        {
            return new ThemeRaw();
        }
    }
}
