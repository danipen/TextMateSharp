using System.Collections.Generic;

namespace TextMateSharp.Internal.Rules
{
    public class IncludeOnlyRule : Rule
    {
        public bool HasMissingPatterns { get; private set; }
        public IList<RuleId> Patterns { get; private set; }

        private RegExpSourceList _cachedCompiledPatterns;

        public IncludeOnlyRule(RuleId id, string name, string contentName, CompilePatternsResult patterns) : base(id, name, contentName)
        {
            Patterns = patterns.Patterns;
            HasMissingPatterns = patterns.HasMissingPatterns;

            _cachedCompiledPatterns = null;
        }

        public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFrist)
        {
            foreach (var pattern in this.Patterns)
            {
                Rule rule = grammar.GetRule(pattern);
                rule.CollectPatternsRecursive(grammar, sourceList, false);
            }
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