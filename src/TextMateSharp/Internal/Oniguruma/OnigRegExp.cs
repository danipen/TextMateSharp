using System;
using System.Collections.Generic;

namespace TextMateSharp.Internal.Oniguruma
{
    public class OnigRegExp : IDisposable
    {
        private OnigString lastSearchString;
        private int lastSearchPosition;
        private OnigResult lastSearchResult;
        private ORegex regex;
        private string patterDebug;
        public OnigRegExp(string source)
        {
            lastSearchString = null;
            lastSearchPosition = -1;
            lastSearchResult = null;

            regex = new ORegex(source, false, false);
            patterDebug = source;
        }

        public void Dispose()
        {
            if (regex != null)
            {
                regex.Dispose();
            }
        }

        // TODO: dispose regex

        public OnigResult Search(OnigString str, int position)
        {
            if (lastSearchString == str && lastSearchPosition <= position &&
                (lastSearchResult == null || lastSearchResult.LocationAt(0) >= position))
            {
                return lastSearchResult;
            }

            lastSearchString = str;
            lastSearchPosition = position;
            lastSearchResult = Search(str._string, position);
            return lastSearchResult;
        }

        private OnigResult Search(string data, int position)
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

            OnigResult result = new OnigResult(region, -1);

            /*Matcher matcher = regex.matcher(data);
			int status = matcher.search(position, end, Option.DEFAULT);
			if (status != Matcher.FAILED)
			{
				Region region = matcher.getEagerRegion();
				return new OnigResult(region, -1);
			}*/
            return result;
        }
    }
}
