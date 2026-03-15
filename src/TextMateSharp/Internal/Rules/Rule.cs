using Onigwrap;
using System;

using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Rules
{
    public abstract class Rule
    {
        internal RuleId Id { get; private set; }

        private readonly bool _nameIsCapturing;
        private readonly string _name;

        private readonly bool _contentNameIsCapturing;
        private readonly string _contentName;

        protected Rule(RuleId id, string name, string contentName)
        {
            Id = id;

            _name = name;
            _nameIsCapturing = RegexSource.HasCaptures(this._name);
            _contentName = contentName;
            _contentNameIsCapturing = RegexSource.HasCaptures(this._contentName);
        }

        public string GetName(ReadOnlyMemory<char> lineText, IOnigCaptureIndex[] captureIndices)
        {
            if (!this._nameIsCapturing)
            {
                return this._name;
            }

            return RegexSource.ReplaceCaptures(this._name, lineText, captureIndices);
        }

        public string GetContentName(ReadOnlyMemory<char> lineText, IOnigCaptureIndex[] captureIndices)
        {
            if (!this._contentNameIsCapturing)
            {
                return this._contentName;
            }
            return RegexSource.ReplaceCaptures(this._contentName, lineText, captureIndices);
        }

        public abstract void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst);

        public abstract CompiledRule Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG);
    }
}