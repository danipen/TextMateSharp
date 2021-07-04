using System;
using System.Collections.Generic;

namespace TextMateSharp.Internal.Oniguruma
{
    public class OnigRegExp : IDisposable
    {
        private string lastSearchString;
        private int lastSearchPosition;
        private OnigResult lastSearchResult;
        private ORegex regex;
        private bool _disposed;
        public OnigRegExp(string source)
        {
            lastSearchString = null;
            lastSearchPosition = -1;
            lastSearchResult = null;

            regex = new ORegex(source, false, false);
        }

        ~OnigRegExp() => Dispose(false);
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (regex != null)
                regex.Dispose();
            
            _disposed = true;
        }        
        
        public OnigResult Search(string str, int position)
        {
            if (lastSearchString == str && lastSearchPosition <= position &&
                (lastSearchResult == null || lastSearchResult.LocationAt(0) >= position))
            {
                return lastSearchResult;
            }

            lastSearchString = str;
            lastSearchPosition = position;
            lastSearchResult = GetOnigResult(str, position);
            return lastSearchResult;
        }

        private OnigResult GetOnigResult(string data, int position)
        {
            List<ORegexResult> results = regex.SafeSearch(data, position);

            if (results == null || results.Count == 0)
                return null;

            Region region = new Region(results.Count);

            for (int i = 0; i < results.Count; i++)
            {
                region.beg[i] = results[i].Position;
                region.end[i] = results[i].Position + results[i].Length;
            }

            return new OnigResult(region, -1);
        }
    }
}
