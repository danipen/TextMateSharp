namespace TextMateSharp.Internal.Oniguruma
{
    public class OnigRegExp
    {
        private OnigString lastSearchString;
        private int lastSearchPosition;
        private OnigResult lastSearchResult;
        //private Regex regex;

        public OnigRegExp(string source)
        {
            lastSearchString = null;
            lastSearchPosition = -1;
            lastSearchResult = null;
            /*byte[] pattern = source.getBytes(StandardCharsets.UTF_8);
			this.regex = new Regex(pattern, 0, pattern.length, Option.CAPTURE_GROUP, UTF8Encoding.INSTANCE, Syntax.DEFAULT,
					WarnCallback.DEFAULT);*/
        }

        public OnigResult Search(OnigString str, int position)
        {
            if (lastSearchString == str && lastSearchPosition <= position &&
                (lastSearchResult == null || lastSearchResult.LocationAt(0) >= position))
            {
                return lastSearchResult;
            }

            lastSearchString = str;
            lastSearchPosition = position;
            //lastSearchResult = Search(str.utf8_value, position, str.utf8_value.length);
            return lastSearchResult;
        }

        private OnigResult Search(byte[] data, int position, int end)
        {
            /*Matcher matcher = regex.matcher(data);
			int status = matcher.search(position, end, Option.DEFAULT);
			if (status != Matcher.FAILED)
			{
				Region region = matcher.getEagerRegion();
				return new OnigResult(region, -1);
			}*/
            return null;
        }
    }
}
