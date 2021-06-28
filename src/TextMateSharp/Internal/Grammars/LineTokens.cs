using System.Collections.Generic;

using TextMateSharp.Grammars;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Grammars
{
    internal class LineTokens
    {

        private string lineText;

        // used only if `_emitBinaryTokens` is false.
        private List<IToken> tokens;


        private bool emitBinaryTokens;

        // used only if `_emitBinaryTokens` is true.
        private List<int> binaryTokens;

        private int lastTokenEndIndex;

        internal LineTokens(bool emitBinaryTokens, string lineText)
        {
            this.emitBinaryTokens = emitBinaryTokens;
            this.lineText = lineText;
            if (this.emitBinaryTokens)
            {
                this.tokens = null;
                this.binaryTokens = new List<int>();
            }
            else
            {
                this.tokens = new List<IToken>();
                this.binaryTokens = null;
            }
            this.lastTokenEndIndex = 0;
        }

        public void Produce(StackElement stack, int endIndex)
        {
            this.ProduceFromScopes(stack.contentNameScopesList, endIndex);
        }

        public void ProduceFromScopes(ScopeListElement scopesList, int endIndex)
        {
            if (this.lastTokenEndIndex >= endIndex)
            {
                return;
            }

            if (this.emitBinaryTokens)
            {
                int metadata = scopesList.metadata;
                if (this.binaryTokens.Count != 0 && this.binaryTokens[this.binaryTokens.Count - 1] == metadata)
                {
                    // no need to push a token with the same metadata
                    this.lastTokenEndIndex = endIndex;
                    return;
                }

                this.binaryTokens.Add(this.lastTokenEndIndex);
                this.binaryTokens.Add(metadata);

                this.lastTokenEndIndex = endIndex;
                return;
            }

            List<string> scopes = scopesList.GenerateScopes();

            foreach (string scope in scopes)
            {
                System.Diagnostics.Debug.WriteLine("  token: |" + this.lineText.SubstringAtIndexes(
                    this.lastTokenEndIndex, endIndex)
                    .Replace("\n", "\\n") + '|');
                System.Diagnostics.Debug.WriteLine("      * " + scope);
            }

            this.tokens.Add(new Token(this.lastTokenEndIndex, endIndex, scopes));
            this.lastTokenEndIndex = endIndex;
        }

        public IToken[] GetResult(StackElement stack, int lineLength)
        {
            if (this.tokens.Count != 0 && this.tokens[this.tokens.Count - 1].StartIndex == lineLength - 1)
            {
                // pop produced token for newline
                this.tokens.RemoveAt(this.tokens.Count - 1);
            }

            if (this.tokens.Count == 0)
            {
                this.lastTokenEndIndex = -1;
                this.Produce(stack, lineLength);
                this.tokens[this.tokens.Count - 1].StartIndex = 0;
            }

            return this.tokens.ToArray();
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
                this.lastTokenEndIndex = -1;
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