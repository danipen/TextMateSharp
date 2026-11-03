namespace TextMateSharp.Internal.Grammars
{
    readonly struct LocalStackElement
    {
        internal AttributedScopeStack Scopes { get; }
        internal int EndPos { get; }

        internal LocalStackElement(AttributedScopeStack scopes, int endPos)
        {
            Scopes = scopes;
            EndPos = endPos;
        }
    }
}