using System;

namespace TextMateSharp.Internal.Rules
{
    public class CaptureRule : Rule
    {
        public int retokenizeCapturedWithRuleId;

        public CaptureRule(int id, string name, string contentName, int retokenizeCapturedWithRuleId) : base(id, name, contentName)
        {
            this.retokenizeCapturedWithRuleId = retokenizeCapturedWithRuleId;
        }

        public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst)
        {
            throw new InvalidOperationException("Not supported");
        }

        public override ICompiledRule Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG)
        {
            throw new InvalidOperationException("Not supported");
        }

    }
}