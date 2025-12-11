using System;
using Onigwrap;

using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Rules
{
    public abstract class Rule
    {
        public RuleId Id { get; private set; }

        private bool _nameIsCapturing;
        private string _name;

        private bool _contentNameIsCapturing;
        private string _contentName;

        public Rule(RuleId id, string name, string contentName)
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