namespace TextMateSharp.Internal.Oniguruma
{
    public class OnigScanner
    {
        private OnigSearcher searcher;

        public OnigScanner(string[] regexps)
        {
            this.searcher = new OnigSearcher(regexps);
        }

        public IOnigNextMatchResult FindNextMatchSync(OnigString source, int charOffset)
        {
            OnigResult bestResult = searcher.Search(source, charOffset);
            if (bestResult != null)
            {
                return new OnigNextMatchResult(bestResult, source);
            }
            return null;
        }

        public IOnigNextMatchResult FindNextMatchSync(string lin, int pos)
        {
            return FindNextMatchSync(new OnigString(lin), pos);
        }

    }
}