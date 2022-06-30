using System.Collections.Generic;
using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    internal class LineTokens
    {
        private string _lineText;

        // used only if `_emitBinaryTokens` is false.
        private List<IToken> _tokens;

        private bool _emitBinaryTokens;

        // used only if `_emitBinaryTokens` is true.
        private List<int> binaryTokens;

        private int _lastTokenEndIndex;

        internal LineTokens(bool emitBinaryTokens, string lineText)
        {
            this._emitBinaryTokens = emitBinaryTokens;
            this._lineText = lineText;
            if (this._emitBinaryTokens)
            {
                this._tokens = null;
                this.binaryTokens = new List<int>();
            }
            else
            {
                this._tokens = new List<IToken>();
                this.binaryTokens = null;
            }
            this._lastTokenEndIndex = 0;
        }

        public void Produce(StackElement stack, int endIndex)
        {
            this.ProduceFromScopes(stack.ContentNameScopesList, endIndex);
        }

        public void ProduceFromScopes(ScopeListElement scopesList, int endIndex)
        {
            if (this._lastTokenEndIndex >= endIndex)
            {
                return;
            }

            if (this._emitBinaryTokens)
            {
                int metadata = scopesList.Metadata;
                if (this.binaryTokens.Count != 0 && this.binaryTokens[this.binaryTokens.Count - 1] == metadata)
                {
                    // no need to push a token with the same metadata
                    this._lastTokenEndIndex = endIndex;
                    return;
                }

                this.binaryTokens.Add(this._lastTokenEndIndex);
                this.binaryTokens.Add(metadata);

                this._lastTokenEndIndex = endIndex;
                return;
            }

            List<string> scopes = scopesList.GenerateScopes();

            this._tokens.Add(new Token(this._lastTokenEndIndex >= 0 ? this._lastTokenEndIndex : 0, endIndex, scopes));
            this._lastTokenEndIndex = endIndex;
        }


        public IToken[] GetResult(StackElement stack, int lineLength)
        {
            if (this._tokens.Count != 0 && this._tokens[this._tokens.Count - 1].StartIndex == lineLength - 1)
            {
                // pop produced token for newline
                this._tokens.RemoveAt(this._tokens.Count - 1);
            }

            if (this._tokens.Count == 0)
            {
                this._lastTokenEndIndex = -1;
                this.Produce(stack, lineLength);
                this._tokens[this._tokens.Count - 1].StartIndex = 0;
            }

            return this._tokens.ToArray();
        }

        public int[] GetBinaryResult(StackElement stack, int lineLength)
        {
            if (this.binaryTokens.Count != 0 && this.binaryTokens[this.binaryTokens.Count - 2] == lineLength - 1)
            {
                // pop produced token for newline
                this.binaryTokens.RemoveAt(this.binaryTokens.Count - 1);
                this.binaryTokens.RemoveAt(this.binaryTokens.Count - 1);
            }

            if (this.binaryTokens.Count == 0)
            {
                this._lastTokenEndIndex = -1;
                this.Produce(stack, lineLength);
                this.binaryTokens[this.binaryTokens.Count - 2] = 0;
            }

            int[] result = new int[this.binaryTokens.Count];
            for (int i = 0, len = this.binaryTokens.Count; i < len; i++)
            {
                result[i] = this.binaryTokens[i];
            }

            return result;
        }
    }
}