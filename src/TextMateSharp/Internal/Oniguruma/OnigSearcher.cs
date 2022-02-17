using System.Collections.Generic;

namespace TextMateSharp.Internal.Oniguruma
{
    public class OnigSearcher
    {
        private List<OnigRegExp> _regExps;

        public OnigSearcher(string[] regexps)
        {
            _regExps = new List<OnigRegExp>(regexps.Length);
            foreach (string regexp in regexps)
            {
                _regExps.Add(new OnigRegExp(regexp));
            }
        }

        public OnigResult Search(string source, in int charOffset)
        {
            int byteOffset = charOffset;

            int bestLocation = 0;
            OnigResult bestResult = null;
            int index = 0;

            foreach (OnigRegExp regExp in _regExps)
            {
                OnigResult result = regExp.Search(source, byteOffset);
                if (result != null && result.Count() > 0)
                {
                    int location = result.LocationAt(0);

                    if (bestResult == null || location < bestLocation)
                    {
                        bestLocation = location;
                        bestResult = result;
                        bestResult.SetIndex(index);
                    }

                    if (location == byteOffset)
                    {
                        break;
                    }
                }
                index++;
            }
            return bestResult;
        }

    }
}