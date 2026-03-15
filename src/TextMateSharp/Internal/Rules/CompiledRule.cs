using Onigwrap;
using System.Collections.Generic;

namespace TextMateSharp.Internal.Rules
{
    public sealed class CompiledRule
    {
        public OnigScanner Scanner { get; private set; }
        public IList<RuleId> Rules { get; private set; }

        internal CompiledRule(OnigScanner scanner, IList<RuleId> rules)
        {
            Scanner = scanner;
            Rules = rules;
        }
    }
}