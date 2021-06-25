using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Internal.Rules
{
    public class ICompiledRule
    {
        public OnigScanner scanner;
        public int?[] rules;

        public ICompiledRule(OnigScanner scanner, int?[] rules)
        {
            this.scanner = scanner;
            this.rules = rules;
        }
    }
}