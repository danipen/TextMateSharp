using System.Collections.Generic;

namespace TextMateSharp.Internal.Matcher
{
    public interface IMatchesName<T>
    {
        bool Match(ICollection<string> names, T scopes);
    }

    public class NameMatcher : IMatchesName<List<string>>
    {
        public bool Match(ICollection<string> identifers, List<string> scopes)
        {
            if (scopes.Count < identifers.Count)
            {
                return false;
            }

            for (int i = 0; i < scopes.Count; i++)
            {
                foreach (string identifier in identifers)
                {
                    if (!ScopesAreMatching(scopes[i], identifier))
                        return false;
                }
            }

            return true;
        }

        private bool ScopesAreMatching(string thisScopeName, string scopeName)
        {
            if (thisScopeName == null)
            {
                return false;
            }
            if (thisScopeName.Equals(scopeName))
            {
                return true;
            }
            int len = scopeName.Length;
            return thisScopeName.Length > len && thisScopeName.Substring(0, len).Equals(scopeName)
                    && thisScopeName[len] == '.';
        }
    }
}