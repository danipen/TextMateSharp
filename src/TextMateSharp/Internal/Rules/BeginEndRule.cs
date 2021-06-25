using System.Collections.Generic;

using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Internal.Rules
{
    public class BeginEndRule : Rule
    {
        private RegExpSource begin;
        public List<CaptureRule> beginCaptures;
        private RegExpSource end;
        public bool endHasBackReferences;
        public List<CaptureRule> endCaptures;
        public bool applyEndPatternLast;
        public bool hasMissingPatterns;
        public int?[] patterns;
        private RegExpSourceList cachedCompiledPatterns;

        public BeginEndRule(int? id, string name, string contentName, string begin, List<CaptureRule> beginCaptures,
            string end, List<CaptureRule> endCaptures, bool applyEndPatternLast, ICompilePatternsResult patterns)
            : base(id, name, contentName)
        {
            this.begin = new RegExpSource(begin, this.id);
            this.beginCaptures = beginCaptures;
            this.end = new RegExpSource(end, -1);
            this.endHasBackReferences = this.end.HasBackReferences();
            this.endCaptures = endCaptures;
            this.applyEndPatternLast = applyEndPatternLast;
            this.patterns = patterns.patterns;
            this.hasMissingPatterns = patterns.hasMissingPatterns;
            this.cachedCompiledPatterns = null;
        }

        public string GetEndWithResolvedBackReferences(string lineText, IOnigCaptureIndex[] captureIndices)
        {
            return this.end.ResolveBackReferences(lineText, captureIndices);
        }

        public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst)
        {
            if (isFirst)
            {
                foreach (int pattern in this.patterns)
                {
                    Rule rule = grammar.GetRule(pattern);
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
            RegExpSourceList precompiled = this.Precompile(grammar);
            if (this.end.HasBackReferences())
            {
                if (this.applyEndPatternLast)
                {
                    precompiled.SetSource(precompiled.Length() - 1, endRegexSource);
                }
                else
                {
                    precompiled.SetSource(0, endRegexSource);
                }
            }
            return this.cachedCompiledPatterns.Compile(grammar, allowA, allowG);
        }

        private RegExpSourceList Precompile(IRuleRegistry grammar)
        {
            if (this.cachedCompiledPatterns == null)
            {
                this.cachedCompiledPatterns = new RegExpSourceList();

                this.CollectPatternsRecursive(grammar, this.cachedCompiledPatterns, true);

                if (this.applyEndPatternLast)
                {
                    this.cachedCompiledPatterns.Push(this.end.HasBackReferences() ? this.end.Clone() : this.end);
                }
                else
                {
                    this.cachedCompiledPatterns.UnShift(this.end.HasBackReferences() ? this.end.Clone() : this.end);
                }
            }
            return this.cachedCompiledPatterns;
        }
    }
}