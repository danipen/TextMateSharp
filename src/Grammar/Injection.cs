using System;
using System.Collections.Generic;

using TextMateSharp.Internal.Types;

namespace TextMateSharp.Grammars
{
    public class Injection
    {

        private Predicate<List<string>> matcher;
        public int priority; // -1 | 0 | 1; // 0 is the default. -1 for 'L' and 1 for 'R'
        public int? ruleId;
        public IRawGrammar grammar;

        public Injection(Predicate<List<string>> matcher, int? ruleId, IRawGrammar grammar, int priority)
        {
            this.matcher = matcher;
            this.ruleId = ruleId;
            this.grammar = grammar;
            this.priority = priority;
        }

        public bool Match(List<string> states)
        {
            return matcher.Invoke(states);
        }
    }
}