namespace TextMateSharp.Internal.Rules
{
    public class IncludeOnlyRule : Rule
    {
        public bool hasMissingPatterns;
        public int[] patterns;
        private RegExpSourceList cachedCompiledPatterns;

        public IncludeOnlyRule(int id, string name, string contentName, ICompilePatternsResult patterns) : base(id, name, contentName)
        {
            this.patterns = patterns.patterns;
            this.hasMissingPatterns = patterns.hasMissingPatterns;
            this.cachedCompiledPatterns = null;
        }

        public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst)
        {
            foreach (int pattern in this.patterns)
            {
                Rule rule = grammar.GetRule(pattern);
                rule.CollectPatternsRecursive(grammar, sourceList, false);
            }
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