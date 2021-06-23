using System;

namespace TextMateSharp.Internal.Rules
{
    public interface IRuleRegistry
    {
        Rule GetRule(int patternId);

        Rule RegisterRule(Func<int, Rule> factory);
    }
}