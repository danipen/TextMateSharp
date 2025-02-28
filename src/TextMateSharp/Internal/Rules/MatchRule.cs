using System.Collections.Generic;

namespace TextMateSharp.Internal.Rules
{
    public class MatchRule : Rule
    {
        public List<CaptureRule> Captures { get; private set; }

        private RegExpSource _match;
        private RegExpSourceList _cachedCompiledPatterns;

        public MatchRule(RuleId id, string name, string match, List<CaptureRule> captures) : base(id, name, null)
        {
            this._match = new RegExpSource(match, this.Id);
            this.Captures = captures;
            this._cachedCompiledPatterns = null;
        }

        public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFrist)
        {
            sourceList.Push(this._match);
        }

        public override CompiledRule Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG)
        {
            if (this._cachedCompiledPatterns == null)
            {
                this._cachedCompiledPatterns = new RegExpSourceList();
                this.CollectPatternsRecursive(grammar, this._cachedCompiledPatterns, true);
            }
            return this._cachedCompiledPatterns.Compile(allowA, allowG);
        }
    }
}