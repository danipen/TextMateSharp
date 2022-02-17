using System;

namespace TextMateSharp.Internal.Rules
{
    public class CaptureRule : Rule
    {
        public int? RetokenizeCapturedWithRuleId { get; private set; }

        public CaptureRule(int? id, string name, string contentName, int? retokenizeCapturedWithRuleId) : base(id, name, contentName)
        {
            RetokenizeCapturedWithRuleId = retokenizeCapturedWithRuleId;
        }

        public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst)
        {
            
        }

        public override ICompiledRule Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG)
        {
            return null;
        }

    }
}