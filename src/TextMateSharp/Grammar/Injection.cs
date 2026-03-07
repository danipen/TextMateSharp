using System;
using System.Collections.Generic;

using TextMateSharp.Internal.Rules;
using TextMateSharp.Internal.Types;

namespace TextMateSharp.Grammars
{
    internal sealed class Injection
    {
        internal int Priority { get; private set; } // -1 | 0 | 1; // 0 is the default. -1 for 'L' and 1 for 'R'
        internal RuleId RuleId { get; private set; }
        internal IRawGrammar Grammar { get; private set; }

        private readonly Predicate<List<string>> _matcher;

        internal Injection(Predicate<List<string>> matcher, RuleId ruleId, IRawGrammar grammar, int priority)
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