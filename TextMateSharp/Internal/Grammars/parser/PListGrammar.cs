using System.Collections.Generic;

using TextMateSharp.Internal.Parser;

namespace TextMateSharp.Internal.Grammars.Parser
{
    public class PListGrammar : PListObject
    {
        public PListGrammar(PListObject parent, bool valueAsArray) : base(parent, valueAsArray)
        {
        }

        protected override Dictionary<string, object> CreateRaw()
        {
            return new Raw();
        }
    }
}