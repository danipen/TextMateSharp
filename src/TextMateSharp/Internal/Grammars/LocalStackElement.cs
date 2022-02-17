namespace TextMateSharp.Internal.Grammars
{
    class LocalStackElement
    {
        public ScopeListElement Scopes { get; private set; }
        public int EndPos { get; private set; }

        public LocalStackElement(ScopeListElement scopes, int endPos)
        {
            Scopes = scopes;
            EndPos = endPos;
        }
    }
}