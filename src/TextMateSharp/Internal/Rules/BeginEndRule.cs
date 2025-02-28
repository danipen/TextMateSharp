using System.Collections.Generic;
using Onigwrap;

namespace TextMateSharp.Internal.Rules
{
    public class BeginEndRule : Rule
    {
        public List<CaptureRule> BeginCaptures { get; private set; }
        public bool EndHasBackReferences { get; private set; }
        public List<CaptureRule> EndCaptures { get; private set; }
        public bool ApplyEndPatternLast { get; private set; }
        public bool HasMissingPatterns { get; private set; }
        public IList<RuleId> Patterns { get; private set; }

        private RegExpSource _begin;
        private RegExpSource _end;
        private RegExpSourceList _cachedCompiledPatterns;

        public BeginEndRule(RuleId id, string name, string contentName, string begin, List<CaptureRule> beginCaptures,
            string end, List<CaptureRule> endCaptures, bool applyEndPatternLast, CompilePatternsResult patterns)
            : base(id, name, contentName)
        {
            _begin = new RegExpSource(begin, this.Id);
            _end = new RegExpSource(end, RuleId.END_RULE);

            BeginCaptures = beginCaptures;
            EndHasBackReferences = _end.HasBackReferences();
            EndCaptures = endCaptures;
            ApplyEndPatternLast = applyEndPatternLast;
            Patterns = patterns.Patterns;
            HasMissingPatterns = patterns.HasMissingPatterns;

            _cachedCompiledPatterns = null;
        }

        public string GetEndWithResolvedBackReferences(string lineText, IOnigCaptureIndex[] captureIndices)
        {
            return this._end.ResolveBackReferences(lineText, captureIndices);
        }

        public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst)
        {
            if (!isFirst)
            {
                // Do not attempt to add patrterns for the frist time
                foreach (RuleId pattern in this.Patterns)
                {
                    Rule rule = grammar.GetRule(pattern);
                    rule.CollectPatternsRecursive(grammar, sourceList, false);
                }

                return;
            }

            sourceList.Push(this._begin);
        }

        public override CompiledRule Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG)
        {
            RegExpSourceList precompiled = this.Precompile(grammar);
            if (this._end.HasBackReferences())
            {
                if (this.ApplyEndPatternLast)
                {
                    precompiled.SetSource(precompiled.Length() - 1, endRegexSource);
                }
                else
                {
                    precompiled.SetSource(0, endRegexSource);
                }
            }
            return this._cachedCompiledPatterns.Compile(allowA, allowG);
        }

        private RegExpSourceList Precompile(IRuleRegistry grammar)
        {
            if (this._cachedCompiledPatterns == null)
            {
                this._cachedCompiledPatterns = new RegExpSourceList();

                this.CollectPatternsRecursive(grammar, this._cachedCompiledPatterns, true);

                if (this.ApplyEndPatternLast)
                {
                    this._cachedCompiledPatterns.Push(this._end.HasBackReferences() ? this._end.Clone() : this._end);
                }
                else
                {
                    this._cachedCompiledPatterns.UnShift(this._end.HasBackReferences() ? this._end.Clone() : this._end);
                }
            }
            return this._cachedCompiledPatterns;
        }
    }
}