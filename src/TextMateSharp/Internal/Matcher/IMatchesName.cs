using System;
using System.Collections.Generic;

namespace TextMateSharp.Internal.Matcher
{
    public interface IMatchesName<T>
    {
        bool Match(ICollection<string> names, T scopes);
    }

    public sealed class NameMatcher : IMatchesName<List<string>>
    {
        public static IMatchesName<List<string>> Default = new NameMatcher();

        public bool Match(ICollection<string> identifers, List<string> scopes)
        {
            if (identifers == null) throw new ArgumentNullException(nameof(identifers));
            if (scopes == null) throw new ArgumentNullException(nameof(scopes));

            if (scopes.Count < identifers.Count)
            {
                return false;
            }

            if (identifers is List<string> list)
            {
                // Indexed, without boxing the enumerator: selectors' identifiers are lists.
                int start = 0;
                for (int j = 0; j < list.Count; j++)
                {
                    start = IndexOfMatching(scopes, start, list[j]);
                    if (start < 0)
                    {
                        return false;
                    }
                    start++;
                }
                return true;
            }

            int lastIndex = 0;
            foreach (string identifier in identifers)
            {
                bool found = false;
                for (int i = lastIndex; i < scopes.Count; i++)
                {
                    if (ScopesAreMatching(scopes[i], identifier))
                    {
                        // Start the next search immediately after the current match
                        lastIndex = i + 1;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        // The index of the first scope from start that matches identifier, or -1.
        private static int IndexOfMatching(List<string> scopes, int start, string identifier)
        {
            for (int i = start; i < scopes.Count; i++)
            {
                if (ScopesAreMatching(scopes[i], identifier))
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool ScopesAreMatching(string thisScopeName, string scopeName)
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
            return (thisScopeName.Length > len) && (thisScopeName[len] == '.') && string.CompareOrdinal(thisScopeName, 0, scopeName, 0, len) == 0;
        }
    }
}