using System.Collections.Generic;
using Onigwrap;

namespace TextMateSharp.Internal.Rules
{
    public class CompiledRule
    {
        public OnigScanner Scanner { get; private set; }
        public IList<RuleId> Rules { get; private set; }

        public CompiledRule(OnigScanner scanner, IList<RuleId> rules)
        {
            Scanner = scanner;
            Rules = rules;
        }
    }
}