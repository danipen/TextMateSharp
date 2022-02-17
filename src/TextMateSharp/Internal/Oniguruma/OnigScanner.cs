namespace TextMateSharp.Internal.Oniguruma
{
    public class OnigScanner
    {
        private OnigSearcher _searcher;

        public OnigScanner(string[] regexps)
        {
            this._searcher = new OnigSearcher(regexps);
        }

        public IOnigNextMatchResult FindNextMatchSync(string source, in int charOffset)
        {
            OnigResult bestResult = _searcher.Search(source, charOffset);
            if (bestResult != null)
            {
                return new OnigNextMatchResult(bestResult);
            }
            return null;
        }
    }
}