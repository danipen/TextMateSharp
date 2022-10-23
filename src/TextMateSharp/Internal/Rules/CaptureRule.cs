using System;

namespace TextMateSharp.Internal.Rules
{
    public class CaptureRule : Rule
    {
        public RuleId RetokenizeCapturedWithRuleId { get; private set; }

        public CaptureRule(RuleId id, string name, string contentName, RuleId retokenizeCapturedWithRuleId) : base(id, name, contentName)
        {
            RetokenizeCapturedWithRuleId = retokenizeCapturedWithRuleId;
        }

        public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst)
        {
            
        }

        public override CompiledRule Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG)
        {
            return null;
        }

    }
}