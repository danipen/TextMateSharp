using System;
using System.Collections.Generic;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Matcher
{
    public interface IMatchesName<T>
    {
        bool Match(ICollection<string> names, T scopes);
    }

    public class NameMatcher : IMatchesName<List<string>>
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

            int lastIndex = 0;
            foreach (string identifier in identifers)
            {
                bool found = false;
                for (int i = lastIndex; i < scopes.Count; i++)
                {
                    if (ScopesAreMatching(scopes[i], identifier))
                    {
                        // BUG FIX: Original code used lastIndex++ which only incremented by 1 from
                        // the previous starting position, not from the actual match position.
                        // This caused the next search to potentially re-scan already checked scopes.
                        // Correct behavior: Start the next search immediately after the current match
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
            return (thisScopeName.Length > len) && (thisScopeName[len] == '.') && thisScopeName.SubstringAtIndexes(0, len).Equals(scopeName);
        }
    }
}