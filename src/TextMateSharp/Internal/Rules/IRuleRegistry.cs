using System;

namespace TextMateSharp.Internal.Rules
{
    public interface IRuleRegistry
    {
        Rule GetRule(RuleId patternId);

        Rule RegisterRule(Func<RuleId, Rule> factory);
    }
}