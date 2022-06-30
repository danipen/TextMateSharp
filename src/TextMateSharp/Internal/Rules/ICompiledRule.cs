using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Internal.Rules
{
    public class CompiledRule
    {
        public OnigScanner Scanner { get; private set; }
        public int?[] Rules { get; private set; }

        public CompiledRule(OnigScanner scanner, int?[] rules)
        {
            Scanner = scanner;
            Rules = rules;
        }
    }
}