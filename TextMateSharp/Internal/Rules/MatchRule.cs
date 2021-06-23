using System.Collections.Generic;

namespace TextMateSharp.Internal.Rules
{
    public class MatchRule : Rule
    {

        private RegExpSource match;
        public List<CaptureRule> captures;
        private RegExpSourceList cachedCompiledPatterns;

        public MatchRule(int id, string name, string match, List<CaptureRule> captures) : base(id, name, null)
        {
            this.match = new RegExpSource(match, this.id);
            this.captures = captures;
            this.cachedCompiledPatterns = null;
        }

        public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst)
        {
            sourceList.Push(this.match);
        }

        public override ICompiledRule Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG)
        {
            if (this.cachedCompiledPatterns == null)
            {
                this.cachedCompiledPatterns = new RegExpSourceList();
                this.CollectPatternsRecursive(grammar, this.cachedCompiledPatterns, true);
            }
            return this.cachedCompiledPatterns.Compile(grammar, allowA, allowG);
        }
    }
}