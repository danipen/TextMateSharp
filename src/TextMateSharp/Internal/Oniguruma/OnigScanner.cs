namespace TextMateSharp.Internal.Oniguruma
{
    public class OnigScanner
    {
        private OnigSearcher searcher;

        public OnigScanner(string[] regexps)
        {
            this.searcher = new OnigSearcher(regexps);
        }

        public IOnigNextMatchResult FindNextMatchSync(string source, in int charOffset)
        {
            OnigResult bestResult = searcher.Search(source, charOffset);
            if (bestResult != null)
            {
                return new OnigNextMatchResult(bestResult);
            }
            return null;
        }
    }
}