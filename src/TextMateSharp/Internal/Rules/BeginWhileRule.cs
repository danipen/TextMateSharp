using System.Collections.Generic;
using Onigwrap;

namespace TextMateSharp.Internal.Rules
{
    public class BeginWhileRule : Rule
    {
        public List<CaptureRule> BeginCaptures { get; private set; }
        public List<CaptureRule> WhileCaptures { get; private set; }
        public bool WhileHasBackReferences { get; private set; }
        public bool HasMissingPatterns { get; private set; }
        public IList<RuleId>Patterns { get; private set; }

        private RegExpSource _begin;
        private RegExpSource _while;
        private RegExpSourceList _cachedCompiledPatterns;
        private RegExpSourceList _cachedCompiledWhilePatterns;

        public BeginWhileRule(RuleId id, string name, string contentName, string begin,
                List<CaptureRule> beginCaptures, string whileStr, List<CaptureRule> whileCaptures,
                CompilePatternsResult patterns) : base(id, name, contentName)
        {
            _begin = new RegExpSource(begin, this.Id);
            _while = new RegExpSource(whileStr, RuleId.WHILE_RULE);

            BeginCaptures = beginCaptures;
            WhileCaptures = whileCaptures;
            WhileHasBackReferences = this._while.HasBackReferences();
            Patterns = patterns.Patterns;
            HasMissingPatterns = patterns.HasMissingPatterns;

            _cachedCompiledPatterns = null;
            _cachedCompiledWhilePatterns = null;
        }

        public string getWhileWithResolvedBackReferences(string lineText, IOnigCaptureIndex[] captureIndices)
        {
            return this._while.ResolveBackReferences(lineText, captureIndices);
        }

        public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFrist)
        {
            if (isFrist)
            {
                Rule rule;
                foreach (RuleId pattern in Patterns)
                {
                    rule = grammar.GetRule(pattern);
                    rule.CollectPatternsRecursive(grammar, sourceList, false);
                }
            }
            else
            {
                sourceList.Push(this._begin);
            }
        }

        public override CompiledRule Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG)
        {
            this.Precompile(grammar);
            return this._cachedCompiledPatterns.Compile(allowA, allowG);
        }

        private void Precompile(IRuleRegistry grammar)
        {
            if (this._cachedCompiledPatterns == null)
            {
                this._cachedCompiledPatterns = new RegExpSourceList();
                this.CollectPatternsRecursive(grammar, this._cachedCompiledPatterns, true);
            }
        }

        public CompiledRule CompileWhile(string endRegexSource, bool allowA, bool allowG)
        {
            this.PrecompileWhile();
            if (this._while.HasBackReferences())
            {
                this._cachedCompiledWhilePatterns.SetSource(0, endRegexSource);
            }
            return this._cachedCompiledWhilePatterns.Compile(allowA, allowG);
        }

        private void PrecompileWhile()
        {
            if (this._cachedCompiledWhilePatterns == null)
            {
                this._cachedCompiledWhilePatterns = new RegExpSourceList();
                this._cachedCompiledWhilePatterns.Push(this._while.HasBackReferences() ? this._while.Clone() : this._while);
            }
        }

    }
}