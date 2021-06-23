using System.Collections.Generic;

using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Internal.Rules
{
    public class BeginWhileRule : Rule
    {
        private RegExpSource begin;
        public List<CaptureRule> beginCaptures;
        public List<CaptureRule> whileCaptures;
        private RegExpSource _while;
        public bool whileHasBackReferences;
        public bool hasMissingPatterns;
        public int[] patterns;
        private RegExpSourceList cachedCompiledPatterns;
        private RegExpSourceList cachedCompiledWhilePatterns;

        public BeginWhileRule(int id, string name, string contentName, string begin,
                List<CaptureRule> beginCaptures, string _while, List<CaptureRule> whileCaptures,
                ICompilePatternsResult patterns) : base(id, name, contentName)
        {
            this.begin = new RegExpSource(begin, this.id);
            this.beginCaptures = beginCaptures;
            this.whileCaptures = whileCaptures;
            this._while = new RegExpSource(_while, -2);
            this.whileHasBackReferences = this._while.HasBackReferences();
            this.patterns = patterns.patterns;
            this.hasMissingPatterns = patterns.hasMissingPatterns;
            this.cachedCompiledPatterns = null;
            this.cachedCompiledWhilePatterns = null;
        }

        public string getWhileWithResolvedBackReferences(string lineText, IOnigCaptureIndex[] captureIndices)
        {
            return this._while.ResolveBackReferences(lineText, captureIndices);
        }

        public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst)
        {
            if (isFirst)
            {
                Rule rule;
                foreach (int pattern in patterns)
                {
                    rule = grammar.GetRule(pattern);
                    rule.CollectPatternsRecursive(grammar, sourceList, false);
                }
            }
            else
            {
                sourceList.Push(this.begin);
            }
        }

        public override ICompiledRule Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG)
        {
            this.Precompile(grammar);
            return this.cachedCompiledPatterns.Compile(grammar, allowA, allowG);
        }

        private void Precompile(IRuleRegistry grammar)
        {
            if (this.cachedCompiledPatterns == null)
            {
                this.cachedCompiledPatterns = new RegExpSourceList();
                this.CollectPatternsRecursive(grammar, this.cachedCompiledPatterns, true);
            }
        }

        public ICompiledRule CompileWhile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG)
        {
            this.PrecompileWhile();
            if (this._while.HasBackReferences())
            {
                this.cachedCompiledWhilePatterns.SetSource(0, endRegexSource);
            }
            return this.cachedCompiledWhilePatterns.Compile(grammar, allowA, allowG);
        }

        private void PrecompileWhile()
        {
            if (this.cachedCompiledWhilePatterns == null)
            {
                this.cachedCompiledWhilePatterns = new RegExpSourceList();
                this.cachedCompiledWhilePatterns.Push(this._while.HasBackReferences() ? this._while.Clone() : this._while);
            }
        }

    }
}