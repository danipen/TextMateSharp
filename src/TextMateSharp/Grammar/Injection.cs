using System;
using System.Collections.Generic;

using TextMateSharp.Internal.Types;

namespace TextMateSharp.Grammars
{
    public class Injection
    {
        public int Priority { get; private set; } // -1 | 0 | 1; // 0 is the default. -1 for 'L' and 1 for 'R'
        public int? RuleId { get; private set; }
        public IRawGrammar Grammar { get; private set; }

        private Predicate<List<string>> _matcher;

        public Injection(Predicate<List<string>> matcher, int? ruleId, IRawGrammar grammar, int priority)
        {
            RuleId = ruleId;
            Grammar = grammar;
            Priority = priority;

            this._matcher = matcher;
        }

        public bool Match(List<string> states)
        {
            return _matcher.Invoke(states);
        }
    }
}