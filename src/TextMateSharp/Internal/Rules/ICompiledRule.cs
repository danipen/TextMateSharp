using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Internal.Rules
{
    public class ICompiledRule
    {
        public OnigScanner Scanner { get; private set; }
        public int?[] Rules { get; private set; }

        public ICompiledRule(OnigScanner scanner, int?[] rules)
        {
            Scanner = scanner;
            Rules = rules;
        }
    }
}