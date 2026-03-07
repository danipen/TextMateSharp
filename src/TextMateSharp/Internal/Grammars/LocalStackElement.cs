namespace TextMateSharp.Internal.Grammars
{
    class LocalStackElement
    {
        internal AttributedScopeStack Scopes { get; private set; }
        internal int EndPos { get; private set; }

        internal LocalStackElement(AttributedScopeStack scopes, int endPos)
        {
            Scopes = scopes;
            EndPos = endPos;
        }
    }
}