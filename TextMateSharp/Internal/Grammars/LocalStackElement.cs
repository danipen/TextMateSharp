namespace TextMateSharp.Internal.Grammars
{
    class LocalStackElement
    {
        private ScopeListElement scopes;
        private int endPos;

        public LocalStackElement(ScopeListElement scopes, int endPos)
        {
            this.scopes = scopes;
            this.endPos = endPos;
        }

        public ScopeListElement GetScopes()
        {
            return scopes;
        }

        public int GetEndPos()
        {
            return endPos;
        }
    }
}