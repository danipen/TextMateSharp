using System.IO;

using TextMateSharp.Internal.Parser.Json;
using TextMateSharp.Internal.Types;

namespace TextMateSharp.Internal.Grammars.Reader
{
    public class GrammarReader
    {
        public static IRawGrammar ReadGrammarSync(StreamReader reader)
        {
            JSONPListParser<IRawGrammar> parser = new JSONPListParser<IRawGrammar>(false);
            return parser.Parse(reader);
        }
    }
}